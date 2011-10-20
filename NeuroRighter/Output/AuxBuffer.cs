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
    internal delegate void AuxOutputCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    internal delegate void AuxQueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    internal delegate void AuxDAQLoadCompletedHandler(object sender, EventArgs e);

    /// <summary>
    /// General class for continuously regenerable NR output of auxiliary, analog signals. Used by open-loop stimulation
    /// from file as well as closed loop protocols.
    /// </summary>
    public class AuxBuffer : NROutBuffer<AuxOutEvent>
    {
        double[,] lastAuxOutState = new double[4, 1]; // Holds the place of the auxiliary analog ouputs so they are not reset everytime an 
                                                      // new event is written.
        
        internal bool failflag;
        DigitalBuffer digBuffer;
        internal AuxBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
            : base(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold) {
               // this.digBuffer = digBuffer;
            
            digBuffer = null;
            
            failflag = false;
        }
        
        internal void grabPartner(DigitalBuffer digBuffer)
    {
        this.digBuffer = digBuffer;
    }
        
        //internal bool recoveryInProgress = false;
        protected override void Recover()
        {
            //if we have a digital buffer running simultaneously, we need to recover that one as well (as it is timed off of us)
           
                if (Monitor.TryEnter(recoveryInProgress))//if someone is already trying to recovery, skip this to avoid deadlock
                {
                    try
                    {
                        if (digBuffer != null)
                        {
                            //Debugger.Write(" analog buffer attempted recover: with digital buffer");
                            // digBuffer.recoveryInProgress = true;
                            lock(digBuffer.pbaLock)// wait until digbuffer is not in the middle of a populate buffer appending
                            {
                                Console.WriteLine(this.ToString() + " digbuff pba aquired");
                                ClearQueue();
                                digBuffer.ClearQueueInternal();
                                //clearTasks();
                                //Debugger.Write(" analog buffer attempted recover: analog cleared");
                                //digBuffer.clearTasks();
                                //Debugger.Write(" analog buffer attempted recover: digital cleared");
                                restartBuffer();
                                Debugger.Write(" analog buffer attempted recover: analog restarted");
                                digBuffer.restartBufferInternal();
                                Debugger.Write(" analog buffer attempted recover: digital restarted");
                                //  digBuffer.recoveryInProgress = false;
                            }
                            
                            digBuffer.recoveryFlag = true;

                        }
                        else
                        {
                            ClearQueue();
                            Debugger.Write(" analog buffer attempted recover: no digital buffer");
                            clearTasks();
                            Debugger.Write(" analog buffer attempted recover: analog cleared");
                            restartBuffer();
                            Debugger.Write(" analog buffer attempted recover: analog restarted");
                        }
                        recoveryFlag = true;
                    }
                    finally
                    {
                        Monitor.Exit(recoveryInProgress);
                    }
                }
            
            
        }

        protected override void SetupTasksSpecific(ref Task[] analogTasks,ref  Task[] digitalTasks)
        {


            if (analogTasks != null)
                clearTasks();

            string dev = Properties.Settings.Default.SigOutDev;
            int bufferSize = (int)BUFFSIZE;
            string auxTaskName = "auxOut";
            int sampRate =(int)STIM_SAMPLING_FREQ;
            


            Task analogTask = new Task("analog" + auxTaskName);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao0", "",
                -10, 10, AOVoltageUnits.Volts);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao1", "",
                -10, 10, AOVoltageUnits.Volts);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao2", "",
                -10, 10, AOVoltageUnits.Volts);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao3", "",
                -10, 10, AOVoltageUnits.Volts);

            analogTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                sampRate, SampleClockActiveEdge.Rising,
                SampleQuantityMode.ContinuousSamples,
                bufferSize);

            analogTask.SynchronizeCallbacks = false;

            analogTask.Control(TaskAction.Verify);






            // Sync DO start to AO start
            //if (digProvided)
                //auxTaskMaker.SyncDOStartToAOStart();

            analogTasks = new Task[1];
            analogTasks[0] = analogTask;
            digitalTasks = new Task[0];
        }
        //internal void Setup(AnalogMultiChannelWriter auxOutputWriter, Task auxOutputTask, Task buffLoadTask, RealTimeDebugger Debugger)
        //{
        //    AnalogMultiChannelWriter[] analogWriters = new AnalogMultiChannelWriter[1];
        //    analogWriters[0] = auxOutputWriter;

        //    Task[] analogTasks = new Task[1];
        //    analogTasks[0] = auxOutputTask;

        //    base.Setup(analogWriters,new DigitalSingleChannelWriter[0], analogTasks, new Task[0],  buffLoadTask,Debugger);

        //}

        //with this version only one channel can have a non-zero voltage at a time-  eventually might want to switch to a version where keep voltages from previous
        protected override void WriteEvent(AuxOutEvent stim, ref List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            // Increment the auxilary state when an event is encountered
            anEventValues = new List<double[,]>();
            anEventValues.Add(lastAuxOutState);
            anEventValues.ElementAt(0)[stim.eventChannel, 0] = stim.eventVoltage;
            lastAuxOutState = anEventValues.ElementAt(0);
            digEventValues = null;
        }
        
        
    }
}
