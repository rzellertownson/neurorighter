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
using NeuroRighter.dbg;
using ExtensionMethods;


namespace NeuroRighter.Output
{

    /// <summary>
    /// NeuroRighter's abstract class for output service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NROutBuffer<T> where T : NREvent
    {

        /// <summary>
        ///  The mutex class for concurrent read and write access to data buffers
        /// </summary>
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        //events
        internal bool recoveryFlag = false;
        private int queueThreshold = 0;//how many NREvents do you want to have 'on deck' before asking for more?
        internal event QueueLessThanThresholdHandler QueueLessThanThreshold;// fire when there are less than that many events
        internal event StimulationCompleteHandler StimulationComplete;
        internal event DAQLoadCompletedHandler DAQLoadCompleted;

        // Internal Properties
        internal bool StillWritting = false;
        internal ulong numBuffLoadsCompleted = 0;
        internal ulong numBuffLoadsThisRun = 0;
        internal uint numBuffLoadsRequired = 0;
        internal bool running = false;

        internal bool immortal = false;//do we keep on loading the buffer forever, or do we have a finite number of loads to perform (openloop)
        internal bool robust = true;
        bool notDead;

        // Private Properties
        private int semSize = Int32.MaxValue;
        private string[] s = DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.All, PhysicalChannelAccess.Internal);
        private List<T> outerbuffer;//the list of all the things that this NROutBuffer needs to do
        private ulong currentSample;

        //properties of the buffer being written to the DAQ
        protected uint BUFFSIZE;//how long is the DAQ half buffer (ie, how many samples get loaded into the DAQ per bufferload?
        private ulong bufferIndex = 0;//where are we in the buffer we are writing?
        private ulong numEventsWritten = 0;//how many NREvents have we loaded?
        private ulong totalEvents;//what is the total number of events we need to load?  0 means we will load eternally
        protected uint STIM_SAMPLING_FREQ;
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
        protected Task masterTask;

        //these are the DAQ tasks that take the loaded buffer and turn it into stuff in the real world
        Task[] analogTasks;
        AnalogMultiChannelWriter[] analogWriters;
        Task[] digitalTasks;
        DigitalSingleChannelWriter[] digitalWriters;

        private BackgroundWorker bw;
        internal int safecount;

        Queue<int> names;
        string masterLoad;
        private object taskLock;
        internal object pbaLock;
        //internal object loopLock;
        // DEBUGGING
        // these are in place so you can watch the timing of your NROutBuffer
        protected RealTimeDebugger Debugger;
        private CounterOutputEventHandler tt;
        #region internal/protected methods- used by NR objects

