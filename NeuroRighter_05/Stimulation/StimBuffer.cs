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
        public ulong BufferIndex = 0; //Incex for inner buffer
        public uint StimulusIndex = 0; // Index for outer buffer
        public double[,] AnalogBuffer; //actual values written to the analog task
        public UInt32[] DigitalBuffer; //ditto for digital task
        public bool StillWritting = false;
        public ulong NumBuffLoadsCompleted = 0;
        public uint NumBuffLoadsRequired;
        

        // Private Properties
        private uint WaveLength;//length of all the waveforms
        private uint StimulusLength;//total lenght of stimulus, including blanking
        private uint NumSampWrittenForCurrentStim = 0;  //how far are we in the current stimulus?
        private uint NumSamplesLoadedForWave = 0;
        private uint STIM_SAMPLING_FREQ;
        private uint NUM_SAMPLES_BLANKING;
        private ulong[] StimSample; //record of when stimulations start, in units of samples
        private double[,] AnalogEncode;
        private UInt32[,] DigitalEncode;
        private double[] AnalogPoint = new double[] {0, 0};
        private UInt32 DigitalPoint;
        private double TriggerPoint;


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

        //RTZT3 recording edits
        private int[] recordTimes; //when does each recording begin, in ms?
        private int[] recordDurations; //how long is each recording, in ms?
        private uint NumSampWrittenForCurrentRecord = 0; //how far are we into the current recording?
        private uint RecordingLength; //how long is this recording going to be (in samples)?
        public uint RecordIndex = 0; // Index for the outer buffer
        private ulong[] RecSample; //how long are all of the recordings, in samples?
        private uint outerRecordWrite; //where are we writing in the record buffer?

        //RTZT3 appending edits
        private uint outerbufferSize; //the number of stimuli that can be loaded into memory at once
        private uint SamplesPerStim; //waveform length
        //private uint outerIndexRead; //current location in the outer buffer that we are reading from
        private uint outerIndexWrite; //where we are writing to in the outer buffer
        private bool isAppending; //are we in append mode or one-shot mode?
        private BackgroundWorker bw_stimbuffer; //handles reads and writes to the inner (waveform) buffer, the parent thread handles reads and writes to the outer (stimulus) buffer
        public bool running =false;

        //background worker requires the DAQ constructs so that it can encapsulate the asynchronous stimulation task
        AnalogMultiChannelWriter stimAnalogWriter;
        DigitalSingleChannelWriter stimDigitalWriter;
        Task stimDigitalTask, stimAnalogTask;
        

        //Constructor to create a Stim Buffer object for use by File2Stim (one-shot mode, no recording)
        internal StimBuffer(int[] TimeVector, int[] ChannelVector, double[,] WaveMatrix, int LengthWave,
            int BUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING)
            
                     :this(TimeVector, ChannelVector, WaveMatrix, null, null, LengthWave,
                  BUFFSIZE, STIM_SAMPLING_FREQ, NUM_SAMPLES_BLANKING)
        { }
        

        //Constructor to create a Stim Buffer object for use by File2Stim (one-shot mode, recording)
        internal StimBuffer(int[] TimeVector, int[] ChannelVector, double[,] WaveMatrix, int[] recordTimes, int[] recordDurations, 
                int LengthWave,int BUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING)
        {
            this.TimeVector = TimeVector;
            this.ChannelVector = ChannelVector;
            this.WaveMatrix = WaveMatrix;
            this.BUFFSIZE = (uint)BUFFSIZE;
            this.LengthWave = (uint)LengthWave;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.NUM_SAMPLES_BLANKING = (uint)NUM_SAMPLES_BLANKING;
            this.recordTimes = recordTimes;
            this.recordDurations = recordDurations;
            isAppending = false;

        }
        //constructor if using stim buffer in append mode
        internal StimBuffer(int OUTERBUFFSIZE, int SamplesPerStim, int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING)
        {
            this.SamplesPerStim = (uint)SamplesPerStim;
            this.outerbufferSize = (uint)OUTERBUFFSIZE;
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.NUM_SAMPLES_BLANKING = (uint)NUM_SAMPLES_BLANKING;
            this.WaveMatrix = new double[OUTERBUFFSIZE+1, SamplesPerStim];
            this.ChannelVector = new int[OUTERBUFFSIZE+1];
            //PRECOMPUTE STUFF
            // initialize the outer buffer- we don't use the this.timevec, channelvec, or wavemat, just load directly into the vectors below:
            StimSample = new ulong[outerbufferSize+1];//buffer size is plus one, such that if the read and write indices are at the same point, the buffer is empty, while at the same time providing the specified effective buffer size
            AnalogEncode = new double[2, outerbufferSize+1];
            DigitalEncode = new UInt32[3, outerbufferSize+1];
            RecSample = new ulong[outerbufferSize + 1];

            WaveLength = (uint)SamplesPerStim;
            StimulusLength = (uint)(WaveLength + 2 * NUM_SAMPLES_BLANKING + 2); //length of waveform + padding on either side due to digital signaling.

            StimulusIndex = 0;
            outerIndexWrite = 0;
            isAppending = true;

            // What are the buffer offset settings for this system?
            NumAOChannels = 2;
            RowOffset = 0;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                NumAOChannels = 4;
                RowOffset = 2;
            }

            //create background worker, which will run in a second thread to load up stimuli into the outer buffer
            bw_stimbuffer = new BackgroundWorker();
            bw_stimbuffer.DoWork += new DoWorkEventHandler(bw_stimbuffer_DoWork);
            bw_stimbuffer.WorkerSupportsCancellation = true;

            //the numBuffloads variable does not apply to the append process


            //TODO: create locks for the outer buffer (writeIndex, spaces to be written to- can I lock only some of them?  which ones?)
        }

        #region appending methods

        internal void append(int[] TimeVector, int[] ChannelVector, double[,] WaveMatrix)
        {
           // MessageBox.Show("append 0");
            //needs to include precompute stuff!  ie, convert to stimsample, analog encode, etc
            if (WaveMatrix.GetLength(1) != this.WaveMatrix.GetLength(1))
                throw new Exception("attempting to append waveforms with " + WaveMatrix.GetLength(1) + " samples, the buffer is configured to use " + this.WaveMatrix.GetLength(1) + " samples");
            ulong available = availableBufferSpace();
            if (available<(ulong)WaveMatrix.GetLength(0))
                throw new Exception("outer buffer overflow: " + WaveMatrix.GetLength(0) + " stimuli were appended to the stimuli buffer, but only " + available + " spaces were available in the buffer");
            
            //okay, passed the tests, start appending
            //MessageBox.Show("append 1");
            for (int i = 0; i < WaveMatrix.GetLength(0); i++)
            {
                //this.TimeVector[outerIndexWrite] = TimeVector[i];
                StimSample[outerIndexWrite] = (uint)Math.Round((double)(TimeVector[i] * (STIM_SAMPLING_FREQ / 1000)));
                //MessageBox.Show("append 2");
                //this.ChannelVector[outerIndexWrite] = ChannelVector[i];
                AnalogEncode[0, outerIndexWrite] = Math.Ceiling((double)ChannelVector[i] / 8.0);
                AnalogEncode[1, outerIndexWrite] = (double)((ChannelVector[i] - 1) % 8) + 1.0;
                //MessageBox.Show("append 3");
                DigitalEncode[0, outerIndexWrite] = Convert.ToUInt32(Math.Pow(2, (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort)));
                DigitalEncode[1, outerIndexWrite] = channel2MUX_noEN((double)ChannelVector[i]);
                DigitalEncode[2, outerIndexWrite] = channel2MUX((double)ChannelVector[i]);
                this.ChannelVector[outerIndexWrite] = ChannelVector[i];
                for (int j = 0; j < this.WaveLength; j++)
                {
                    this.WaveMatrix[outerIndexWrite, j] = WaveMatrix[i, j];
                }
                 
                //RecSample[outerIndexWrite] = (uint)Math.Round((double)(recordTimes[i] * (STIM_SAMPLING_FREQ / 1000)));

                outerIndexWrite++;
                if (outerIndexWrite > outerbufferSize)
                    outerIndexWrite = 0;
            }
        }

        internal void appendRecord(int[] TimeVector, int[] duration)
        {
            throw new NotImplementedException();
        }

        internal void start(AnalogMultiChannelWriter stimAnalogWriter, DigitalSingleChannelWriter stimDigitalWriter, Task stimDigitalTask, Task stimAnalogTask)
        {
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalTask = stimDigitalTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
            bw_stimbuffer.RunWorkerAsync();
            running = true;
        }

        internal void stop()
        {
            bw_stimbuffer.CancelAsync();
        }

        internal void bw_stimbuffer_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                //Populate the 1st stimulus buffer

                populateBuffer();

                //Write Samples to the hardware buffer
                stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
                stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);

                //Populate the 2nd stimulus buffer
                populateBuffer();

                //Write Samples to the hardware buffer
                stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
                stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);

                stimDigitalTask.Start();
                stimAnalogTask.Start();
                ulong samplessent = 0;

                while (!bw_stimbuffer.CancellationPending)//stimulusbuffer.NumBuffLoadsCompleted < stimulusbuffer.NumBuffLoadsRequired)
                {
                    //if all is well, keep reading out
                    //  if (StimulusIndex != outerIndexWrite)
                    // {
                    //Populate the stimulus buffer
                    populateBuffer();

                    // Wait for space to open in the buffer
                    samplessent = (ulong)stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                    while (((NumBuffLoadsCompleted - 1) * BUFFSIZE - samplessent > BUFFSIZE) && !bw_stimbuffer.CancellationPending)
                    {
                        samplessent = (ulong)stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                    }
                    if (bw_stimbuffer.CancellationPending) break;
                    //Write Samples to the hardware buffer
                    stimAnalogWriter.WriteMultiSample(false, AnalogBuffer);
                    stimDigitalWriter.WriteMultiSamplePort(false, DigitalBuffer);
                    // }
                    // else
                    //we have emptied the buffer
                    // {
                    //we've reached the end of the buffer- should wait here until we get another stimulus

                    // }
                }
                stimAnalogTask.Stop();
                stimDigitalTask.Stop();

                running = false;
            }
            catch (Exception me)
            {
                MessageBox.Show("stim buffer error:  please close NeuroRighter after saving your data. " + me.Message, "stimbuffer exception thrown");
            }
            }

        public uint availableBufferSpace()
        {
            uint filled = (uint)(((int)outerIndexWrite - (int)StimulusIndex + (outerbufferSize + 1)) % ((int)(outerbufferSize + 1)));
            uint available = outerbufferSize - filled;
            

            return available;
        }

        #endregion

        internal void precompute()//should be the same for both appending and not, assuming that the first appended array is longer than the inner buffer
        {
           
                // Does as much pre computation of the buffers that will be populated as possible to prevent buffer load lag and resulting DAQ exceptions
                StimSample = new ulong[TimeVector.Length];
                AnalogEncode = new double[2, ChannelVector.Length];
                DigitalEncode = new UInt32[3, ChannelVector.Length];
            if (recordTimes !=null)
                RecSample = new ulong[recordTimes.Length];

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

                // Populate RecSample if recording
            if (recordTimes != null)
                for (int i = 0; i < RecSample.Length; i++)
                    RecSample[i] = (uint)Math.Round((double)(recordTimes[i] * (STIM_SAMPLING_FREQ / 1000)));

            
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
                NumBuffLoadsRequired = 3 + (uint)Math.Ceiling((double)((StimSample[StimSample.Length - 1]+StimulusLength) / BUFFSIZE));
            //possibly extend required loads if recording
            if (recordTimes != null)
            {
                ulong recsamplesneeded = RecSample[RecSample.Length - 1]+(ulong)(recordDurations[RecSample.Length - 1]* (STIM_SAMPLING_FREQ / 1000));
                uint recbuffloadsneeded = 3 + (uint)Math.Ceiling((double)( recsamplesneeded/ BUFFSIZE));
                NumBuffLoadsRequired = Math.Max(NumBuffLoadsRequired, recbuffloadsneeded);
           
            }
               
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

        internal void populateBuffer()
        {

            AnalogBuffer = new double[NumAOChannels, BUFFSIZE]; // buffer for analog channels
            DigitalBuffer = new UInt32[BUFFSIZE]; // buffer for digital channels
            BufferIndex = 0;

            // Populate the buffers if a stimulus occurs in this particular buffer length

            #region Finish up writing the stimulus from the last buffer load if you didn't finish then

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

                // Finished writing current stimulus, reset variables
                NumSampWrittenForCurrentStim = 0;

                //find next stimulus
                StimulusIndex++;
                if (isAppending &( StimulusIndex >= (ulong)StimSample.Length))//cycle through to beginning only if in appending mode
                    StimulusIndex = 0;

                //move the buffer to the next stimulus, if needed
                if (StimulusIndex < (ulong)StimSample.Length)//only attempt to look up a stimulus if you can
                {
                    BufferIndex = StimSample[StimulusIndex] - NumBuffLoadsCompleted * BUFFSIZE;
                    if (StimSample[StimulusIndex] < NumBuffLoadsCompleted * BUFFSIZE)
                    {
                        //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " +NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                        throw new Exception("trying to write an expired stimulus: stimulation at sample no " + StimSample[StimulusIndex] + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + ChannelVector[StimulusIndex]);
                    }
                }
            }
            else
            {
                //move to the next stimulus
                if (((isAppending) & (StimulusIndex != outerIndexWrite)) || 
                    ((!isAppending) & (StimulusIndex < StimSample.Length)))//if we haven't reached the end of the stimbuffer...
                    {
                        //move to the space in the inner buffer where the next stimulus begins
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
            #endregion

            #region Write to the buffer if the next stimulus is within this buffer's time span
            while (BufferIndex < BUFFSIZE  && BufferIndex >= 0)
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
                    if (isAppending & (StimulusIndex >= StimSample.Length))
                        StimulusIndex = 0;
                    
                    if (((isAppending) & (StimulusIndex != outerIndexWrite)) || ((!isAppending) & (StimulusIndex < StimSample.Length)))
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

        internal void addTriggerToBuffer()
        {
            //this code is designed to be run directly after populate buffer, and effectively performs the 
            //same tasks, but for the recording buffer.  That is, it edits analogbuffer to include the
            //record signal (trigger).  It draws these trigger signals from two vectors, one which contains
            //the trigger start times and one which contains the durations.

            //we just ran populate buffer- so numbuffloadscompleted has been incremented already.
            ulong NumBuffLoadsCompleted_d = NumBuffLoadsCompleted - 1;
            //dont clear the buffer- just add to analog[0]
            BufferIndex = 0;
#region are we in the middle of a recording?- if so, finish
            if (RecordIndex < recordDurations.Length)
                RecordingLength = (uint)recordDurations[RecordIndex] * STIM_SAMPLING_FREQ / 1000;
            else
                RecordingLength = 0;

            if (NumSampWrittenForCurrentRecord <= RecordingLength && NumSampWrittenForCurrentRecord != 0)
            {
                //MessageBox.Show("finishing one");
                ulong Samples2Finish = RecordingLength - NumSampWrittenForCurrentRecord;
                for (ulong i = 0; i < Samples2Finish; i++)
                {
                    AnalogBuffer[0,(int)BufferIndex] = 4.0;
                    NumSampWrittenForCurrentRecord++;
                    BufferIndex++;
                }
                //finished writing current record signal, reset variables
                NumSampWrittenForCurrentRecord = 0;
                RecordIndex++;
                
                //MessageBox.Show("wrote " + RecordIndex + " to buffer");
                if (isAppending & (RecordIndex >= (ulong)RecSample.Length))//cycle through to beginning if in appending mode
                    RecordIndex = 0;
                if (RecordIndex < (ulong)RecSample.Length)
                {
                    BufferIndex = RecSample[RecordIndex] - NumBuffLoadsCompleted_d * BUFFSIZE;
                    if (RecSample[RecordIndex] < NumBuffLoadsCompleted_d * BUFFSIZE)
                    {
                        throw new Exception("trying to record at some time in the past: recording number " + RecordIndex + " at time " + recordTimes[RecordIndex] + "ms was attempted at buffer load " + NumBuffLoadsCompleted_d + "at time " +
                            (NumBuffLoadsCompleted_d * BUFFSIZE * 1000 / STIM_SAMPLING_FREQ) + "ms");
                    }
                }
            }
            //move to the index of the next recording
            else
            {
                if (((isAppending) & (RecordIndex != outerRecordWrite)) || ((!isAppending) & (RecordIndex < RecSample.Length)))
                {
                    //go to next stimulus
                    BufferIndex = RecSample[RecordIndex] - NumBuffLoadsCompleted_d * BUFFSIZE;
                    //RecordingLength = recordDurations[RecordIndex] * STIM_SAMPLING_FREQ / 1000;
                    if (RecSample[RecordIndex] < NumBuffLoadsCompleted_d * BUFFSIZE)
                    {
                        throw new Exception("trying to record at some time in the past: recording number " + RecordIndex + " at time " + recordTimes[RecordIndex] + "ms was attempted at buffer load " + NumBuffLoadsCompleted_d + "at time " +
                         (NumBuffLoadsCompleted_d * BUFFSIZE * 1000 / STIM_SAMPLING_FREQ) + "ms, right here, and we already wrote " + NumSampWrittenForCurrentRecord);
                    }
                }
                else
                {
                    //go to end of the buffer
                    BufferIndex = BUFFSIZE;
                }
            }
#endregion


            #region find next recording region and move to it
            if (RecordIndex < recordDurations.Length)
            {
                //while still in this buffer
                while (BufferIndex < BUFFSIZE && BufferIndex >= 0)
                {
                    //MessageBox.Show("have started a stimulus at bufload " + NumBuffLoadsCompleted_d);
                    if (NumSampWrittenForCurrentRecord < recordDurations[RecordIndex] * STIM_SAMPLING_FREQ / 1000)
                    {
                        AnalogBuffer[0, (int)BufferIndex] = 4.0;
                        NumSampWrittenForCurrentRecord++;
                        BufferIndex++;
                    }
                    else
                    {
                        //finished writing that stimulus, reset vars
                        NumSampWrittenForCurrentRecord = 0;
                        RecordIndex++;
                        //MessageBox.Show("wrote " + RecordIndex + " to buffer");
                        if (isAppending & (RecordIndex >= (ulong)RecSample.Length))//cycle through to beginning if in appending mode
                            RecordIndex = 0;
                        //if (RecordIndex >= (ulong)RecSample.Length) return;
                        //move buffer to next index, or finish this buffer
                        if (((isAppending) & (RecordIndex != outerRecordWrite)) || ((!isAppending) & (RecordIndex < RecSample.Length)))
                        {
                            //go to next stimulus
                            BufferIndex = RecSample[RecordIndex] - NumBuffLoadsCompleted_d * BUFFSIZE;
                            if (RecSample[RecordIndex] < NumBuffLoadsCompleted_d * BUFFSIZE)
                            {
                                throw new Exception("trying to record at some time in the past: recording at time " + recordTimes[RecordIndex] + "ms was attempted at time " +
                                                       (NumBuffLoadsCompleted_d * BUFFSIZE * STIM_SAMPLING_FREQ / 1000) + "ms");
                            }
                        }
                        else
                        {
                            //go to end of the buffer
                            BufferIndex = BUFFSIZE;
                        }
                    }


                }
            }
            #endregion

            //populate takes care of incrementing the numbuffloads completed.

           
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
