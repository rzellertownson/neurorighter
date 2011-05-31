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
    internal abstract class NROutBuffer<T> where T : NREvent
    {
        //events
        private int queueThreshold = 0;//how many NREvents do you want to have 'on deck' before asking for more?
        public event QueueLessThanThresholdHandler QueueLessThanThreshold;//event thrown when there are less than that many events
        public event StimulationCompleteHandler StimulationComplete;
        public event DAQLoadCompletedHandler DAQLoadCompleted;

        // This class's thread
        

        // Internal Properties
        
        
        internal bool StillWritting = false;
        internal ulong numBuffLoadsCompleted = 0;
        internal uint numBuffLoadsRequired = 0;
        internal bool running = false;
       
        internal bool immortal = false;//do we keep on loading the buffer forever, or do we have a finite number of loads to perform (openloop)
        internal bool done;

        // Private Properties
        
        
        private string[] s = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.All, PhysicalChannelAccess.Internal);
        private List<T> outerbuffer;//the list of all the things that this NROutBuffer needs to do
        private ulong currentSample;


        //properties of the buffer being written to the DAQ
        private uint BUFFSIZE;//how long is the DAQ half buffer (ie, how many samples get loaded into the DAQ per bufferload?
        //private bool bufferLoadFinished = true;//are we done loading the most recent buffer, or instead are we still loading?
        private ulong bufferIndex = 0;//where are we in the buffer we are writing?
        private ulong numEventsWritten = 0;//how many NREvents have we loaded?
        private ulong totalEvents;//what is the total number of events we need to load?  0 means we will load eternally
        private uint STIM_SAMPLING_FREQ;
        private List<double[,]> abuffs;//the analog buffers that are going to be written to the DAQ
        private List<uint[]> dbuffs;//the digital buffers that are going to be written to the DAQ

        //properties of the current stimulus:
        private T currentStim;//the thing that we are working on right now
        private T nextStim;//the next guy we have to worry about
        private uint numSampWrittenForCurrentStim = 0;//how many samples we have written of the current stimulus
        private List<double[,]> anEventValues;//the analog values for the current stimulus (copied into abuffs)
        private List<uint[]> digEventValues;//the digital values for the current stimulus (copied into dbuffs)
        

        
        //NI DAQ properties
        //this is the DAQ task that sends the 'load buffer' signal.

        Task buffLoadTask;

        //these are the DAQ tasks that take the loaded buffer and turn it into stuff in the real world
        //you need a task for each device that this NROutBuffer uses, as well as each type of output (analog or digital).  
        //Don't blame us, that's how NI works.

        Task[] analogTasks;
        AnalogMultiChannelWriter[] analogWriters;
        Task[] digitalTasks;
        DigitalSingleChannelWriter[] digitalWriters;
        

        // DEBUGGING
        // these are in place so you can watch the timing of your NROutBuffer
        
        //private double startTime;
        //private DateTime tickTime;
        //private TimeSpan tickDiff;

        

        
        

        #region internal methods- used by NR objects

        //creates the NROutBuffer so that you can start appending and whatnot.
        internal NROutBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
        {
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.queueThreshold = queueThreshold;

            outerbuffer = new List<T>();
        }

        //this NROutBuffer has configured the NI DAQs for use, and has written the first two buffer loads to the DAQs.  If you have any stimuli 
        //that you plan on applying within the first two buffer loads, append those before calling this method.
        internal void Setup(AnalogMultiChannelWriter[] AnalogWriters, DigitalSingleChannelWriter[] DigitalWriters, Task[] AnalogTasks, Task[] DigitalTasks,  Task BuffLoadTask)
        {


            this.analogWriters = new AnalogMultiChannelWriter[AnalogWriters.Length];
            this.analogTasks = new Task[AnalogTasks.Length];
            for (int i = 0; i < AnalogWriters.Length; i++)
            {
                this.analogWriters[i] = AnalogWriters[i];
                this.analogTasks[i] = AnalogTasks[i];
                this.analogTasks[i].Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
                this.analogTasks[i].Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            }

            this.digitalWriters = new DigitalSingleChannelWriter[DigitalWriters.Length];
            this.digitalTasks = new Task[DigitalTasks.Length];
            for (int i = 0; i < DigitalWriters.Length; i++)
            {
                this.digitalWriters[i] = DigitalWriters[i];
                this.digitalTasks[i] = DigitalTasks[i];
                this.digitalTasks[i].Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
                this.digitalTasks[i].Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            }

            // Add reload method to the Counter output event
            this.buffLoadTask = BuffLoadTask;
            buffLoadTask.CounterOutput += new CounterOutputEventHandler(TimerTick);

           // Populate the outer-buffer twice
            PopulateBufferAppending();
            PopulateBufferAppending();

            
        }
        
        //this NROutBuffer is now ready to go, awaiting the green light from the master task (and the buffloadtask)
        internal void Start()
        {
            running = true;

            for (int i = 0; i < analogTasks.Length; i++)
                analogTasks[i].Start();
            for (int i = 0; i < digitalTasks.Length; i++)
                digitalTasks[i].Start();
        }

        //this NROutBuffer will no longer load data into the DAQs, and instead will stop and dispose those NI tasks
        internal void Stop()
        {
            running = false;
        }

        //write this list of NREvents to the buffer.  Double checks to make sure that you haven't attempted to stimulate at some time in the past
        internal void writeToBuffer(List<T> addtobuffer)
        {
            //error checking
            foreach (T n in addtobuffer)
            {
                if (n.sampleIndex < currentSample)
                    throw new Exception(this.ToString() + ": Attempted to stimulate in the past (sample " + n.sampleIndex + " at sample " + currentSample + ")");
                lock(outerbuffer) outerbuffer.Add(n);
            }
            //passed, add to buffer
            //Append(addtobuffer);
        }

        //inform the NROutBuffer how long it has to go for- used in open loop
        internal void CalculateLoadsRequired(double finalStimTime)
        {
            //How many buffer loads will this stimulus task take? 3 extra are for (1) Account for delay in start that might push
            //last stimulus overtime by a bit and 2 loads to zero out the double buffer.
            numBuffLoadsRequired = 4 + (uint)Math.Ceiling((double)(STIM_SAMPLING_FREQ * finalStimTime / (double)BUFFSIZE));
        }

        //inform the NROutBuffer how many events it has to deal with
        internal void SetNumberofEvents(ulong totalNumberOfEvents)
        {
            totalEvents = totalNumberOfEvents;
        }

        //used to verify how many stimuli are currently enqueued
        internal int StimuliInQueue()
        {
            return outerbuffer.Count();
        }

        //get the current time in miliseconds, as measured by the number of samples generated by this NROutBuffer
        internal double GetTime()
        {
            if (analogTasks.Length > 0)
            {
                return (double)((analogTasks[0].Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
            }
            else if (digitalTasks.Length >0)
                {
                    return (double)((digitalTasks[0].Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
                }
            throw new Exception("GetTime() requested from " + this.ToString() + ", which has no tasks");
            
                
        }

        internal uint GetBufferSize()
        {
            return BUFFSIZE;
        }

        #endregion

        #region private methods


        //method implimented in the subclass that specifies how to turn this NREvent into analog and digital streams
        protected abstract void writeEvent(T stim, ref  List<double[,]> anEventValues, ref List<uint[]> digEventValues);

        //event called by the buffload task
        private void TimerTick(object sender, EventArgs e)
        {
            //Console.Write("anda...");
           // thrd = Thread.CurrentThread;
          //  thrd.Priority = ThreadPriority.Highest;
            double tickDiff = GetTime();
            if (running)
            {
              //  Console.WriteLine(Convert.ToString(tickDiff) + ": " + this.ToString() + " DAQ half-load event.");
                WriteToDAQ();
            }
            else
            {
               // Console.WriteLine(Convert.ToString(tickDiff) + ": "+this.ToString() +" not running.");
                FinishStimulation(e);
            }
         //   thrd.Priority = ThreadPriority.Normal;
        }

        //event raised if we are done.
        private void FinishStimulation(EventArgs e)
        {
            if (StimulationComplete != null)
            {
                // Stop the tasks and dispose of them
                foreach (Task t in analogTasks)
                {
                    t.Stop();
                    t.Dispose();
                }
                foreach (Task t in digitalTasks)
                {
                    t.Stop();
                    t.Dispose();
                }
                
                // Tell NR that stimulation has finished
                StimulationComplete(this, e);
            }
        }

        //wrapper method for PopulateBufferAppending that also handles
        //some debugging stuff
        private void WriteToDAQ()
        {
            
            done = false;
            //Console.WriteLine("Write to Buffer Started");
            //Now look in the outer buffer and grab whatever is needed for this load.
            //this operation is the most intensive the buffer needs to 
            PopulateBufferAppending();
            
            
            done = true;
            
        }

        //called each bufferload, that is, each time the buffload task fires off
        //creates a set of empty buffers (arrays) for each task, and then goes through
        //the outerbuffer (list) and looks for events that need to be applied during 
        //this particular load.  Those events are then written to the buffer
        private void PopulateBufferAppending()
        {
            //create the buffers we are about to write to the DAQ
            double start;
            if (running)
                start = this.GetTime();
            else
                start = 0;
            
            abuffs = new List<double[,]>();
            dbuffs = new List<uint[]>();
               
            foreach (Task n in analogTasks)
            {
                double[,] abuff = new double[n.AOChannels.ICollection_Count,BUFFSIZE];
                abuffs.Add(abuff);
                
            }
            foreach (Task n in digitalTasks)
            {
                uint[] dbuff = new uint[BUFFSIZE];
                dbuffs.Add(dbuff);
                
            }
                
            
              

            
            bufferIndex = 0;
                


                //if we don't have a stimulus 'on deck', look for one (needed for state change buffers).
            if ((outerbuffer.Count > 0) & (nextStim == null))
                nextStim = outerbuffer.ElementAt(0);
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
               
                //at this point, we have either finished a stimulus, or finished the buffer.
                //therefore, if there is room left in this buffer, find the next stimulus and move to it.

            //for state change buffers, keep in mind that ApplyCurrentStimulus will go until the next stimulus
            //or until the end of this particular buffer
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
                if ((numBuffLoadsCompleted >= numBuffLoadsRequired)&(!immortal))
                {
                    Stop() ; // Start clean up cascade
                }

                OnBufferLoad(EventArgs.Empty);

            //write buffers

                for(int i = 0;i< analogWriters.Length;i++)
                {
                    analogWriters[i].WriteMultiSample(false,abuffs.ElementAt(i));
                    double[,] tmp = abuffs.ElementAt(i);
                    tmp = null;
                    
                }
                for (int i = 0; i < digitalWriters.Length;i++ )
                {
                    
                    digitalWriters[i].WriteMultiSamplePort(false, dbuffs.ElementAt(i));
                    uint[] tmp = dbuffs.ElementAt(i);
                    tmp = null;
                }
                double stop = 0;
            if (running)
                stop = this.GetTime();
                //Console.WriteLine(this.ToString() + " just finished buffering through sample " + currentSample+ " which took " + (stop-start)+ "ms");

        }

        //write as much of the current stimulus as possible
        //agnostic as to whether or not you've finished this stimulus or not.
        //returns if finished the stimulus or not
        private bool ApplyCurrentStimulus()
        {

            //how many samples should we write, including blanking?
            uint samples2Finish;
            if (currentStim.sampleDuration > 0)//this is a non-instantaneous event
                samples2Finish = currentStim.sampleDuration   - numSampWrittenForCurrentStim;
            else//this is an instantaneous event, ie a state change
                
            {
                //if we have another event on deck, and we aren't done with events
                if ((nextStim!= null) & ((numEventsWritten < totalEvents) || immortal))

                    samples2Finish = (uint)(nextStim.sampleIndex - currentStim.sampleIndex) - numSampWrittenForCurrentStim; 
                else
                    //if not, than hold this state change throughout the duration of the buffer
                    samples2Finish = BUFFSIZE;
            }
                

            //write samples to the buffer
            for (ulong i = 0; (i < samples2Finish) & (bufferIndex < BUFFSIZE); i++)
            {
                WriteSample();
            }

            return (bufferIndex < BUFFSIZE);
        }

        //find the next stimulus to apply
        //returns true if there is a stimulus within this bufferload
        private bool NextStimulusAppending()
        {
            
            lock (outerbuffer)//prevent anyone from writing to the outer buffer for this
            {
                if (outerbuffer.Count > 0)//buffer isn't empty
                {
                    if (outerbuffer.ElementAt(0).sampleIndex < (numBuffLoadsCompleted + 1) * BUFFSIZE)
                    {

                        currentStim = (T)outerbuffer.ElementAt(0).Copy();
                        outerbuffer.RemoveAt(0);

                        if (outerbuffer.Count > 0)
                            nextStim = outerbuffer.ElementAt(0);//we don't need a copy as we don't expect this element to change.

                        //turn the new stim into something we can throw at the DAQs
                        writeEvent(currentStim, ref  anEventValues, ref digEventValues);


                        if (outerbuffer.Count == (queueThreshold - 1))
                            OnThreshold(EventArgs.Empty);

                        numSampWrittenForCurrentStim = 0;
                        bufferIndex = currentStim.sampleIndex - numBuffLoadsCompleted * BUFFSIZE;//move to beginning of this stimulus
                        if (currentStim.sampleIndex < numBuffLoadsCompleted * BUFFSIZE)//check to make sure we aren't attempting to stimulate in the past
                        {
                            //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + currentStim.StimSample + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentStim.channel);
                            throw new Exception("trying to write an expired stimulus: stimulation at sample no " + currentStim.sampleIndex + " was written at time " + numBuffLoadsCompleted * BUFFSIZE );
                        }

                        return true;
                    }
                    else
                    {

                        currentStim = null;//we aren't currently stimulating

                        numSampWrittenForCurrentStim = 0;//we haven't written anything
                        bufferIndex = BUFFSIZE;//we are done with this buffer
                        return false;
                    }
                }
                else
                {
                    currentStim = null;//we aren't currently stimulating

                    numSampWrittenForCurrentStim = 0;//we haven't written anything
                    bufferIndex = BUFFSIZE;//we are done with this buffer
                    return false;
                }
            }
        }

        //called whenver we finish a particular NREvent
        private void FinishStim()
        {
            //more explicit garbage collection
            for (int i = 0; i < anEventValues.Count; i++)
            {
                double[,] tmp = anEventValues.ElementAt(i);
                tmp = null;
            }

            for (int i = 0; i < digEventValues.Count; i++)
            {
                uint[] tmp = digEventValues.ElementAt(i);
                tmp = null;
            }
            
            currentStim = null;//we aren't currently stimulating

        }

        //TODO:  needs error checking to make sure that the writeEvent created buffer matches up with the buffers created for the tasks.
        private void WriteSample()
        {
            for (int i = 0; i < abuffs.Count; i++)
            {
                for (int j = 0; j < abuffs.ElementAt(i).GetLength(0); j++)
                    if (currentStim.sampleDuration>0)
                        abuffs.ElementAt(i)[j, (int)bufferIndex] = anEventValues.ElementAt(i)[j, numSampWrittenForCurrentStim];
                    else//state change mode
                        abuffs.ElementAt(i)[j, (int)bufferIndex] = anEventValues.ElementAt(i)[j, 0];
            }

            for (int i = 0; i < dbuffs.Count; i++)
            {
                //for (int j = 0; j < dbuffs.ElementAt(i).GetLength(1); j++)
                    if (currentStim.sampleDuration > 0)
                        dbuffs.ElementAt(i)[(int)bufferIndex] = digEventValues.ElementAt(i)[numSampWrittenForCurrentStim];
                    else//state change mode
                        dbuffs.ElementAt(i)[(int)bufferIndex] = digEventValues.ElementAt(i)[0];
            }

            numSampWrittenForCurrentStim++;
            bufferIndex++;

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
        
        #endregion
    }
}
