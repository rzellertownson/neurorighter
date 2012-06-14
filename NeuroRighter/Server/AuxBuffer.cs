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
using NeuroRighter.Debug;

namespace NeuroRighter.Server
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
        internal AuxBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold,bool robust)
            : base(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold,robust) {
               // this.digBuffer = digBuffer;
            
            digBuffer = null;
            
            failflag = false;
        }
        
        internal void grabPartner(DigitalBuffer digBuffer)
    {
        this.digBuffer = digBuffer;
    }

        internal override void calculateBuffer(List<double[,]> abuffs, List<uint[]> dbuffs, bool recoveryFlag)
        {
            List<uint[]> dummyd = new List<uint[]>();
            //for (int i = 0; i < dbuffs.Count; i++)
            //{
            //    dummyd.Add(dbuffs.ElementAt(i));
            //}
            List<double[,]> dummya = new List<double[,]>();
            //for (int i = 0; i < abuffs.Count; i++)
            //{
            //    dummya.Add(abuffs.ElementAt(i));
            //}
            
            base.calculateBuffer(abuffs, dummyd, recoveryFlag);
            if (digBuffer != null)
            {
                digBuffer.setNBLC(numBuffLoadsCompleted);
                digBuffer.calculateBuffer(dummya, dbuffs, recoveryFlag);
            }
        }

        //sets up both the analog and digital auxillary tasks
        protected override void SetupTasksSpecific(ref Task[] analogTasks,ref  Task[] digitalTasks)
        {
            if (analogTasks != null)
                ClearTasks();

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
        }
       

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
