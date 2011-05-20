using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using NeuroRighter.DataTypes;

namespace NeuroRighter.Output
{   
    // called when the 2+requested number of buffer loads have occured
    internal delegate void StimulationCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    internal delegate void QueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    internal delegate void DAQLoadCompletedHandler(object sender, EventArgs e);

    internal class StimBuffer : NROutBuffer<StimulusOutEvent>
    {

        //events
        private int queueThreshold = 0;
        public event QueueLessThanThresholdHandler QueueLessThanThreshold;
        public event StimulationCompleteHandler StimulationComplete;
        public event DAQLoadCompletedHandler DAQLoadCompleted;

        // This class's thread
        Thread thrd;

        // Public Properties
        internal ulong bufferIndex = 0;
        internal double[,] AnalogBuffer;
        internal UInt32[] DigitalBuffer;
        internal bool StillWritting = false;
        internal ulong numBuffLoadsCompleted = 0;
        internal uint numBuffLoadsRequired = 0;
        internal bool running = false;

        // Private Properties
        private uint WaveLength;
        private uint StimulusLength;
        private uint NumSampWrittenForCurrentStim = 0;
        private uint NumSamplesLoadedForWave = 0;
        private uint STIM_SAMPLING_FREQ;
        private uint NUM_SAMPLES_BLANKING;
        private ulong[] StimSample; 
        private double[,] AnalogEncode;
        private UInt32[,] DigitalEncode;
        private double[] AnalogPoint = new double[] {0, 0};
        private UInt32 DigitalPoint;
        private ulong StimulusIndex;
        private string[] s = DaqSystem.Local. GetPhysicalChannels(PhysicalChannelTypes.All, PhysicalChannelAccess.Internal);
        private List<StimulusOutEvent> outerbuffer;
        private StimulusOutEvent currentStim;
        private bool digitaldone, analogdone;

        //DO line that will have the blanking signal for different hardware configurations
        private const int BLANKING_BIT_32bitPort = 31;
        private const int BLANKING_BIT_8bitPort = 7;
        private const int PORT_OFFSET_32bitPort = 7;
        private const int PORT_OFFSET_8bitPort = 0;

        // Constants for different systems
        private int NumAOChannels;
        private int RowOffset;

        //Stuff that gets defined with input arguments to constructor
        private int[] TimeVector;
        private int[] ChannelVector;
        private double[,] WaveMatrix;
        private uint BUFFSIZE;
        private uint LengthWave;

        // DEBUGGING
        private Stopwatch sw = new Stopwatch();
        DateTime startTime;
        DateTime tickTime;
        TimeSpan tickDiff;

        //background worker requires the DAQ constructs so that it can encapsulate the asynchronous stimulation task
        AnalogMultiChannelWriter stimAnalogWriter;
        DigitalSingleChannelWriter stimDigitalWriter;
        Task stimDigitalTask, stimAnalogTask, buffLoadTask;

        //Constructor to create a Stim Buffer object for use by File2Stim (one-shot mode)
        internal StimBuffer(int[] TimeVector, int[] ChannelVector, double[,] WaveMatrix, int LengthWave,
            int BUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING)
        {
            this.TimeVector = TimeVector;
            this.ChannelVector = ChannelVector;
            this.WaveMatrix = WaveMatrix;
            this.BUFFSIZE = (uint)BUFFSIZE;
            this.LengthWave = (uint)LengthWave;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.NUM_SAMPLES_BLANKING = (uint)NUM_SAMPLES_BLANKING;
            //isAppending = false;

        }
        //constructor if using stim buffer in Append mode
       
        //constructor if using stim buffer in Append mode- with lists!
        internal StimBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING, int queueThreshold)
        {
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.NUM_SAMPLES_BLANKING = (uint)NUM_SAMPLES_BLANKING;
            this.queueThreshold = queueThreshold;

            // What are the buffer offset settings for this system?
            NumAOChannels = 2;
            RowOffset = 0;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                NumAOChannels = 4;
                RowOffset = 2;
            }

