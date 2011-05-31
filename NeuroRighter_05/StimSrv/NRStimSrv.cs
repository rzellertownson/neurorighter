using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NationalInstruments.DAQmx;
using System.Threading;
using System.Diagnostics;

namespace NeuroRighter.StimSrv
{
    class NRStimSrv
    {
        //stim event buffers- these are what the user interfaces with directly

        internal AuxBuffer AuxOut;
        internal DigitalBuffer DigitalOut;
        internal StimBuffer StimOut;
        
        // Actual Tasks that play with NI DAQ
        internal Task buffLoadTask;
        internal ContStimTask stimTaskMaker;
        internal AuxOutTask auxTaskMaker;

        // Master timing and triggering task
        internal Task masterTask;

        private int INNERBUFFSIZE;
        private int STIM_SAMPLING_FREQ;

        public NRStimSrv(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, Task masterTask)
        {
            this.masterTask = masterTask;
            this.INNERBUFFSIZE = INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
            int sampblanking = 2;
            int queueThreshold = -1;//no queue thresholds for closed loop
            //create buffers
            AuxOut = new AuxBuffer(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold);
            DigitalOut = new DigitalBuffer(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold);
            StimOut = new StimBuffer(INNERBUFFSIZE, STIM_SAMPLING_FREQ, sampblanking, queueThreshold);

            


            //basically, this needs to run, or at least start, the code for all the 'File2X' classes.
        }

        //this method writes the first 2 buffer loads to the daq in preparation
        internal void Setup()
        {
            ConfigureCounter();
            ConfigureStim(masterTask);
            ConfigureAODO(true, masterTask);
                
            //assign tasks to buffers
            AuxOut.immortal = true;
            DigitalOut.immortal = true;
            StimOut.immortal = true;

            
            DigitalOut.Setup(auxTaskMaker.digitalWriter, auxTaskMaker.digitalTask, buffLoadTask);
            AuxOut.Setup(auxTaskMaker.analogWriter, auxTaskMaker.analogTask, buffLoadTask);
            StimOut.Setup(stimTaskMaker.analogWriter, stimTaskMaker.digitalWriter, stimTaskMaker.digitalTask, stimTaskMaker.analogTask, buffLoadTask);

            AuxOut.Start();
            DigitalOut.Start();
            StimOut.Start();
        }

        internal void StartAllTasks()
        {
            buffLoadTask.Start();
        }

        internal void KillAllAODOTasks()
        {
            if (buffLoadTask != null)
            {
                buffLoadTask.Dispose();
                buffLoadTask = null;
            }

            if (stimTaskMaker != null)
            {
                stimTaskMaker.Dispose();
                stimTaskMaker = null;
            }

            if (auxTaskMaker != null)
            {
                auxTaskMaker.Dispose();
                auxTaskMaker = null;
            }

        }

        internal int getBuffSize()
        {
            return INNERBUFFSIZE;
        }
        private void ConfigureCounter()
        {
            //configure counter
            if (buffLoadTask != null) { buffLoadTask.Dispose(); buffLoadTask = null; }
            
            buffLoadTask = new Task("stimBufferTask");

            // Trigger a buffer load event off every edge of this channel
            buffLoadTask.COChannels.CreatePulseChannelFrequency(Properties.Settings.Default.SigOutDev + "/ctr1",
                "BufferLoadCounter", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, ((double)STIM_SAMPLING_FREQ / (double)INNERBUFFSIZE) / 2.0, 0.5);
            buffLoadTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);
            buffLoadTask.SynchronizeCallbacks = false;
            buffLoadTask.Timing.ReferenceClockSource = "OnboardClock";
            buffLoadTask.Control(TaskAction.Verify);

            // Syncronize the start to the master recording task
            buffLoadTask.Triggers.ArmStartTrigger.ConfigureDigitalEdgeTrigger(
                masterTask.Triggers.StartTrigger.Terminal, DigitalEdgeArmStartTriggerEdge.Rising);
            buffLoadTask.CounterOutput += new CounterOutputEventHandler(delegate
            {

               
                Thread thrd = Thread.CurrentThread;

                thrd.Priority = ThreadPriority.Highest;
                    //Console.WriteLine("buffload tick");
            }
                );
        }

        private void ConfigureStim(Task masterTask)
        {

            //configure stim
            // Refresh DAQ tasks as they are needed for file2stim
            if (stimTaskMaker != null)
            {
                stimTaskMaker.Dispose();
                stimTaskMaker = null;
            }

            // Create new DAQ tasks and corresponding writers
            stimTaskMaker = new ContStimTask(Properties.Settings.Default.StimulatorDevice,
                INNERBUFFSIZE);
            stimTaskMaker.MakeAODOTasks("NeuralStim",
                Properties.Settings.Default.StimPortBandwidth,
                STIM_SAMPLING_FREQ);

            // Verify
            stimTaskMaker.VerifyTasks();

            // Sync DO start to AO start
            stimTaskMaker.SyncDOStartToAOStart();

            // Syncronize stimulation with the master task
            stimTaskMaker.SyncTasksToMasterClock(masterTask);
            stimTaskMaker.SyncTasksToMasterStart(masterTask);

            // Create buffer writters
            stimTaskMaker.MakeWriters();

            // Verify
            stimTaskMaker.VerifyTasks();
        }

       private void ConfigureAODO(bool digProvided, Task masterTask)
        {
            //configure stim
            // Refresh DAQ tasks as they are needed for file2stim
            if (auxTaskMaker != null)
            {
                auxTaskMaker.Dispose();
                auxTaskMaker = null;
            }

            // Create new DAQ tasks and corresponding writers
            auxTaskMaker = new AuxOutTask(Properties.Settings.Default.SigOutDev,
                INNERBUFFSIZE);
            auxTaskMaker.MakeAODOTasks("auxOut",
                STIM_SAMPLING_FREQ,
                digProvided);

            // Verify
            auxTaskMaker.VerifyTasks();

            // Sync DO start to AO start
            if (digProvided)
                auxTaskMaker.SyncDOStartToAOStart();

            // Syncronize stimulation with the master task
            auxTaskMaker.SyncTasksToMasterClock(masterTask);
            auxTaskMaker.SyncTasksToMasterStart(masterTask);

            // Create buffer writters
            auxTaskMaker.MakeWriters();

            // Verify
            auxTaskMaker.VerifyTasks();
        }

       



    }
}
