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

namespace NeuroRighter.Output
{

    // called when the 2+requested number of buffer loads have occured
    internal delegate void AuxOutputCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    internal delegate void AuxQueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    internal delegate void AuxDAQLoadCompletedHandler(object sender, EventArgs e);

    internal class AuxBuffer
    {
        //events
        private int queueThreshold = 0;
        public event AuxQueueLessThanThresholdHandler AuxQueueLessThanThreshold;
        public event AuxOutputCompleteHandler AuxOutputComplete;
        public event AuxDAQLoadCompletedHandler AuxDAQLoadCompleted;

        // This class's thread
        Thread thrd;

        // Internal Properties
        internal ulong bufferIndex = 0;
        internal double[,] auxBufferLoad;
        internal bool StillWritting = false;
        internal ulong numBuffLoadsCompleted = 0;
        internal uint numBuffLoadsRequired = 0;
        internal bool running = false;

        // Private Properties
        bool bufferLoadFinished = true;
        private ulong numEventsWritten = 0;
        private ulong totalAuxEvents;
        private ulong samples2Finish;
        private uint STIM_SAMPLING_FREQ;
        private uint numSampWrittenForCurrent = 0;
        private string[] s = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.All, PhysicalChannelAccess.Internal);
        private List<AuxData> outerbuffer;
        private AuxData currentAux;
        private AuxData nextAux;
        bool auxDone;

        //Stuff that gets defined with input arguments to constructor
        private uint BUFFSIZE;

        // DEBUGGING
        private Stopwatch sw = new Stopwatch();
        DateTime startTime;
        DateTime tickTime;
        TimeSpan tickDiff;

        //background worker requires the DAQ constructs so that it can encapsulate the asynchronous aux output task
        AnalogMultiChannelWriter auxOutputWriter;
        Task auxOutputTask, buffLoadTask;

        //constructor if using stim buffer in Append mode- with lists!
        internal AuxBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
        {
            //this.SamplesPerStim = (uint)SamplesPerStim;
            // this.outerbufferSize = (uint)OUTERBUFFSIZE;
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.totalAuxEvents = totalAuxEvents;
            this.queueThreshold = queueThreshold;

            outerbuffer = new List<AuxData>();
        }

        internal void Setup(AnalogMultiChannelWriter auxOutputWriter, Task auxOutputTask, Task buffLoadTask)
        {

            startTime = DateTime.Now;
            this.auxOutputTask = auxOutputTask;
            this.auxOutputWriter = auxOutputWriter;
            this.buffLoadTask = buffLoadTask;

            //Set buffer regenation mode to off and set parameters
            auxOutputTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            auxOutputTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;

            // Add reload method to the Counter output event
            buffLoadTask.CounterOutput += new CounterOutputEventHandler(FinishAuxOutput);

            // Populate the outer-buffer twice
            PopulateBufferAppending();
            auxOutputWriter.WriteMultiSample(false, auxBufferLoad);
            PopulateBufferAppending();
            auxOutputWriter.WriteMultiSample(false, auxBufferLoad);

        }

        internal void Start()
        {
            running = true;
            auxOutputTask.Start();
        }

        internal void FinishAuxOutput(EventArgs e)
        {
            if (AuxOutputComplete != null)
            {
                // Tell NR that Aux Out has finished
                AuxOutputComplete(this, e);
            }
        }

        internal void FinishAuxOutput(object sender, EventArgs e)
        {
            if (running)
            {
                //tickTime = DateTime.Now;
                //tickDiff = tickTime.Subtract(startTime);
                //Console.WriteLine(Convert.ToString(tickDiff.TotalMilliseconds) + ": DAQ Aux half-load event.");
                WriteToBuffer();
            }
            else
            {
                FinishAuxOutput(e);
            }
        }

        internal void WriteToBuffer()
        {
            thrd = Thread.CurrentThread;
            thrd.Priority = ThreadPriority.Highest;
            auxDone = false;
            PopulateBufferAppending();
            //Console.WriteLine("Write to Aux Buffer Started");
            auxOutputWriter.WriteMultiSample(false, auxBufferLoad);
            auxDone = true;

        }

        internal void Stop()
        {
            running = false;
        }