            // Make an outer buffer to Append stimuli to before loading into DAQ's memory
            outerbuffer = new List<StimulusOutEvent>();
            }

        internal void Setup(AnalogMultiChannelWriter stimAnalogWriter, DigitalSingleChannelWriter stimDigitalWriter, Task stimDigitalTask, Task stimAnalogTask, Task buffLoadTask)//, ulong starttime)
        {

            startTime = DateTime.Now;
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalTask = stimDigitalTask;
            this.buffLoadTask = buffLoadTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;

            //Set buffer regenation mode to off and set parameters
            stimAnalogTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            stimDigitalTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            stimAnalogTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
            stimDigitalTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
            
            // Add reload method to the Counter output event
            buffLoadTask.CounterOutput += new CounterOutputEventHandler(TimerTick);
            
            // Populate the outer-buffer twice
            PopulateBufferAppending();
            
            // Start the counter that tells when to reload the daq
            stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);

            PopulateBufferAppending();

            // Start the counter that tells when to reload the daq
            stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);
        }

        internal void Start()
        {
            running = true;
            stimDigitalTask.Start();
            stimAnalogTask.Start();
        }

        internal void FinishStimulation(EventArgs e)
        {
            if (StimulationComplete != null)
            {
                // Stop the tasks and dispose of them
                stimDigitalTask.Stop();
                stimAnalogTask.Stop();

                stimDigitalTask.Dispose();
                stimAnalogTask.Dispose();

                // Tell NR that stimulation has finished
                StimulationComplete(this, e);
            }
        }
        
        void TimerTick(object sender, EventArgs e)
        {
            if (running)
            {
                tickTime = DateTime.Now;
                tickDiff = tickTime.Subtract(startTime);
                Console.WriteLine(Convert.ToString(tickDiff.TotalMilliseconds) + ": DAQ half-load event.");
                WriteToBuffer();
            }
            else
            {
                FinishStimulation(e);
            }
        }

        internal void WriteToBuffer()
        {
            thrd = Thread.CurrentThread;
            thrd.Priority = ThreadPriority.Highest;

            analogdone = false;
            digitaldone = false;
            PopulateBufferAppending();
            Console.WriteLine("Write to Buffer Started");
            stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);
            analogdone = true;
            digitaldone = true;
            
        }

        internal void Stop()
        {
            running = false;
        }

        internal void PreCompute()//not used for appending- instead, Append needs to perform these actions
        {
           
            // Does as much pre computation of the buffers that will be populated as possible to prevent buffer load lag and resulting DAQ exceptions
            StimSample = new ulong[TimeVector.Length];
            AnalogEncode = new double[2, ChannelVector.Length];
            DigitalEncode = new UInt32[3, ChannelVector.Length];

            WaveLength = (uint)WaveMatrix.GetLength(1);
            StimulusLength = (uint)(WaveLength + 2 * NUM_SAMPLES_BLANKING + 2); //length of waveform + padding on either side due to digital signaling.

            // Populate StimSample
            for (int i = 0; i < StimSample.Length; i++)
                StimSample[i] = (uint)Math.Round((double)(TimeVector[i] * (STIM_SAMPLING_FREQ / 1000)));

            // Populate AnalogEncode
            for (int i = 0; i < AnalogEncode.GetLength(1); i++)
            {
                AnalogEncode[0, i] = Math.Ceiling((double)ChannelVector[i] / 8.0);
                AnalogEncode[1, i] = (double)((ChannelVector[i] - 1) % 8) + 1.0;
            }
                 
           
            // Populate DigitalEncode
            for (int i = 0; i < DigitalEncode.GetLength(1); i++)
            {
                    
                DigitalEncode[0, i] = Convert.ToUInt32(Math.Pow(2, (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort)));
                DigitalEncode[1, i] = channel2MUX_noEN((double)ChannelVector[i]);
                DigitalEncode[2, i] = channel2MUX((double)ChannelVector[i]);
                

            }
            
            // What are the buffer offset settings for this system?
            NumAOChannels = 2;
            RowOffset = 0;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                NumAOChannels = 4;
                RowOffset = 2;
            }

            //How many buffer loads will this stimulus task take? 3 extra are for (1) Account for delay in start that might push
            //last stimulus overtime by a bit and 2 loads to zero out the double buffer.
            numBuffLoadsRequired = 4 + (uint)Math.Ceiling((double)(StimSample[StimSample.Length - 1] / BUFFSIZE));
           
        }      
        
        internal void ValidateStimulusParameters()
        {
            // This method looks at the parameters of stimulus provided in the .olstim file and decides if they are cool or not

            // Check if stimuli are delivered in a logical order and have enough spacing in between
            int TimeBWSamples;
            for (int i = 0; i < StimSample.Length-1; i++)
            {
                TimeBWSamples = (int)(StimSample[i + 1] - StimSample[i]);
                int MinSpacing = (int)(2 * NUM_SAMPLES_BLANKING + 2);
                if(TimeBWSamples < MinSpacing)
                {
                    throw new ArgumentException("Stimulation times are too close to be executed by the hardware. The start and end time of consecutive stimuli need to be spaced by at least " + MinSpacing.ToString() + " samples.");
                }
               
            }

            // Check to make sure StimulusLength < BuffSize    
            if (StimulusLength >= BUFFSIZE)
            {
                throw new ArgumentException("The length of your stimulus waveforms exceeds that of the buffer size. Your waveforms should be less than " + StimulusLength.ToString() + " samples long.");
            }

            // Check to make sure that the stimulus waveforms used are at least 80 samples long
            if (WaveLength < 80)
            {
                throw new ArgumentException("The length of your stimulus waveforms Should be at least 80 Samples long so that its parameters can be encoded by the DAQ in four 20 sample chunks. Shorter stimuli, you can defined multiple ones per line so they are effictively one stimulus.");
            }

        }
        
        //this is the scary one
        internal void PopulateBuffer()
        {
            //clear buffers and reset index
            AnalogBuffer = new double[NumAOChannels, BUFFSIZE]; // buffer for analog channels
            DigitalBuffer = new UInt32[BUFFSIZE]; // buffer for digital channels
            bufferIndex = 0;

            #region Populate the buffers if a stimulus occurs in this particular buffer length

            // Finish up writing the stimulus from the last buffer load if you didn't finish then
            if (NumSampWrittenForCurrentStim < StimulusLength && NumSampWrittenForCurrentStim != 0)
            {
                uint samples2Finish = StimulusLength - NumSampWrittenForCurrentStim;
                for (int i = 0; i < samples2Finish; i++)
                {
                    CalculateAnalogPoint(StimulusIndex, NumSampWrittenForCurrentStim, NumAOChannels);
                    CalculateDigPoint(StimulusIndex, NumSampWrittenForCurrentStim);
                    AnalogBuffer[0 + RowOffset, (int)bufferIndex] = AnalogPoint[0];
                    AnalogBuffer[1 + RowOffset, (int)bufferIndex] = AnalogPoint[1];
                    DigitalBuffer[(int)bufferIndex] = DigitalPoint;
                    NumSampWrittenForCurrentStim++;
                    bufferIndex++;
                }

                // Finished writting current stimulus, reset variables
                NumSampWrittenForCurrentStim = 0;
                StimulusIndex++;
                bufferIndex = StimSample[StimulusIndex] - numBuffLoadsCompleted * BUFFSIZE;
                if (StimSample[StimulusIndex] < numBuffLoadsCompleted * BUFFSIZE)
                {
                    //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " +numBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    throw new Exception ("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " +numBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
            }
            }
            else
            {
                if (StimulusIndex < (ulong)StimSample.Length)
                    {
                        bufferIndex = StimSample[StimulusIndex] - numBuffLoadsCompleted * BUFFSIZE;
                        if (StimSample[StimulusIndex] < numBuffLoadsCompleted * BUFFSIZE)
                        {
                           // MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                            throw new Exception("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    }
                    }
                    else
                    {
                        bufferIndex = BUFFSIZE;
                    }
            }

            // Write to the buffer if the next stimulus is within this buffer's time span
            while(bufferIndex < BUFFSIZE  && bufferIndex >= 0)
            {
                if (NumSampWrittenForCurrentStim < StimulusLength)
                {
                    CalculateAnalogPoint(StimulusIndex, NumSampWrittenForCurrentStim, NumAOChannels);
                    CalculateDigPoint(StimulusIndex, NumSampWrittenForCurrentStim);
                    AnalogBuffer[0 + RowOffset, (int)bufferIndex] = AnalogPoint[0];
                    AnalogBuffer[1 + RowOffset, (int)bufferIndex] = AnalogPoint[1];
                    DigitalBuffer[(int)bufferIndex] = DigitalPoint;
                    NumSampWrittenForCurrentStim++;
                    bufferIndex++;
                }
                else
                {
                    // Finished writting current stimulus, reset variables
                    NumSampWrittenForCurrentStim = 0;
                    StimulusIndex++;
                   if (StimulusIndex < (ulong)StimSample.Length)
                    {
                        bufferIndex = StimSample[StimulusIndex] - numBuffLoadsCompleted * BUFFSIZE;
                        if (StimSample[StimulusIndex] < numBuffLoadsCompleted * BUFFSIZE)
                        {
                            //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                            throw new Exception("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    }
                    }
                    else
                    {
                        bufferIndex = BUFFSIZE;
                    }
                    
                }
            }
            #endregion

            numBuffLoadsCompleted++; 
        }
        
        //lets see if we can simplify things here...
        internal void PopulateBufferAppending()
        {
            lock (this)
            {
                
                //tickTime = DateTime.Now;
                //tickDiff = tickTime.Subtract(startTime);
                //Console.WriteLine(Convert.ToString(tickDiff.TotalMilliseconds) + ": populate buffer started...");
                
                //Stopwatch ws = new Stopwatch();
                //ws.Start();
                //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                //clear buffers and reset index
                AnalogBuffer = new double[NumAOChannels, BUFFSIZE]; // buffer for analog channels
                DigitalBuffer = new UInt32[BUFFSIZE]; // buffer for digital channels
                bufferIndex = 0;
                // MessageBox.Show("cleared buffer");
                //current stim- stimulus we are in the middle of.  if null, not yet stimming.  
                //dont load a stim to current stim until you actually start stimulating with it
                //empty array- nothing to stim with.



                //are we in the middle of a stimulus?  if so, finish as much as you can

                if (currentStim != null)
                {
                    //   MessageBox.Show("examining first stim");
                    bool finished = ApplyCurrentStimulus();
                    if (finished)
                    {
                        FinishStim();
                    }

                }
                //  MessageBox.Show("cleared first stim check");
                //at this point, we have either finished a stimulus, or finished the buffer.
                //therefore, if there is room left in this buffer, find the next stimulus and move to it.

                while (bufferIndex < BUFFSIZE & outerbuffer.Count > 0)
                {
                    //is next stimulus within range of this buffload?
                    bool ready = NextStimulusAppending();

                    if (ready)
                    {
                        bool finished = ApplyCurrentStimulus();
                        if (finished)
                        {
                            FinishStim();
                        }
                    }
                }

                //congratulations!  we finished the buffer!
                numBuffLoadsCompleted++;
                currentSample = numBuffLoadsCompleted * BUFFSIZE;
                // Check if protocol is completed
                if (numBuffLoadsCompleted >= numBuffLoadsRequired)
                {
                    running = false; // Start clean up cascade
                }

                OnBufferLoad(EventArgs.Empty);

                //ws.Stop();
                //Console.WriteLine("Buffer load took " + ws.Elapsed);
            }
        }

        internal void Append(ulong[] TimeVector, int[] ChannelVector, double[,] WaveMatrix)
        {

            //needs to include PreCompute stuff!  ie, convert to stimsample, analog encode, etc

            //okay, passed the tests, start appending
            Stopwatch sw = new Stopwatch();
            sw.Start();
            StimulusOutEvent stim;
            Console.WriteLine("Append started...");
            lock (this)
            {
                for (int i = 0; i < WaveMatrix.GetLength(0); i++)
                {
                    double[] wave = new double[WaveMatrix.GetLength(1)];
                    //this.TimeVector[outerIndexWrite] = TimeVector[i];
                    for (int j = 0; j < WaveMatrix.GetLength(1); j++)
                    {
                        wave[j] = WaveMatrix[i, j];
                    }
                    // MessageBox.Show("finished a wave");
                    // double[] w = {1.0,1.0};
                    // stim = new StimulusData(1,1.0,w);
                    stim = new StimulusOutEvent(ChannelVector[i], TimeVector[i], wave);

                    //  MessageBox.Show("created a stim");
                   
                    //  MessageBox.Show("calc'd the index");

                    outerbuffer.Add(stim);
                    Console.Write(stim.sampleIndex + ",");
                    // MessageBox.Show("added it");
                }
            }
            sw.Stop();
            Console.WriteLine("Append took " + sw.Elapsed);
            //  MessageBox.Show("finished Append");

        }

        override internal void Append(List<StimulusOutEvent> stimlist)
        {
            lock (this)
            {
                outerbuffer.AddRange(stimlist);
            }
        }
        
        //write as much of the current stimulus as possible
        //agnostic as to whether or not you've finished this stimulus or not.
        //returns if finished the stimulus or not
        internal bool ApplyCurrentStimulus()
        {

           //how many samples should we write, including blanking?
            ulong samples2Finish = (ulong)currentStim.waveform.Length+NUM_SAMPLES_BLANKING*2 - NumSampWrittenForCurrentStim;

            //write samples to the buffer
            for (ulong i = 0; (i < samples2Finish) & (bufferIndex < BUFFSIZE - 1); i++)
            {
                WriteSample();
            }

            return (bufferIndex < BUFFSIZE);
        }
        
        //examines the next stimulus, determines if it is within range, and loads it if it is
        //returns if stimulus is within range or not
        internal bool NextStimulusAppending()
        {
            lock (this)
            {
                if (outerbuffer.ElementAt(0).sampleIndex < (numBuffLoadsCompleted + 1) * BUFFSIZE)
                {

                    currentStim = new StimulusOutEvent(outerbuffer.ElementAt(0).channel, outerbuffer.ElementAt(0).sampleIndex, outerbuffer.ElementAt(0).waveform);
                    outerbuffer.RemoveAt(0);
                  //  Console.Write("starting stim at " + currentStim.time);
                    

                    if (outerbuffer.Count == (queueThreshold - 1))
                        OnThreshold(EventArgs.Empty);

                    NumSampWrittenForCurrentStim = 0;
                    bufferIndex = currentStim.sampleIndex - numBuffLoadsCompleted * BUFFSIZE;//move to beginning of this stimulus
                    if (currentStim.sampleIndex < numBuffLoadsCompleted * BUFFSIZE)//check to make sure we aren't attempting to stimulate in the past
                    {
                        //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + currentStim.StimSample + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentStim.channel);
                        throw new Exception("trying to write an expired stimulus: stimulation at sample no " + currentStim.sampleIndex + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentStim.channel);
                    }

                    return true;
                }
                else
                {
                    
                    currentStim = null;//we aren't currently stimulating
                    
                    NumSampWrittenForCurrentStim = 0;//we haven't written anything
                    bufferIndex = BUFFSIZE;//we are done with this buffer
                    return false;
                }
            }
        }

        internal void WriteSample()
        {
            CalculateAnalogPointAppending(NumSampWrittenForCurrentStim, NumAOChannels);
            CalculateDigPointAppending(NumSampWrittenForCurrentStim);
            AnalogBuffer[0 + RowOffset, (int)bufferIndex] = AnalogPoint[0];
            AnalogBuffer[1 + RowOffset, (int)bufferIndex] = AnalogPoint[1];
            DigitalBuffer[(int)bufferIndex] = DigitalPoint;
            NumSampWrittenForCurrentStim++;
            bufferIndex++;

        }

        internal void FinishStim()
        {
           // Console.Write("stim at " + currentStim.time + ",");
                currentStim = null;//we aren't currently stimulating
            
        }

        internal void CalculateLoadsRequired(double finalStimTime)
        {
            //How many buffer loads will this stimulus task take? 3 extra are for (1) Account for delay in start that might push
            //last stimulus overtime by a bit and 2 loads to zero out the double buffer.
            numBuffLoadsRequired = 4 + (uint)Math.Ceiling((double)(STIM_SAMPLING_FREQ*finalStimTime / (double)BUFFSIZE));
        }

        // Methods to calculate digital and alalog points to send to daq based on required channel and timing
        internal void CalculateDigPoint(ulong StimulusIndex, uint NumSampLoadedForCurr)
        {

            //Get the digital encoding for this stimulus
            if (NumSampLoadedForCurr < NUM_SAMPLES_BLANKING || NumSampLoadedForCurr > NUM_SAMPLES_BLANKING + WaveLength)
            {
                DigitalPoint = DigitalEncode[0, StimulusIndex];

            }
            else if (NumSampLoadedForCurr == NUM_SAMPLES_BLANKING || NumSampLoadedForCurr == NUM_SAMPLES_BLANKING + WaveLength)
            {
                DigitalPoint = DigitalEncode[1, StimulusIndex];
            }
            else
            {
                DigitalPoint = DigitalEncode[2, StimulusIndex];
            }

        }

        internal void CalculateAnalogPoint(ulong StimulusIndex, uint NumSampLoadedForCurr, int NumAOChannels)
        {

            //Get the analog encoding for this stimulus
            if (NumSampLoadedForCurr < (NUM_SAMPLES_BLANKING + 1))
            {
                AnalogPoint[0] = 0;
                AnalogPoint[1] = 0;
            }
            if (NumSampLoadedForCurr >= NUM_SAMPLES_BLANKING + 1 + WaveLength)
            {
                AnalogPoint[0] = 0;
                AnalogPoint[1] = 0;
            }
            // If actually during a stimulus
            if (NumSampLoadedForCurr >= NUM_SAMPLES_BLANKING + 1 && NumSampLoadedForCurr < NUM_SAMPLES_BLANKING + 1 + WaveLength)
            {
                NumSamplesLoadedForWave = NumSampLoadedForCurr - 1 - NUM_SAMPLES_BLANKING;

                if (NumSamplesLoadedForWave < 20)
                {
                    AnalogPoint[0] = WaveMatrix[StimulusIndex, NumSamplesLoadedForWave];
                    AnalogPoint[1] = AnalogEncode[0, StimulusIndex];
                }
                else if (NumSamplesLoadedForWave < 40)
                {
                    AnalogPoint[0] = WaveMatrix[StimulusIndex, NumSamplesLoadedForWave];
                    AnalogPoint[1] = AnalogEncode[1, StimulusIndex];
                }
                else if (NumSamplesLoadedForWave < 60)
                {
                    AnalogPoint[0] = WaveMatrix[StimulusIndex, NumSamplesLoadedForWave];
                    AnalogPoint[1] = 0;
                }
                else if (NumSamplesLoadedForWave < 80)
                {
                    AnalogPoint[0] = WaveMatrix[StimulusIndex, NumSamplesLoadedForWave];
                    AnalogPoint[1] = ((double)(WaveLength) / 100.0 > 10.0 ? -1 : (double)(WaveLength) / 100.0);
                }
                else
                {
                    AnalogPoint[0] = WaveMatrix[StimulusIndex, NumSamplesLoadedForWave];
                    AnalogPoint[1] = 0;
                }
            }
        }

        //appending versions
        internal void CalculateDigPointAppending(uint NumSampLoadedForCurr)
        {
            uint wavelength = (uint)currentStim.waveform.Length;
            if (NumSampLoadedForCurr < NUM_SAMPLES_BLANKING || NumSampLoadedForCurr > NUM_SAMPLES_BLANKING + wavelength)
            {
                DigitalPoint = currentStim.DigitalEncode[0];
            }
            else if (NumSampLoadedForCurr == NUM_SAMPLES_BLANKING || NumSampLoadedForCurr == NUM_SAMPLES_BLANKING + wavelength)
            {
                DigitalPoint = currentStim.DigitalEncode[1];
            }
            else
            {
                DigitalPoint = currentStim.DigitalEncode[2];
            }

        }

        internal void CalculateAnalogPointAppending(uint NumSampLoadedForCurr, int NumAOChannels)
        {
            uint WaveLength = (uint)currentStim.waveform.Length;

            //Get the analog encoding for this stimulus
            // Case when we are in blanking before a stimulus
            if (NumSampLoadedForCurr < (NUM_SAMPLES_BLANKING + 1))
            {
                AnalogPoint[0] = 0;
                AnalogPoint[1] = 0;
            }
            // Case when we are in blanking after stimulus
            if (NumSampLoadedForCurr >= NUM_SAMPLES_BLANKING + 1 + WaveLength)
            {
                AnalogPoint[0] = 0;
                AnalogPoint[1] = 0;
            }
            // OK, we actually need to load the buffer with some meat
            if (NumSampLoadedForCurr >= NUM_SAMPLES_BLANKING + 1 && NumSampLoadedForCurr < NUM_SAMPLES_BLANKING + 1 + WaveLength)
            {
                //how much has been loaded so far?
                NumSamplesLoadedForWave = NumSampLoadedForCurr - 1 - NUM_SAMPLES_BLANKING;

                //what is the current analog value to be sent to the buffer?
                double voltageOut;
                if (NumSamplesLoadedForWave < WaveLength)
                    voltageOut = currentStim.waveform[NumSamplesLoadedForWave];
                else
                    voltageOut = 0;
                
                // NeuroWronger's crazy analog simulus encoding scheme
                if (NumSamplesLoadedForWave < 20)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = currentStim.AnalogEncode[0];
                }
                else if (NumSamplesLoadedForWave < 40)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = currentStim.AnalogEncode[1];
                }
                else if (NumSamplesLoadedForWave < 60)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = 0;
                }
                else if (NumSamplesLoadedForWave < 80)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = ((double)(WaveLength) / 100.0 > 10.0 ? -1 : (double)(WaveLength) / 100.0);
                }
                else
                {
                    AnalogPoint[0] = currentStim.waveform[NumSamplesLoadedForWave]; 
                    AnalogPoint[1] = 0;
                }

            }

        }

        //appending tools
        public int StimuliInQueue()
        {
            return outerbuffer.Count();
        }

        public double GetTime()
        {
            return (double)((stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
        }

        public uint GetBufferSize()
        {
            return BUFFSIZE;
        }

        private void OnThreshold(EventArgs e)
        {
            if (QueueLessThanThreshold != null)
                QueueLessThanThreshold(this, e);
        }

        private void OnBufferLoad(EventArgs e)
        {
            if (DAQLoadCompleted != null)
                DAQLoadCompleted(this, e);
        }

        #region MUX conversion Functions
        internal static UInt32 channel2MUX(double channel)
        {
            if (Properties.Settings.Default.ChannelMapping == "invitro")
                channel = MEAChannelMappings.ch2stimChannel[(short)(--channel)];

            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8: return _channel2MUX_8chMUX(channel, true, true);
                case 16: return _channel2MUX_16chMUX(channel, true, true);
                default: return 0; //Error!
            }
        }

        internal static UInt32 channel2MUX_noEN(double channel)
        {
            if (Properties.Settings.Default.ChannelMapping == "invitro")
                channel = MEAChannelMappings.ch2stimChannel[(short)(--channel)];

            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8: return _channel2MUX_8chMUX(channel, false, false);
                case 16: return _channel2MUX_16chMUX(channel, false, false);
                default: return 0; //Error!
            }
        }

        internal static UInt32 channel2MUX(double channel, bool enable, bool trigger)
        {
            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8: return _channel2MUX_8chMUX(channel, enable, trigger);
                case 16: return _channel2MUX_16chMUX(channel, enable, trigger);
                default: return 0; //Error!
            }
        }


        //Convert channel number into control signal for deMUX
        private static UInt32 _channel2MUX_16chMUX(double channel, bool enable, bool trigger) //'channel' is 1-based
        {
            bool[] data = new bool[32];

            int portOffset = (Properties.Settings.Default.StimPortBandwidth == 32 ? PORT_OFFSET_32bitPort : PORT_OFFSET_8bitPort);
            if (enable)
            {
                //Pick which mux to use
                double tempDbl = (channel - 1) / 16.0;
                int tempInt = (int)Math.Floor(tempDbl);
                data[portOffset + 5 + tempInt] = true;
            }
            if (trigger)
                data[portOffset] = true;  //Always true, since this is start trigger for AO

            channel = 1 + ((channel - 1) % 16.0);
            if (channel / 8.0 > 1) { data[portOffset + 4] = true; channel -= 8; }
            if (channel / 4.0 > 1) { data[portOffset + 3] = true; channel -= 4; }
            if (channel / 2.0 > 1) { data[portOffset + 2] = true; channel -= 2; }
            if (channel / 1.0 > 1) { data[portOffset + 1] = true; }

            int blankingBit = (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort);
            data[blankingBit] = true; //Always true, since this is the "something's happening" signal

            UInt32 temp = 0;
            for (int p = 0; p < 32; ++p)
                temp += (UInt32)Math.Pow(2, p) * Convert.ToUInt32(data[p]);

            return temp;
        }

        private static UInt32 _channel2MUX_8chMUX(double channel, bool enable, bool trigger) //'channel' is 1-based
        {
            bool[] data = new bool[32];

            int portOffset = (Properties.Settings.Default.StimPortBandwidth == 32 ? PORT_OFFSET_32bitPort : PORT_OFFSET_8bitPort);
            if (enable)
            {
                //Pick which mux to use
                double tempDbl = (channel - 1) / 8.0;
                int tempInt = (int)Math.Floor(tempDbl);
                data[portOffset + 4 + tempInt] = true;
            }
            if (trigger)
                data[portOffset] = true;  //Always true, since this is start trigger for AO

            channel = 1 + ((channel - 1) % 8.0);

            if (channel / 4.0 > 1) { data[portOffset + 3] = true; channel -= 4; }
            if (channel / 2.0 > 1) { data[portOffset + 2] = true; channel -= 2; }
            if (channel / 1.0 > 1) { data[portOffset + 1] = true; }

            int blankingBit = (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort);
            data[blankingBit] = true; //Always true, since this is the "something's happening" signal

            UInt32 temp = 0;
            for (int p = 0; p < 32; ++p)
                temp += (UInt32)Math.Pow(2, p) * Convert.ToUInt32(data[p]);

            return temp;
        }

        internal static Byte[] convertTo8Bit(UInt32[] data)
        {
            Byte[] output = new Byte[data.Length];
            for (int i = 0; i < data.Length; ++i) output[i] = (Byte)data[i];
            return output;
        }
        #endregion MUX conversion Functions

    }

}
