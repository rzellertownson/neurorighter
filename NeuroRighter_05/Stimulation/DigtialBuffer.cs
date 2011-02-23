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

namespace NeuroRighter
{
    // called when the 2+requested number of buffer loads have occured
    public delegate void DigitalOutputCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    public delegate void DigitalQueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    public delegate void DigitalDAQLoadCompletedHandler(object sender, EventArgs e);

    public class DigitalBuffer
    {

        //events
        private int queueThreshold = 0;
        public event DigitalQueueLessThanThresholdHandler DigitalQueueLessThanThreshold;
        public event DigitalOutputCompleteHandler DigitalOutputComplete;
        public event DigitalDAQLoadCompletedHandler DigitalDAQLoadCompleted;

        // This class's thread
        Thread thrd;

        // Public Properties
        public ulong BufferIndex = 0;
        public UInt32[] DigitalBufferLoad;
        public bool StillWritting = false;
        public ulong NumBuffLoadsCompleted = 0;
        public uint NumBuffLoadsRequired = 0;
        public bool running = false;

        // Private Properties
        bool BufferLoadFinished = true;
        private ulong NumDigitalEventsWritten = 0;
        private ulong TotalDigitalEvents;
        private ulong Samples2Finish;
        private uint STIM_SAMPLING_FREQ;
        private uint NumSampWrittenForCurrentDO = 0;
        private string[] s = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.All, PhysicalChannelAccess.Internal);
        private List<DigitalData> outerbuffer;
        private DigitalData currentDig;
        private DigitalData nextDig;
        bool digitaldone;

        //Stuff that gets defined with input arguments to constructor
        private uint BUFFSIZE;

        // DEBUGGING
        private Stopwatch sw = new Stopwatch();
        DateTime startTime;
        DateTime tickTime;
        TimeSpan tickDiff;

        //background worker requires the DAQ constructs so that it can encapsulate the asynchronous stimulation task
        DigitalSingleChannelWriter digitalOutputWriter;
        Task digitalOutputTask, buffLoadTask;


        //constructor if using stim buffer in append mode- with lists!
        internal DigitalBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
        {
            //this.SamplesPerStim = (uint)SamplesPerStim;
            // this.outerbufferSize = (uint)OUTERBUFFSIZE;
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.TotalDigitalEvents = TotalDigitalEvents;
            this.queueThreshold = queueThreshold;

            outerbuffer = new List<DigitalData>();
        }

        internal void setup(DigitalSingleChannelWriter digitalOutputWriter, Task digitalOutputTask, Task buffLoadTask)
        {

            startTime = DateTime.Now;
            this.digitalOutputTask = digitalOutputTask;
            this.digitalOutputWriter = digitalOutputWriter;
            this.buffLoadTask = buffLoadTask;

            //Set buffer regenation mode to off and set parameters
            digitalOutputTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            digitalOutputTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;

            // Add reload method to the Counter output event
            buffLoadTask.CounterOutput += new CounterOutputEventHandler(timerTickDigital);

            // Populate the outer-buffer twice
            populateBufferAppending();

            // Start the counter that tells when to reload the daq
            digitalOutputWriter.WriteMultiSamplePort(false, DigitalBufferLoad);

            populateBufferAppending();

            // Start the counter that tells when to reload the daq
            digitalOutputWriter.WriteMultiSamplePort(false, DigitalBufferLoad);

        }

        internal void start()
        {
            running = true;
            digitalOutputTask.Start();
        }

        internal void finishDigitalOutput(EventArgs e)
        {
            if (DigitalOutputComplete != null)
            {
                // Stop the DO task and dispose of it
                digitalOutputTask.Stop();
                digitalOutputTask.Dispose();

                // Tell NR that DO has BufferLoadFinished
                DigitalOutputComplete(this, e);
            }
        }

        void timerTickDigital(object sender, EventArgs e)
        {
            if (running)
            {
                tickTime = DateTime.Now;
                tickDiff = tickTime.Subtract(startTime);
                Console.WriteLine(Convert.ToString(tickDiff.TotalMilliseconds) + ": DAQ Digital half-load event.");
                writeToBuffer();
            }
            else
            {
                finishDigitalOutput(e);
            }
        }

        internal void writeToBuffer()
        {
            thrd = Thread.CurrentThread;
            thrd.Priority = ThreadPriority.Highest;


            digitaldone = false;
            populateBufferAppending();
            Console.WriteLine("Write to Digital Buffer Started");
            digitalOutputWriter.WriteMultiSamplePort(false, DigitalBufferLoad);
            digitaldone = true;

        }

        internal void stop()
        {
            running = false;
        }

