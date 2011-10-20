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

namespace NeuroRighter.Output
{
    // called when the 2+requested number of buffer loads have occured
    //internal delegate void StimulationCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    //internal delegate void QueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    //internal delegate void DAQLoadCompletedHandler(object sender, EventArgs e);

    /// <summary>
    /// NeuroRighter's standard output buffer for digital signals.
    /// </summary>
    public class DigitalBuffer : NROutBuffer<DigitalOutEvent>
    {
        AuxBuffer analogBuffer;

        internal DigitalBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
            : base(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold) {
                //this.analogBuffer = analogBuffer;
                failflag = false;
        }
       
        internal bool failflag;
        internal void grabPartner(AuxBuffer analogBuffer)//different from the analog buffer version!
        {
            this.analogBuffer = analogBuffer;
            recoveryInProgress = analogBuffer.recoveryInProgress;
        }
        
        //since we need to restart the analog task as well, we need to override the base Recover method to signal to the analog buffer for restart
        protected override void Recover()
        {
            //we know that there is an analog buffer running simultaneously, so we need to restart it as well.

            if (Monitor.TryEnter(recoveryInProgress))//this lock is so analogbuffer doesnt try to restart, but if it has we dont need to do this
            {
                try
                {
                    //Debugger.Write(" digital buffer attempted recover");
                    lock (analogBuffer.pbaLock)//this lock is so we don't try to restart in the middle of a pop.buf.append.
                    {
                        ClearQueue();
                        analogBuffer.ClearQueueInternal();
                        //analogBuffer.clearTasks();
                        //Debugger.Write(" digital buffer attempted recover: analog cleared");
                        //clearTasks();
                        //Debugger.Write(" digital buffer attempted recover: digital cleared");
                        analogBuffer.restartBufferInternal();
                        Debugger.Write(" digital buffer attempted recover: analog restarted");
                        restartBuffer();
                        Debugger.Write(" digital buffer attempted recover: digital cleared");
                        //analogBuffer.recoveryInProgress = false;
                        analogBuffer.failflag = false;
                        analogBuffer.recoveryFlag = true;
                        failflag = false;
                    }
                    recoveryFlag = true;
                }
                finally
                {
                    Monitor.Exit(recoveryInProgress);
                }
            }
        }

        protected override void SetupTasksSpecific(ref Task[] analogTasks, ref Task[] digitalTasks)
        {
            string dev = Properties.Settings.Default.SigOutDev;
            string auxTaskName = "digOut";
            int sampRate =  (int)STIM_SAMPLING_FREQ;
            int bufferSize = (int)BUFFSIZE;


            Task digitalTask;
                digitalTask = new Task("digital" + auxTaskName);
                digitalTask.DOChannels.CreateChannel(dev + "/Port0/line0:31",
                    "Generic Digital Out",
                    ChannelLineGrouping.OneChannelForAllLines);

                // Setup DO clock
                digitalTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                    sampRate,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples,
                    bufferSize);

                digitalTask.SynchronizeCallbacks = false;

                digitalTask.Control(TaskAction.Verify);

            // Verify
            //auxTaskMaker.VerifyTasks();

            // Sync DO start to AO start
            //if (digProvided)
                digitalTask.Timing.ConfigureSampleClock("/" + dev + "/ao/SampleClock",
                    sampRate,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples,
                    bufferSize);
            

            
            digitalTasks = new Task[1];
            digitalTasks[0] = digitalTask;
            

            
                analogTasks = new Task[0];
            
        }

        //internal void Setup(DigitalSingleChannelWriter digitalOutputWriter, Task digitalOutputTask, Task buffLoadTask, RealTimeDebugger Debugger)
        //{
        //    //encapsulate the tasks and writer given into arrays
        //    DigitalSingleChannelWriter[] digitalWriters = new DigitalSingleChannelWriter[1];
        //    digitalWriters[0] = digitalOutputWriter;

        //    Task[] digitalTasks = new Task[1];
        //    digitalTasks[0]=digitalOutputTask;

        //    base.Setup(new AnalogMultiChannelWriter[0],digitalWriters,new Task[0],digitalTasks,buffLoadTask,Debugger);
            

        //}

        protected override void  WriteEvent(DigitalOutEvent stim, ref List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            anEventValues = null;
            digEventValues = new List<uint[]>();
            digEventValues.Add(new uint[1]);
            digEventValues.ElementAt(0)[0] = stim.Byte;
        }
    }

}
