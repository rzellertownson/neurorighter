using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace NeuroRighter
{
    public class StimBuffer
    {
        // Public Properties
        public ulong BufferIndex = 0;
        public double[,] AnalogBuffer;
        public UInt32[] DigitalBuffer;
        public bool StillWritting = false;
        public ulong NumBuffLoadsCompleted = 0;
        public uint NumBuffLoadsRequired;
        

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

        //RTZT3 appending edits
       
        private uint SamplesPerStim; //waveform length
        //private uint outerIndexRead; //current location in the outer buffer that we are reading from
        private List<StimulusData> outerbuffer;
        private StimulusData currentStim;
        private BackgroundWorker bw_stimbuffer; //handles reads and writes to the inner (waveform) buffer, the parent thread handles reads and writes to the outer (stimulus) buffer
        public bool running =false;

        //background worker requires the DAQ constructs so that it can encapsulate the asynchronous stimulation task
        AnalogMultiChannelWriter stimAnalogWriter;
        DigitalSingleChannelWriter stimDigitalWriter;
        Task stimDigitalTask, stimAnalogTask;
        

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
        //constructor if using stim buffer in append mode
       
        //constructor if using stim buffer in append mode- with lists!
        internal StimBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING)
        {
            //this.SamplesPerStim = (uint)SamplesPerStim;
           // this.outerbufferSize = (uint)OUTERBUFFSIZE;
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.NUM_SAMPLES_BLANKING = (uint)NUM_SAMPLES_BLANKING;
            //this.WaveMatrix = new double[OUTERBUFFSIZE + 1, SamplesPerStim];
            //this.ChannelVector = new int[OUTERBUFFSIZE + 1];
            //PRECOMPUTE STUFF
            // initialize the outer buffer- we don't use the this.timevec, channelvec, or wavemat, just load directly into the vectors below:
            //StimSample = new ulong[outerbufferSize + 1];//buffer size is plus one, such that if the read and write indices are at the same point, the buffer is empty, while at the same time providing the specified effective buffer size
           

          //  WaveLength = (uint)SamplesPerStim;
          //  StimulusLength = (uint)(WaveLength + 2 * NUM_SAMPLES_BLANKING + 2); //length of waveform + padding on either side due to digital signaling.

            

            // What are the buffer offset settings for this system?
            NumAOChannels = 2;
            RowOffset = 0;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
        {
                NumAOChannels = 4;
                RowOffset = 2;
            }

            //
            outerbuffer = new List<StimulusData>();

            //create background worker, which will run in a second thread to load up stimuli into the outer buffer
            bw_stimbuffer = new BackgroundWorker();
            bw_stimbuffer.DoWork += new DoWorkEventHandler(bw_stimbuffer_DoWork);
            bw_stimbuffer.WorkerSupportsCancellation = true;
        }

        internal void append(double[] TimeVector, int[] ChannelVector, double[,] WaveMatrix)
        {

            //needs to include precompute stuff!  ie, convert to stimsample, analog encode, etc
             
            //okay, passed the tests, start appending
            StimulusData stim;
            
            for (int i = 0; i < WaveMatrix.GetLength(1); i++)
            {
                double[] wave = new double[WaveMatrix.GetLength(0)];
                //this.TimeVector[outerIndexWrite] = TimeVector[i];
                for (int j = 0; j < WaveMatrix.GetLength(0); j++)
                {
                    wave[j] = WaveMatrix[j, i];
                }
               // MessageBox.Show("finished a wave");
               // double[] w = {1.0,1.0};
               // stim = new StimulusData(1,1.0,w);
                stim = new StimulusData(ChannelVector[i], TimeVector[i], wave);
                
              //  MessageBox.Show("created a stim");
                stim.calcIndex(STIM_SAMPLING_FREQ);
              //  MessageBox.Show("calc'd the index");
                outerbuffer.Add(stim);
               // MessageBox.Show("added it");

                

               
            }
          //  MessageBox.Show("finished append");
            
        }
        internal void append(List<StimulusData> stimlist)
        {
            foreach(StimulusData stim in stimlist)
            {
                stim.calcIndex(STIM_SAMPLING_FREQ);
            }
            outerbuffer.AddRange(stimlist);
        }
        
        internal void start(AnalogMultiChannelWriter stimAnalogWriter, DigitalSingleChannelWriter stimDigitalWriter, Task stimDigitalTask, Task stimAnalogTask)//, ulong starttime)
        {
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalTask = stimDigitalTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
            //calculate what time stimulation is starting at- start is controlled by user, so can't anticipate that
            // it is synched to the overall start
            //starttime is in microseconds


         //   NumBuffLoadsCompleted = starttime / (BUFFSIZE * 1000000 / STIM_SAMPLING_FREQ);
          //  BufferIndex = starttime % (BUFFSIZE * 1000000 / STIM_SAMPLING_FREQ);


            bw_stimbuffer.RunWorkerAsync();
            running = true;
        }

        internal void stop()
        {
            bw_stimbuffer.CancelAsync();
        }

        //rather straightforward- wait until buffer clears, and then call populate buffer.  keep on doing so until cancelled.
        internal void bw_stimbuffer_DoWork(object sender, DoWorkEventArgs e)
        {
            int flag = 0;
            try
            {
                //Populate the 1st stimulus buffer
                flag++;
                populateBufferAppending();
                flag++;
                //Write Samples to the hardware buffer
                stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
                stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);
                flag++;
                //Populate the 2nd stimulus buffer
                populateBufferAppending();
                flag++;
                //Write Samples to the hardware buffer
                stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
                stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);
                flag++;
                stimDigitalTask.Start();
                stimAnalogTask.Start();
                flag++;
                ulong samplessent = 0;

                while (!bw_stimbuffer.CancellationPending)
                {
                    
                    //Populate the stimulus buffer
                    populateBufferAppending();
                    flag = 1000;
                    // Wait for space to open in the buffer
                    samplessent = (ulong)stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                    flag++;
                    while (((NumBuffLoadsCompleted - 1) * BUFFSIZE - samplessent > BUFFSIZE) && !bw_stimbuffer.CancellationPending)
                    {
                        samplessent = (ulong)stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                    }
                    flag++;
                    if (bw_stimbuffer.CancellationPending) break;
                    //Write Samples to the hardware buffer
                    stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
                    stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);
                    flag++;
                }
                stimAnalogTask.Stop();
                stimDigitalTask.Stop();

                running = false;
            }
            catch (Exception me)
            {
                MessageBox.Show("stim buffer (append mode) error:  please close NeuroRighter after saving your data. flag:"+flag +". "+ me.Message,  "stimbuffer exception thrown");
            }
            }

      

        internal void precompute()//not used for appending- instead, append needs to perform these actions
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
            NumBuffLoadsRequired = 3 + (uint)Math.Ceiling((double)(StimSample[StimSample.Length - 1] / BUFFSIZE));
           
        }
        
        internal void validateStimulusParameters()
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

            // Check to make sure StimulusLength > BuffSize    
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
        internal void populateBuffer()
        {
            //clear buffers and reset index
            AnalogBuffer = new double[NumAOChannels, BUFFSIZE]; // buffer for analog channels
            DigitalBuffer = new UInt32[BUFFSIZE]; // buffer for digital channels
            BufferIndex = 0;

            #region Populate the buffers if a stimulus occurs in this particular buffer length

            // Finish up writting the stimulus from the last buffer load if you didn't finish then
            if (NumSampWrittenForCurrentStim < StimulusLength && NumSampWrittenForCurrentStim != 0)
            {
                uint Samples2Finish = StimulusLength - NumSampWrittenForCurrentStim;
                for (int i = 0; i < Samples2Finish; i++)
                {
                    calculateAnalogPoint(StimulusIndex, NumSampWrittenForCurrentStim, NumAOChannels);
                    calculateDigPoint(StimulusIndex, NumSampWrittenForCurrentStim);
                    AnalogBuffer[0 + RowOffset, (int)BufferIndex] = AnalogPoint[0];
                    AnalogBuffer[1 + RowOffset, (int)BufferIndex] = AnalogPoint[1];
                    DigitalBuffer[(int)BufferIndex] = DigitalPoint;
                    NumSampWrittenForCurrentStim++;
                    BufferIndex++;
                }

                // Finished writting current stimulus, reset variables
                NumSampWrittenForCurrentStim = 0;
                StimulusIndex++;
                BufferIndex = StimSample[StimulusIndex] - NumBuffLoadsCompleted * BUFFSIZE;
                if (StimSample[StimulusIndex] < NumBuffLoadsCompleted * BUFFSIZE)
                {
                    //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " +NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    throw new Exception ("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " +NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
            }
            }
            else
            {
                if (StimulusIndex < (ulong)StimSample.Length)
                    {
                        BufferIndex = StimSample[StimulusIndex] - NumBuffLoadsCompleted * BUFFSIZE;
                        if (StimSample[StimulusIndex] < NumBuffLoadsCompleted * BUFFSIZE)
                        {
                           // MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                            throw new Exception("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    }
                    }
                    else
                    {
                        BufferIndex = BUFFSIZE;
                    }
            }

            // Write to the buffer if the next stimulus is within this buffer's time span
            while(BufferIndex < BUFFSIZE  && BufferIndex >= 0)
            {
                if (NumSampWrittenForCurrentStim < StimulusLength)
                {
                    calculateAnalogPoint(StimulusIndex, NumSampWrittenForCurrentStim, NumAOChannels);
                    calculateDigPoint(StimulusIndex, NumSampWrittenForCurrentStim);
                    AnalogBuffer[0 + RowOffset, (int)BufferIndex] = AnalogPoint[0];
                    AnalogBuffer[1 + RowOffset, (int)BufferIndex] = AnalogPoint[1];
                    DigitalBuffer[(int)BufferIndex] = DigitalPoint;
                    NumSampWrittenForCurrentStim++;
                    BufferIndex++;
                }
                else
                {
                    // Finished writting current stimulus, reset variables
                    NumSampWrittenForCurrentStim = 0;
                    StimulusIndex++;
                   if (StimulusIndex < (ulong)StimSample.Length)
                    {
                        BufferIndex = StimSample[StimulusIndex] - NumBuffLoadsCompleted * BUFFSIZE;
                        if (StimSample[StimulusIndex] < NumBuffLoadsCompleted * BUFFSIZE)
                        {
                            //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                            throw new Exception("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    }
                    }
                    else
                    {
                        BufferIndex = BUFFSIZE;
                    }
                    
                }
            }
            #endregion

            NumBuffLoadsCompleted++; 
        }
        
        //lets see if we can simplify things here...
        internal void populateBufferAppending()
        {
            //clear buffers and reset index
            AnalogBuffer = new double[NumAOChannels, BUFFSIZE]; // buffer for analog channels
            DigitalBuffer = new UInt32[BUFFSIZE]; // buffer for digital channels
            BufferIndex = 0;
           // MessageBox.Show("cleared buffer");
            //current stim- stimulus we are in the middle of.  if null, not yet stimming.  
            //dont load a stim to current stim until you actually start stimulating with it
            //empty array- nothing to stim with.



            //are we in the middle of a stimulus?  if so, finish as much as you can

            if (currentStim != null)
        {
             //   MessageBox.Show("examining first stim");
                bool finished = applyCurrentStimulus();
                if (finished)
                {
                    finishStim();
                }

            }
          //  MessageBox.Show("cleared first stim check");
            //at this point, we have either finished a stimulus, or finished the buffer.
            //therefore, if there is room left in this buffer, find the next stimulus and move to it.

            while (BufferIndex < BUFFSIZE & outerbuffer.Count > 0)
            {
                //is next stimulus within range of this buffload?
                bool ready = nextStimulusAppending();

                if (ready)
                {
                    bool finished = applyCurrentStimulus();
                    if (finished)
                    {
                        finishStim();
                    }
                }

            }

            //congratulations!  we finished the buffer!
            NumBuffLoadsCompleted++; 

        }

        //write as much of the current stimulus as possible
        //agnostic as to whether or not you've finished this stimulus or not.
        //returns if finished the stimulus or not
        internal bool applyCurrentStimulus()
        {
            bool finishedIt;

            //how many samples should we write, including blanking?
            ulong Samples2Finish = (ulong)currentStim.waveform.Length+NUM_SAMPLES_BLANKING*2 - NumSampWrittenForCurrentStim;

            //how many samples can we write?
            ulong SamplesAvailable = BUFFSIZE - BufferIndex;

            ulong Samples2Write;
            if (SamplesAvailable >= Samples2Finish)//have enough room to finish stimulus
            {
                Samples2Write = Samples2Finish;
                finishedIt = true;
            }
            else//do not have enough room to finish stimulus
            {
                Samples2Write = SamplesAvailable;
                finishedIt = false;
            }

            //write samples to the buffer
            for (ulong i = 0; i < Samples2Write; i++)
            {
                writeSample();
            }

            return finishedIt;
        }

        //examines the next stimulus, determines if it is within range, and loads it if it is
        //returns if stimulus is within range or not
        internal bool nextStimulusAppending()
        {
            if (outerbuffer.ElementAt(0).StimSample < (NumBuffLoadsCompleted + 1) * BUFFSIZE)
            {

                currentStim = outerbuffer.ElementAt(0);
                outerbuffer.RemoveAt(0);
                //WaveMatrix = currentStim.waveform;
                NumSampWrittenForCurrentStim = 0;
                BufferIndex = currentStim.StimSample - NumBuffLoadsCompleted * BUFFSIZE;//move to beginning of this stimulus
                if (currentStim.StimSample < NumBuffLoadsCompleted * BUFFSIZE)//check to make sure we aren't attempting to stimulate in the past
                {
                    //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + currentStim.StimSample + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentStim.channel);
                    throw new Exception("trying to write an expired stimulus: stimulation at sample no " + currentStim.StimSample + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentStim.channel);
                }
                return true;
            }
            else
            {
                currentStim = null;//we aren't currently stimulating
                NumSampWrittenForCurrentStim = 0;//we haven't written anything
                BufferIndex = BUFFSIZE;//we are done with this buffer
                return false;
            }

        }

        internal void writeSample()
        {
            calculateAnalogPointAppending(NumSampWrittenForCurrentStim, NumAOChannels);
            
            calculateDigPointAppending(NumSampWrittenForCurrentStim);




            AnalogBuffer[0 + RowOffset, (int)BufferIndex] = AnalogPoint[0];
            AnalogBuffer[1 + RowOffset, (int)BufferIndex] = AnalogPoint[1];
            DigitalBuffer[(int)BufferIndex] = DigitalPoint;
            NumSampWrittenForCurrentStim++;
            BufferIndex++;

        }

        internal void finishStim()
        {
            currentStim = null;
        }

        internal void calculateDigPoint(ulong StimulusIndex, uint NumSampLoadedForCurr)
        {

            //Get the digital encoding for this stimulus
            if (NumSampLoadedForCurr < NUM_SAMPLES_BLANKING  || NumSampLoadedForCurr > NUM_SAMPLES_BLANKING + WaveLength)
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


        
        internal void calculateAnalogPoint(ulong StimulusIndex, uint NumSampLoadedForCurr, int NumAOChannels)
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
        internal void calculateDigPointAppending(uint NumSampLoadedForCurr)
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

        internal void calculateAnalogPointAppending(uint NumSampLoadedForCurr, int NumAOChannels)
        {
            uint WaveLength = (uint)currentStim.waveform.Length;
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
        public int stimuliInQueue()
        {
            return outerbuffer.Count();
        }

        public double time()
        {
            return (double)((stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
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