        //lets see if we can simplify things here...
        internal void populateBufferAppending()
        {
            lock (this)
            {

                tickTime = DateTime.Now;
                tickDiff = tickTime.Subtract(startTime);
                Console.WriteLine(Convert.ToString(tickDiff.TotalMilliseconds) + ": populate buffer started...");

                Stopwatch ws = new Stopwatch();
                ws.Start();

                //clear buffers and reset index
                DigitalBufferLoad = new UInt32[BUFFSIZE]; // buffer for digital port
                BufferIndex = 0;

                //Are we between DO events?  if so, finish as much as you can from the current one
                //if (currentDig != null)
                //{
                    //   MessageBox.Show("examining first stim");


                //}

                while (BufferIndex < BUFFSIZE & outerbuffer.Count > 0)
                {
                    // If digital read stimulus is unfinished, finish it
                    if (!BufferLoadFinished)
                    {
                        BufferLoadFinished = applyCurrentDigitalState();
                        if (BufferLoadFinished)
                        {
                            NumDigitalEventsWritten++;
                        }
                    }
                    else
                    {
                        //is next stimulus within range of this buffload?
                        bool ready = nextDigitalStateAppending();

                        if (ready)
                        {
                            BufferLoadFinished = applyCurrentDigitalState();
                            if (BufferLoadFinished)
                            {
                                NumDigitalEventsWritten++;
                            }
                        }
                    }
                }

                //Finished the buffer
                NumBuffLoadsCompleted++;

                // Check if protocol is completed
                if (NumBuffLoadsCompleted >= NumBuffLoadsRequired)
                {
                    running = false; // Start clean-up cascade
                }

                // Alert system that buffer load was completed
                onBufferLoad(EventArgs.Empty);

                ws.Stop();
                Console.WriteLine("Buffer load took " + ws.Elapsed);
            }
        }

        internal void append(List<DigitalData> digitallist)
        {
            
           Console.WriteLine("Appending next " + digitallist.Count + " digital events to buffer");

            lock (this)
            {
                outerbuffer.AddRange(digitallist);
            }
        }

        //write as much f the current digital state as possible
        internal bool applyCurrentDigitalState()
        {
            //how many samples should we write?
            if (NumDigitalEventsWritten < TotalDigitalEvents)
            {
                Samples2Finish = nextDig.EventTime-currentDig.EventTime - NumSampWrittenForCurrentDO;
            }
            else // last digital event sets digital state to 0
            {
                Samples2Finish = BUFFSIZE;
            }

            //write samples to the buffer
            for (ulong i = 0; (i < Samples2Finish) & (BufferIndex < BUFFSIZE); i++)
            {
                writeSample();
            }

            return (BufferIndex < BUFFSIZE);
        }

        //examines the next stimulus, determines if it is within range, and loads it if it is
        //returns if stimulus is within range or not
        internal bool nextDigitalStateAppending()
        {
            lock (this)
            {
                if (outerbuffer.ElementAt(0).EventTime < (NumBuffLoadsCompleted + 1) * BUFFSIZE)
                {
                    // Current Digital State
                    currentDig = new DigitalData(outerbuffer.ElementAt(0).EventTime, outerbuffer.ElementAt(0).Byte);
                    outerbuffer.RemoveAt(0);

                    // Next Digital state
                    nextDig = new DigitalData(outerbuffer.ElementAt(0).EventTime, outerbuffer.ElementAt(0).Byte);

                    if (outerbuffer.Count == (queueThreshold - 1))
                        onThreshold(EventArgs.Empty);

                    NumSampWrittenForCurrentDO = 0;
                    BufferIndex = currentDig.EventTime - NumBuffLoadsCompleted * BUFFSIZE;//move to beginning of this Buffer
                    if (currentDig.EventTime < NumBuffLoadsCompleted * BUFFSIZE)//check to make sure we aren't attempting to stimulate in the past
                    {
                        throw new Exception("trying to write an expired digital output: event at sample no. " + currentDig.EventTime + " was written at time " + NumBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentDig.Byte);
                    }

                    return true;
                }
                else
                {
                    NumSampWrittenForCurrentDO = 0;
                    BufferIndex = BUFFSIZE;//we are done with this buffer
                    return false;
                }
            }
        }

        internal void writeSample()
        {
            DigitalBufferLoad[(int)BufferIndex] = currentDig.Byte;
            NumSampWrittenForCurrentDO++;
            BufferIndex++;
        }

        internal void calculateLoadsRequired(double finalEventTime)
        {
            //How many buffer loads will this stimulus task take? 3 extra are for (1) Account for delay in start that might push
            //last stimulus overtime by a bit and 2 loads to zero out the double buffer.
            NumBuffLoadsRequired = 3 + (uint)Math.Ceiling((double)(STIM_SAMPLING_FREQ * finalEventTime / (double)BUFFSIZE));
        }

        public double time()    
        {
            return (double)((digitalOutputTask.Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
        }

        public uint getBufferSize()
        {
            return BUFFSIZE;
        }
        
        public void setNumberofEvents(ulong totalNumberOfDigitalEvents)
        {
            TotalDigitalEvents = totalNumberOfDigitalEvents;
        }

        private void onThreshold(EventArgs e)
        {
            if (DigitalQueueLessThanThreshold != null)
                DigitalQueueLessThanThreshold(this, e);
        }

        private void onBufferLoad(EventArgs e)
        {
            Console.WriteLine("Updating the progress bar");
            if (DigitalDAQLoadCompleted != null)
                DigitalDAQLoadCompleted(this, e);
        }
    }

}