        //creates the NROutBuffer so that you can start appending and whatnot.
        internal NROutBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold, bool robust)
        {
            this.BUFFSIZE = (uint)INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = (uint)STIM_SAMPLING_FREQ;
            this.queueThreshold = queueThreshold;
            this.robust = robust;

            outerbuffer = new List<T>();
            anEventValues = new List<double[,]>();//the analog values for the current stimulus (copied into abuffs)
            digEventValues = new List<uint[]>();
            pbaLock = new object();//to prevent a recovery while we are in this buffer
            //loopLock = new object();
            namesLock = new object();//prevents fighting over the names queue
            runningLock = new object();//prevents fighting over the 'running' boolean
            recoveryInProgress = new object();//prevents multiple threads from trying to restart simultaneously
            taskLock = new object();

        }

        //this NROutBuffer has configured the NI DAQs for use, and has written the first two buffer loads to the DAQs.  If you have any stimuli 
        //that you plan on applying within the first two buffer loads, append those before calling this method.
        internal void Setup(Task buffLoadTask, RealTimeDebugger debugger, Task masterTask)
        {
            //grab the debugger and the counter
            this.Debugger = debugger;
            this.buffLoadTask = buffLoadTask;
            this.masterTask = masterTask;
            this.masterLoad = buffLoadTask.COChannels[0].PulseTerminal;

            //initialize buffer counting mechanisms
            safecount = 0;//this is the number of times the counter has gone off.
            currentCount = 0;//this is the current counter tick we are working on.
            sem = new Semaphore(0, semSize);

            names = new Queue<int>();
            
            //initialize background worker to run responses to counter ticks
            bw = new BackgroundWorker();
            first = true;
            bw.DoWork += new DoWorkEventHandler(ProcessTickThread);
            bw.RunWorkerAsync();
            tt = new CounterOutputEventHandler(TimerTick);
            buffLoadTask.CounterOutput += tt;
            notDead = true;
            //clear tasks and writers if they still exist
            ClearTasks();

            //configure the task arrays
            SetupTasks();

            //load up the first two buffers
            numBuffLoadsCompleted++;
            PopulateBufferAppending(true, false, false);
            numBuffLoadsCompleted++;
            PopulateBufferAppending(true, false, false);


        }

        //parts of setup that are specific to clearing and configuring tasks
        private void SetupTasks()//Task[] analogTasks, AnalogMultiChannelWriter[] analogWriters, Task[] digitalTasks, DigitalSingleChannelWriter[] digitalWriters)
        {


            //do outputbuffer-specific task configuration (which device/channels, ranges)
            SetupTasksSpecific(ref analogTasks, ref digitalTasks);


            //do general configuration stuff (clocks, starts, writemodes, buffer sizes)

            //analog
            analogWriters = new AnalogMultiChannelWriter[analogTasks.Length];
            for (int i = 0; i < analogTasks.Length; i++)
            {
                //set timing off of the master task
                analogTasks[i].Timing.ReferenceClockSource =
                        masterTask.Timing.ReferenceClockSource;
                analogTasks[i].Timing.ReferenceClockRate =
                    masterTask.Timing.ReferenceClockRate;

                //set start triggers off of the buffer loading (counter) task
                analogTasks[i].Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                    masterLoad, DigitalEdgeStartTriggerEdge.Rising);

                //configure the on board (hardware) buffer
                analogTasks[i].Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
                //#soft!
                //  analogTasks[i].Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
                analogTasks[i].Stream.WriteRegenerationMode = WriteRegenerationMode.AllowRegeneration;

                //create a writer for this task
                analogWriters[i] = new AnalogMultiChannelWriter(analogTasks[i].Stream);

                //now that we've created a potential task configuration, check to make sure everything is kosher
                analogTasks[i].Control(TaskAction.Verify);
                //actually set the hardware to these settings
                analogTasks[i].Control(TaskAction.Reserve);
            }

            digitalWriters = new DigitalSingleChannelWriter[digitalTasks.Length];
            for (int i = 0; i < digitalTasks.Length; i++)
            {
                //configure the on board (hardware) buffer
                digitalTasks[i].Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
                //digitalTasks[i].Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
                digitalTasks[i].Stream.WriteRegenerationMode = WriteRegenerationMode.AllowRegeneration;

                //create a writer for this task
                digitalWriters[i] = new DigitalSingleChannelWriter(digitalTasks[i].Stream);

                //now that we've created a potential task configuration, check to make sure everything is kosher
                digitalTasks[i].Control(TaskAction.Verify);
                //actually set the hardware to these settings
                digitalTasks[i].Control(TaskAction.Reserve);
            }
        }

        //parts of task setup that are specific to this particular type of NROutBuffer
        protected abstract void SetupTasksSpecific(ref Task[] analogTasks, ref Task[] digitalTasks);


        //starts all the analog and digital output tasks for this buffer.  generation will begin once the master task and buffer loading task start as well.
        internal void Start()
        {
            lock (runningLock)
                running = true;
            lock (taskLock)
            {
                for (int i = 0; i < analogTasks.Length; i++)
                    analogTasks[i].Start();
                for (int i = 0; i < digitalTasks.Length; i++)
                    digitalTasks[i].Start();
            }
        }

        //prevents this NROutBuffer from loadind data into the DAQs
        internal void Stop()
        {
            // Debugger.Write("nroutbuffer attempted stop");
            lock (runningLock)
            {
                running = false;
                ProcessTickThread(null, null);
            }
        }

        //stops output generation as soon as possible
        //internal void Kill()
        //{
        //  //  Debugger.Write("nroutbuffer attemped kill");
        //    running = false;
        //    clearTasks();
        //}

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

        //disposes all output tasks
        protected void ClearTasks()
        {
            if (analogTasks != null)
                for (int i = 0; i < analogTasks.Length; i++)
                {
                    if (analogTasks[i] != null)
                    {
                        analogTasks[i].Dispose();
                        analogTasks[i] = null;
                        analogWriters[i] = null;
                    }
                }
            if (digitalTasks != null)
                for (int i = 0; i < digitalTasks.Length; i++)
                {
                    if (digitalTasks[i] != null)
                    {
                        digitalTasks[i].Dispose();
                        digitalTasks[i] = null;
                        digitalWriters[i] = null;
                    }
                }

        }

        #endregion

        #region public methods

        /// <summary>
        /// Writes NeuroRighter output events to NeuroRighter's outer event buffer. Event's posted to this list contain a time of executation relative to
        /// the current write posisition on the NI DAQ. When they are in range of the hardware buffer, they are written to the hardware and slated for
        /// executtion. After events are written to hardware, they cannot be stopped.
        /// </summary>
        /// <param name="addtobuffer"> NeuroRighter ouput event type</param>
        public void WriteToBuffer(List<T> addtobuffer)
        {
            bufferLock.EnterWriteLock();
            try
            {
                foreach (T n in addtobuffer)
                {
                    if (n.SampleIndex < currentSample)
                    {
                        Console.WriteLine("WARNING. " + this.ToString() + ": Attempted to stimulate in the past (sample " + n.SampleIndex + " at sample " + currentSample + ")");
                        continue;
                    }
                    outerbuffer.Add(n);
                }

                // Sort the list
                outerbuffer = outerbuffer.OrderBy(x => x.SampleIndex).ToList();
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// This method attempts to delete events within NR's outer output buffer that are slated to be executed in the future.
        /// </summary>
        /// <param name="startSample"> Events greater than the start sample </param>
        /// <param name="endSample"> Events less than the end sample</param>
        public void DeleteSamplesFromBuffer(ulong startSample, ulong endSample)
        {
            bufferLock.EnterWriteLock();
            try
            {
                for (int i = outerbuffer.Count() - 1; i >= 0; --i)
                {
                    if (outerbuffer.ElementAt(i).SampleIndex > startSample && outerbuffer.ElementAt(i).SampleIndex >= endSample)
                        outerbuffer.RemoveAt(i);
                }

                // Sort the list
                outerbuffer = outerbuffer.OrderBy(x => x.SampleIndex).ToList();
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Attempts to delete events within NR's outer output buffer that are slated to be executed in the future.
        /// </summary>
        /// <param name="startSample"> Events greater than the start sample will be deleted. </param>
        public void DeleteSamplesFromBuffer(ulong startSample)
        {
            bufferLock.EnterWriteLock();
            try
            {
                for (int i = outerbuffer.Count() - 1; i >= 0; --i)
                {
                    if (outerbuffer.ElementAt(i).SampleIndex > startSample)
                        outerbuffer.RemoveAt(i);
                }

                // Sort the list
                outerbuffer = outerbuffer.OrderBy(x => x.SampleIndex).ToList();
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Dispose of all pending output events in the outer buffer.
        /// </summary>
        public void EmptyOuterBuffer()
        {
            bufferLock.EnterWriteLock();
            try
            {
                outerbuffer.Clear();
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        ///<summary>
        ///returns the number of stimuli that are enqueued on this buffer, but have yet to be written out to hardware.
        /// </summary>
        ///<returns>Number of stimuli enqueued.</returns>
        public int StimuliInQueue()
        {
            bufferLock.EnterUpgradeableReadLock();
            try
            {
                return outerbuffer.Count();
            }
            finally
            {
                // release the write lock
                bufferLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Get the current time in miliseconds, as measured by the number of samples generated by this NROutBuffer.
        /// </summary>
        /// <returns> Current output buffer time in millieseconds</returns>
        public virtual double GetTime()
        {

            lock (taskLock)
                return GetTimePrivate();
        }

        /// <summary>
        /// Get the number of samples generated by this NROutBuffer.
        /// </summary>
        /// <returns> Current output buffer time in millieseconds</returns>
        public virtual ulong GetCurrentSample()
        {
            lock (taskLock)
                return GetCurrentSamplePrivate();
        }

        private ulong GetCurrentSamplePrivate()
        {
            try
            {
                if (analogTasks.Length > 0)
                {
                    //if (!analogTasks[0].IsDone)
                    return (ulong)analogTasks[0].Stream.TotalSamplesGeneratedPerChannel;
                    //else return 0;
                }
                else if (digitalTasks.Length > 0)
                {
                    //digitalTasks[0].
                    //if (!digitalTasks[0].IsDone)
                    return (ulong)digitalTasks[0].Stream.TotalSamplesGeneratedPerChannel;
                    //else return 0;
                }

            }
            catch (Exception me)
            {
                return 0;//if we have task arrays but no tasks, that means we are in the middle of a restart and therefore our ouput is zero
            }
            throw new Exception("GetCurrentSample() requested from " + this.ToString() + ", which has no tasks");
        }

        /// <summary>
        /// Returns the number of output buffers that have been written to the DAC since the start of the experiment. Each of
        /// these buffers has length int serverObject.GetBuffSize();
        /// </summary>
        /// <returns>numBuffLoadsCompleted</returns>
        public ulong GetNumberBuffLoadsCompleted()
        {
            return numBuffLoadsCompleted;
        }

        /// <summary>
        /// Returns boolean value indicating whether or not output samples are being generated by the buffer output tasks
        /// </summary>
        /// <returns>Task.isDone</returns>
        public bool IsDone()
        {
            lock (taskLock)
                return IsDonePrivate();
        }

        private bool IsDonePrivate()
        {
            try
            {
                if (analogTasks.Length > 0)
                {

                    return analogTasks[0].IsDone;
                }
                else if (digitalTasks.Length > 0)
                {
                    return digitalTasks[0].IsDone;
                }
            }
            catch (DaqException de)
            {
                return false;
            }
            throw new Exception("IsDone() requested from " + this.ToString() + ", which has no tasks");
        }

        internal uint GetBufferSize()
        {
            return BUFFSIZE;
        }



        #endregion

        #region private methods

        private double GetTimePrivate()
        {
            if (analogTasks.Length > 0)
            {
                return (double)((analogTasks[0].Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
            }
            else if (digitalTasks.Length > 0)
            {
                return (double)((digitalTasks[0].Stream.TotalSamplesGeneratedPerChannel) * 1000.0 / STIM_SAMPLING_FREQ);
            }
            throw new Exception("GetTime() requested from " + this.ToString() + ", which has no tasks");
        }

        //method implemented in the subclass that specifies how to turn this NREvent into analog and digital streams
        protected abstract void WriteEvent(T stim, ref  List<double[,]> anEventValues, ref List<uint[]> digEventValues);

        //event called by the buffload task
        private object namesLock;
        private object runningLock = new object();
        private void TimerTick(object sender, EventArgs e)
        {

            //if the buffer is stopped but the counter is still going, we can skip this
            //if (running)
            //{
            //{
            try
            {
                if (notDead)
                    lock (namesLock)
                    {
                        //the number of times the counter has gone off
                        ++safecount;
                        //for each counter tick, enqueue that tick as a name to be executed
                        names.Enqueue(safecount);
                        //for each of these name on deck, release a semaphore
                        sem.Release(1);
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in closed loop Timer Deamon: \n\r");
            }

            //}

            //}


        }


        private Semaphore sem;
        private int currentCount;
        internal object recoveryInProgress;

        //this is the code executed by the thread that actually does the processing
        private void ProcessTickThread(object sender, DoWorkEventArgs e)
        {
            //Debugger.Write("starting processing thread for "+ this.ToString());


            //the "infinite" loop- 
            while (running || first)
            {

                //to prevent us from re-entering the loop during recovery from an error
                sem.WaitOne();//wait for a semaphore to become available

                //at this point, we are commited to processing an output
                lock (namesLock)
                {
                    currentCount = names.Dequeue();//figure out what name needs to be executed
                }
                //Debugger.Write(this.ToString()+ " tick " + currentCount + " passed semaphore");
                lock (runningLock)
                {
                    lock (taskLock)
                    {
                        recoveryFlag = AttemptLoad(currentCount, recoveryFlag);
                    }
                }
            }

            notDead = false;
            //we are no longer trying to load to the DAQ.  Wait for output generation to finish, and then clear everything.
            //Debugger.Write("ending buffer calculation processing queue thread for " + this.ToString());
            // buffLoadTask.CounterOutput -= tt;
            lock (taskLock)
            {
                try
                {
                    if ((analogTasks != null))
                        if (analogTasks[0] != null)
                            analogTasks[0].WaitUntilDone(1000);
                        else
                        {
                            if (digitalTasks != null)
                                if (digitalTasks[0] != null)
                                    digitalTasks[0].WaitUntilDone(1000);
                        }
                }
                catch (Exception me) 
                {
                    ClearTasks();
                }
            }
            FinishStimulation(new DoWorkEventArgs(null));

        }

        //populates the buffers with commands based on the named buffer load, but only if we are generating output
        private bool AttemptLoad(int name, bool recoveryFlag)
        {
            //if (recoveryFlag)
            //    Console.WriteLine(this.ToString() + " recovery flag was high for load " + name);

            //check to see if our output tasks are generating anything
            if (GetCurrentSamplePrivate() == 0)
            {
                // Console.WriteLine(this.ToString() + " looks like we haven't output anything yet " + name.ToString());
                return recoveryFlag;//in case we accidentally start this before generation is go
            }

            //as we always assume 2 buffer loads of zeros have gone out before we started generating outputs:
            numBuffLoadsCompleted = (ulong)(name + 2);


            if (recoveryFlag)//flag is high if we are coming off of a restart
            {
                double stamp = Debugger.GetTimeMS();
                double till = (double)(name * (1000 * (double)BUFFSIZE / (double)STIM_SAMPLING_FREQ));
                // Console.WriteLine(this.ToString() + " recovery flag was high: till " + (till) + " stamp: " + stamp + " running: " + running);
                if (till < stamp)
                {
                    // Console.WriteLine(this.ToString() +" "+ numBuffLoadsCompleted.ToString() + " was recovering with offset. " + name.ToString());
                    return recoveryFlag;//dont do anything, this is the wrong start point
                }
                //recoveryFlag = recoveryFlag;
            }
            if ((numBuffLoadsCompleted > numBuffLoadsRequired) & !immortal)
                running = false;
            //Console.WriteLine(this.ToString() + " tick "+Debugger.GetTimeMS()+ ": " + name + " nblc " + numBuffLoadsCompleted);
            // Console.WriteLine(this.ToString() + " tick " + Debugger.GetTimeMS() + ": to apply nblc " + numBuffLoadsCompleted);
            //
            if (running)//if we haven't finished the necessary buffer loads
            {
                //Debugger.Write("trying " + name);
                //  Console.WriteLine(Convert.ToString(tickDiff) + ": " + this.ToString() + " DAQ half-load event.");
                recoveryFlag = PopulateBufferAppending(true, false, recoveryFlag);

            }
            else
            {
                //Debugger.Write("zeroing " + name);
                //if the buffer is no longer running, then write out a bunch of zeros
                recoveryFlag = ZeroOut();


            }
            //if we finish successfully, than we can lower the recoveryFlag.
            //if (recoveryFlag)
            //    Console.WriteLine(this.ToString() + " recovery flag was high, now lowered for load " + name);
            return recoveryFlag;

        }

        internal void restartBufferInternal()
        {
            //lock (taskLock)
            {
                restartBuffer();
            }
        }

        //clear, reconfigure buffers, fill buffers with zeros, start tasks for triggering upon the next buff load task tick
        protected void restartBuffer()
        {

            //clears, reconfigures tasks
            ClearTasks();
            SetupTasks();

            //write out empty buffers and start each task for generation
            for (int i = 0; i < analogWriters.Length; i++)
            {

                //analogTasks[i].Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(analogTasks[i].Timing.ReferenceClockSource, DigitalEdgeStartTriggerEdge.Rising);
                analogWriters[i].WriteMultiSample(false, new double[abuffs.ElementAt(i).GetLength(0), abuffs.ElementAt(i).GetLength(1)]);
                analogWriters[i].WriteMultiSample(false, new double[abuffs.ElementAt(i).GetLength(0), abuffs.ElementAt(i).GetLength(1)]);

                analogTasks[i].Control(TaskAction.Start);
                //analogTasks[i].Start();


            }
            for (int i = 0; i < digitalWriters.Length; i++)
            {

                digitalWriters[i].WriteMultiSamplePort(false, new uint[dbuffs.ElementAt(i).GetLength(0)]);
                digitalWriters[i].WriteMultiSamplePort(false, new uint[dbuffs.ElementAt(i).GetLength(0)]);

                //digitalWriters[i].WriteMultiSamplePort(false, dbuffs.ElementAt(i));
                digitalTasks[i].Start();
            }
            numBuffLoadsThisRun = numBuffLoadsCompleted;
            //Debugger.Write("restart");

        }


        //event raised if we are done.
        private void FinishStimulation(EventArgs e)
        {
            if (StimulationComplete != null)
            {
                // Stop the tasks and dispose of them


                // Tell NR that stimulation has finished
                StimulationComplete(this, e);
            }
        }



        //called each bufferload, that is, each time the buffload task fires off
        //creates a set of empty buffers (arrays) for each task, and then goes through
        //the outerbuffer (list) and looks for events that need to be applied during 
        //this particular load.  Those events are then written to the buffer
        private bool first;
        private double[,] areserve;

        private bool PopulateBufferAppending(bool firstTime, bool zeroOnly, bool recoveryFlag)
        {

            //done = false;//flag to say that we are inside the populate buffer appending code

            //reliability testing:
            //Debugger.Write(this.ToString() + " pre-start");
            //System.Threading.Thread.Sleep(100);

            try//main try block
            {

                //pbaLock.
                try
                {

                    //Debugger.Write("samples out: " + GetCurrentSamplePrivate());

                }
                catch (Exception me)
                { }
                //Debugger.Write(this.ToString() + " start");

                //double start;
                //if (running)
                //    start = GetTime();
                //else
                //    start = 0;
                abuffs = new List<double[,]>();
                dbuffs = new List<uint[]>();

                foreach (Task n in analogTasks)
                {
                    double[,] abuff = new double[n.AOChannels.ICollection_Count, BUFFSIZE];
                    abuffs.Add(abuff);

                }
                foreach (Task n in digitalTasks)
                {
                    uint[] dbuff = new uint[BUFFSIZE];
                    dbuffs.Add(dbuff);

                }
                if (!zeroOnly)
                    if (recoveryFlag)
                        calculateBuffer(abuffs, dbuffs, recoveryFlag);
                    else
                        calculateBuffer(abuffs, dbuffs, recoveryFlag);
                recoveryFlag = false;
                // Console.WriteLine(this.ToString() + " tick " + Debugger.GetTimeMS() + ": calculated nblc " + numBuffLoadsCompleted);
                //should only try this if we know that we can write- for instance, if an error occurred preventing us from generating samples, but the buffers were full, we could lock here.

                #region buffer writing
                for (int i = 0; i < analogWriters.Length; i++)
                {
                    if (firstTime)
                        analogWriters[i].WriteMultiSample(false, abuffs.ElementAt(i));
                    else
                        analogWriters[i].BeginWriteMultiSample(false, abuffs.ElementAt(i), null, null);// WriteMultiSample(false, abuffs.ElementAt(i));
                }
                //Debugger.Write(this.ToString() + " analog written");

                for (int i = 0; i < digitalWriters.Length; i++)
                {
                    if (firstTime)
                        digitalWriters[i].WriteMultiSamplePort(false, dbuffs.ElementAt(i));
                    else
                        digitalWriters[i].BeginWriteMultiSamplePort(false, dbuffs.ElementAt(i), null, null);// WriteMultiSamplePort(false, dbuffs.ElementAt(i));
                }
                //Debugger.Write(this.ToString() + " digital written");
                #endregion
                //  Console.WriteLine(this.ToString() + " tick " + Debugger.GetTimeMS() + ": applied nblc " + numBuffLoadsCompleted);

                //if (!IsDone())
                //{
                //    int buff = (int)(((int)((numBuffLoadsCompleted -numBuffLoadsThisRun)* BUFFSIZE) - (int)GetCurrentSample()));
                //    //
                //    Debugger.Write("buffer level: " + buff);
                //}

            }
            catch (DaqException de)//for some reason this most recent buffer load caused a hardware error.
            {

                if (robust & running)
                {

                    // Debugger.Write(this.ToString()+ "restarting output after error: " + de.Message);
                    // Console.WriteLine(this.ToString() + " recover at " + Debugger.GetTimeMS().ToString());
                    Recover();
                    //  Console.WriteLine(this.ToString() + " recovered at " + Debugger.GetTimeMS().ToString());
                    //Debugger.Write("tasks reset, waiting on start");
                    recoveryFlag = true;
                }
                else
                {
                    BufferFailure(de.Message);
                }
            }
            return recoveryFlag;
        }

        internal virtual void calculateBuffer(List<double[,]> abuffs, List<uint[]> dbuffs, bool recoveryFlag)
        {

            //timing/debugging stuff
            double bufferEvent = 0;
            // double loadBuffers = 0;
            // double analogbu = 0;



            bufferIndex = 0;
            //double clearBuffers = 0;
            //if (running)
            //    clearBuffers = this.GetTime(); 


            //if we don't have a stimulus 'on deck', look for one (needed for state change buffers).
            if ((outerbuffer.Count > 0) & (nextStim == null))
            {

                nextStim = outerbuffer.ElementAt(0);

            }
            if (recoveryFlag)
                FinishStim(ref currentStim, ref anEventValues, ref digEventValues);//if the recovery flag was high, then the 'current stim' is out of date
            //are we in the middle of a stimulus?  if so, finish as much as you can
            if (currentStim != null)
            {
                //   MessageBox.Show("examining first stim");
                bool finished = ApplyCurrentStimulus(currentStim, abuffs, dbuffs, ref numSampWrittenForCurrentStim, ref  bufferIndex);

                if (finished)
                {
                    FinishStim(ref currentStim, ref anEventValues, ref digEventValues);
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
                    bool finished = ApplyCurrentStimulus(currentStim, abuffs, dbuffs, ref numSampWrittenForCurrentStim, ref  bufferIndex);
                    if (finished)
                    {
                        FinishStim(ref currentStim, ref anEventValues, ref digEventValues);
                    }
                }
            }

            //
            //if (running)
            //    loadBuffers = this.GetTime(); 

            //congratulations!  we finished the buffer!
            //numBuffLoadsCompleted++;
            currentSample = numBuffLoadsCompleted * BUFFSIZE;
            // Check if protocol is completed


            OnBufferLoad(EventArgs.Empty);

            if (running)
                bufferEvent = this.GetTimePrivate();

            if (first)
            {
                first = false;
                //areserve = new double[abuffs.ElementAt(0).GetLength(0), abuffs.ElementAt(0).GetLength(1)];
                //for (int i = 0; i < abuffs.ElementAt(0).GetLength(0); i++)
                //    for (int j = 0; j < abuffs.ElementAt(0).GetLength(1); j++)
                //    {
                //        areserve[i, j] = abuffs.ElementAt(0)[i, j];
                //    }
            }
            //write buffers
            //    if (!buffLoadTask.IsDone)
            //      Debugger.Write(this.ToString() + " buffers calculated for load " + numBuffLoadsCompleted + " via internal count ");//+ Convert.ToDouble(buffLoadTask.COChannels[0].Count) + " via counter"
            //bufferLock.ExitWriteLock();


        }

        protected virtual void Recover()
        {
            lock (namesLock)
            {
                ClearQueue();


                restartBuffer();
            }


        }

        internal void ClearQueueInternal()
        {
            lock (namesLock)
                ClearQueue();
        }

        protected void ClearQueue()
        {
            names.Clear();
            sem = new Semaphore(0, semSize);
        }

        //bool OMGhack;
        //write as much of the current stimulus as possible
        //agnostic as to whether or not you've finished this stimulus or not.
        //returns if finished the stimulus or not
        private bool ApplyCurrentStimulus(T currentStim, List<double[,]> abuffs, List<uint[]> dbuffs, ref uint numSampWrittenForCurrentStim, ref ulong bufferIndex)
        {

            //how many samples should we write, including blanking?
            int samples2Finish;
            if (currentStim.SampleDuration > 0)//this is a non-instantaneous event
                samples2Finish = (int)currentStim.SampleDuration - (int)numSampWrittenForCurrentStim;
            else//this is an instantaneous event, ie a state change
            {
                //if we have another event on deck, and we aren't done with events
                if ((nextStim != null) & ((numEventsWritten < totalEvents) || immortal))

                    samples2Finish = (int)nextStim.SampleIndex - (int)currentStim.SampleIndex - (int)numSampWrittenForCurrentStim;
                else
                {
                    //if not, than hold this state change throughout the duration of the buffer
                    samples2Finish = (int)BUFFSIZE;
                }
            }
            if (nextStim != null)
                if (currentStim.SampleIndex > nextStim.SampleIndex)
                    throw new Exception("NROutBuffer exception: trying to stimulate starting at sample " + currentStim.SampleIndex.ToString() + " and ending at sample " + nextStim.SampleIndex.ToString());
            //  Console.WriteLine(outst+ samples2Finish.ToString());

            //write samples to the buffer
            for (int i = 0; (i < samples2Finish) & (bufferIndex < BUFFSIZE); i++)
            {
                WriteSample(anEventValues, digEventValues, abuffs, dbuffs, ref numSampWrittenForCurrentStim, ref bufferIndex);

            }

            return (bufferIndex < BUFFSIZE);
        }

        //find the next stimulus to apply
        //returns true if there is a stimulus within this bufferload
        private bool NextStimulusAppending()
        {


            // try
            {
                //lock (this)

                while (outerbuffer.Count > 0)//buffer isn't empty
                {
                    //if we have a stimulus within range
                    if (outerbuffer.ElementAt(0).SampleIndex < (numBuffLoadsCompleted) * BUFFSIZE)
                    {

                        currentStim = (T)outerbuffer.ElementAt(0).DeepClone();
                        // Debugger.Write("grabbed stim at " + currentStim.sampleIndex.ToString() + " for NBLC " + numBuffLoadsCompleted);
                        bufferLock.EnterWriteLock();
                        outerbuffer.RemoveAt(0);
                        bufferLock.ExitWriteLock();
                        if (outerbuffer.Count > 0)
                        {
                            nextStim = outerbuffer.ElementAt(0);//we don't need a copy as we don't expect this element to change.
                        }
                        else
                        {
                            nextStim = null;
                        }
                        //turn the new stim into something we can throw at the DAQs
                        WriteEvent(currentStim, ref  anEventValues, ref digEventValues);


                        if (outerbuffer.Count == (queueThreshold - 1))
                            OnThreshold(EventArgs.Empty);

                        numSampWrittenForCurrentStim = 0;
                        bufferIndex = currentStim.SampleIndex - (numBuffLoadsCompleted - 1) * BUFFSIZE;//move to beginning of this stimulus
                        if (currentStim.SampleIndex < (numBuffLoadsCompleted - 1) * BUFFSIZE)//check to make sure we aren't attempting to stimulate in the past
                        {
                            //MessageBox.Show("trying to write an expired stimulus: stimulation at sample no " + currentStim.StimSample + " was written at time " + numBuffLoadsCompleted * BUFFSIZE + ", on channel " + currentStim.channel);
                            if (robust)
                            {
                                //  Debugger.Write("trying to write an expired stimulus: stimulation at sample no " + currentStim.sampleIndex + " was written at time " + numBuffLoadsCompleted * BUFFSIZE);
                                continue;//try and see if the next stimulus is any good.
                            }
                            else
                            {
                                Console.WriteLine("WARNING. Trying to write an expired stimulus: stimulation at sample no " + currentStim.SampleIndex + " was written at time " + numBuffLoadsCompleted * BUFFSIZE);
                            }
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
                //buffer is empty

                currentStim = null;//we aren't currently stimulating

                numSampWrittenForCurrentStim = 0;//we haven't written anything
                bufferIndex = BUFFSIZE;//we are done with this buffer
                return false;

            }
            // Console.WriteLine("/"+currentStim.sampleIndex.ToString() + " - " + nextStim.sampleIndex.ToString());
            //  Console.WriteLine(outst+ samples2Finish.ToString());
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //    return false;
            //}

        }

        //called whenver we finish a particular NREvent
        private void FinishStim(ref T currentStim, ref List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            //more explicit garbage collection
            //  Console.WriteLine("stim finished");
            if (anEventValues != null)
                for (int i = 0; i < anEventValues.Count; i++)
                {
                    double[,] tmp = anEventValues.ElementAt(i);
                    tmp = null;
                }
            if (digEventValues != null)
                for (int i = 0; i < digEventValues.Count; i++)
                {
                    uint[] tmp = digEventValues.ElementAt(i);
                    tmp = null;
                }
            //    Debugger.Write("finished a stim  at time " + (double)currentStim.sampleIndex / 100000);
            currentStim = null;//we aren't currently stimulating

        }

        //TODO:  needs error checking to make sure that the writeEvent created buffer matches up with the buffers created for the tasks.
        private void WriteSample(List<double[,]> anEventValues, List<uint[]> digEventValues, List<double[,]> abuffs, List<uint[]> dbuffs, ref uint numSampWrittenForCurrentStim, ref ulong bufferIndex)
        {
            if (anEventValues != null)
                for (int i = 0; i < abuffs.Count; i++)
                {
                    for (int j = 0; j < abuffs.ElementAt(i).GetLength(0); j++)
                        if (currentStim.SampleDuration > 0)
                            abuffs.ElementAt(i)[j, (int)bufferIndex] = anEventValues.ElementAt(i)[j, numSampWrittenForCurrentStim];
                        else//state change mode
                            abuffs.ElementAt(i)[j, (int)bufferIndex] = anEventValues.ElementAt(i)[j, 0];
                }
            if (digEventValues != null)
                for (int i = 0; i < dbuffs.Count; i++)
                {
                    //for (int j = 0; j < dbuffs.ElementAt(i).GetLength(1); j++)
                    if (currentStim.SampleDuration > 0)
                        dbuffs.ElementAt(i)[(int)bufferIndex] = digEventValues.ElementAt(i)[numSampWrittenForCurrentStim];
                    else//state change mode
                        dbuffs.ElementAt(i)[(int)bufferIndex] = digEventValues.ElementAt(i)[0];
                }

            numSampWrittenForCurrentStim++;
            bufferIndex++;

        }

        private bool ZeroOut()
        {
            // outerbuffer.Clear();
            return PopulateBufferAppending(true, true, false);
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
        private void BufferFailure(string message)
        {
            if (running)
            {
                running = false;
                immortal = false;//if we aren't doing this in a robust manner, then we should just stop now.
                //Debugger.Write(this.ToString() + " FAIL " + message);
                MessageBox.Show("unhandled daq exception on " + this.ToString() +
                    ": " + message + ".  Check log to figure out why.");
            }
            else
            {
                //Debugger.Write(this.ToString() + " FAIL " + message);
            }
        }



    }
        #endregion
}