        internal void PopulateBufferAppending()
        {
            lock (this)
            {

                //tickTime = DateTime.Now;
                //tickDiff = tickTime.Subtract(startTime);
                //Console.WriteLine(Convert.ToString(tickDiff.TotalMilliseconds) + ": populate buffer started...");

                //Stopwatch ws = new Stopwatch();
                //ws.Start();

                //clear buffers and reset index
                auxBufferLoad = new double[4,BUFFSIZE]; // buffer for aux AO
                bufferIndex = 0;

                while (bufferIndex < BUFFSIZE & outerbuffer.Count > 0)
                {
                    // If unfinished, finish it
                    if (!bufferLoadFinished)
                    {
                        bufferLoadFinished = ApplyCurrentAuxState();
                        if (bufferLoadFinished)
                        {
                            numEventsWritten++;
                        }
                    }
                    else
                    {
                        //is next stimulus within range of this buffload?
                        bool ready = NextAuxStateAppending();

                        if (ready)
                        {
                            bufferLoadFinished = ApplyCurrentAuxState();
                            if (bufferLoadFinished)
                            {
                                numEventsWritten++;
                            }
                        }
                    }
                }

                //Finished the buffer
                numBuffLoadsCompleted++;

                // Check if protocol is completed
                if (numBuffLoadsCompleted >= numBuffLoadsRequired)
                {
                    running = false; // Start clean-up cascade
                }

                // Alert system that buffer load was completed
                OnBufferLoad(EventArgs.Empty);

                //ws.Stop();
                //Console.WriteLine("Buffer load took " + ws.Elapsed);
            }
        }

        internal void Append(List<AuxData> auxList)
        {
            
           //Console.WriteLine("Appending next " + auxList.Count + " aux events to buffer");

            lock (this)
            {
                outerbuffer.AddRange(auxList);
            }
        }

        //write as much f the current aux state as possible
        internal bool ApplyCurrentAuxState()
        {
            //how many samples should we write?
            if (numEventsWritten < totalAuxEvents)
            {
                samples2Finish = nextAux.eventTime-currentAux.eventTime - numSampWrittenForCurrent;
            }
            else // last aux event sets aux state to 0
            {
                samples2Finish = BUFFSIZE;
            }

            //write samples to the buffer
            for (ulong i = 0; (i < samples2Finish) & (bufferIndex < BUFFSIZE); i++)
            {
                WriteSample();
            }

            return (bufferIndex < BUFFSIZE);
        }

        //examines the next aux event, determines if it is within range and loads
        //returns if aux event is within range or not
        internal bool NextAuxStateAppending()
        {
            lock (this)
            {
                if (outerbuffer.ElementAt(0).eventTime < (numBuffLoadsCompleted + 1) * BUFFSIZE)
                {
                    // Current Aux State
                    currentAux = new AuxData(outerbuffer.ElementAt(0).eventTime,
                        outerbuffer.ElementAt(0).eventChannel,
                        outerbuffer.ElementAt(0).eventVoltage);
                    outerbuffer.RemoveAt(0);

                    // Next Aux state
                    if (outerbuffer.Count > 0)
                        nextAux = new AuxData(outerbuffer.ElementAt(0).eventTime,
                            outerbuffer.ElementAt(0).eventChannel,
                            outerbuffer.ElementAt(0).eventVoltage);
                    else
                        nextAux = new AuxData(currentAux.eventTime+1,
                            currentAux.eventChannel,
                            0);

                    if (outerbuffer.Count == (queueThreshold - 1))
                        OnThreshold(EventArgs.Empty);

                    numSampWrittenForCurrent = 0;
                    bufferIndex = currentAux.eventTime - numBuffLoadsCompleted * BUFFSIZE;//move to beginning of this Buffer
                    if (currentAux.eventTime < numBuffLoadsCompleted * BUFFSIZE)//check to make sure we aren't attempting to stimulate in the past
                    {
                        throw new Exception("trying to write an expired aux output: event at sample no. " + currentAux.eventTime + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentAux.eventChannel);
                    }

                    return true;
                }
                else
                {
                    numSampWrittenForCurrent = 0;
                    bufferIndex = BUFFSIZE;//we are done with this buffer
                    return false;
                }
            }
        }

        internal void WriteSample()
        {
            auxBufferLoad[currentAux.eventChannel-1, (int)bufferIndex] = currentAux.eventVoltage;
            numSampWrittenForCurrent++;
            bufferIndex++;
        }

        internal void CalculateLoadsRequired(double finalEventTime)
        {
            //How many buffer loads will this stimulus task take? 3 extra are for (1) Account for delay in start that might push
            //last stimulus overtime by a bit and 2 loads to zero out the double buffer.
            numBuffLoadsRequired = 4 + (uint)Math.Ceiling((double)(STIM_SAMPLING_FREQ * finalEventTime / (double)BUFFSIZE));
        }

        internal double GetTime()    
        {
            return (double)((auxOutputTask.Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
        }

        public uint GetBufferSize()
        {
            return BUFFSIZE;
        }
        
        public void SetNumberofEvents(ulong totalNumberOfAuxEvents)
        {
            totalAuxEvents = totalNumberOfAuxEvents;
        }

        private void OnThreshold(EventArgs e)
        {
            if (AuxQueueLessThanThreshold != null)
                AuxQueueLessThanThreshold(this, e);
        }

        private void OnBufferLoad(EventArgs e)
        {
            if (AuxDAQLoadCompleted != null)
                AuxDAQLoadCompleted(this, e);
        }
    }
}
