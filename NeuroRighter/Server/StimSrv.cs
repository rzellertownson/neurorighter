// NeuroRighter
// Copyright (c) 2008-2012 Potter Lab
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NationalInstruments.DAQmx;
using System.Threading;
using System.Diagnostics;
using NeuroRighter.Log;

namespace NeuroRighter.Server
{
    /// <summary>
    /// NeuroRighter's stimulus server. Used in open and closed loop protocols for control over all output types.
    /// </summary>
    public class StimSrv
    {
        /// <summary>
        /// Aux analog output buffer.
        /// </summary>
        private AuxBuffer auxOut;

        /// <summary>
        /// Aux digital output buffer.
        /// </summary>
        private DigitalBuffer digitalOut;

        /// <summary>
        /// Electical stimulus (for use with NR's stimulator) output buffer.
        /// </summary>
        private StimBuffer stimOut;

        /// <summary>
        /// The DAC sampling frequency in Hz for all forms of output.
        /// </summary>
        private double sampleFrequencyHz;

        /// <summary>
        /// The DAC polling periods in seconds.
        /// </summary>
        private double dacPollingPeriodSec;

        /// <summary>
        /// The DAC polling periods in samples.
        /// </summary>
        private int dacPollingPeriodSamples;

        // Actual Tasks that play with NI DAQ
        internal Task buffLoadTask;

        // Master timing and triggering task
        internal Task masterTask;
        private Logger debugger;

        private int INNERBUFFSIZE;
        private int STIM_SAMPLING_FREQ;

        /// <summary>
        /// grabs the size of the inner (array) buffer, in samples
        /// </summary>
        /// <returns>size of the inner buffer, in samples</returns>
        public int GetBuffSize()
        {
            return INNERBUFFSIZE;
        }

        /// <summary>
        /// Neurorighter's stimulus/generic output server. Used in open-loop and closed-loop experiments where just-in-time buffering of output signals is required.
        /// </summary>
        /// <param name="INNERBUFFSIZE"> The size of one half of the double output buffer in samples</param>
        /// <param name="STIM_SAMPLING_FREQ">The DAC sampling frequency in Hz for all forms of output</param>
        /// <param name="masterTask">The NI Task to which all of the output clocks are synchronized to</param>
        /// <param name="debugger"> NR's real-time debugger</param>
        /// <param name="robust">used to determine if the StimServer will be recover from failures</param>
        internal StimSrv(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, Task masterTask, Logger debugger, bool robust)
        {
            this.masterTask = masterTask;
            this.INNERBUFFSIZE = INNERBUFFSIZE;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
            int sampblanking = 2;
            int queueThreshold = -1;//no queue thresholds for closed loop
            //create buffers
            auxOut = new AuxBuffer(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold, robust);
            digitalOut = new DigitalBuffer(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold, robust);
            auxOut.grabPartner(digitalOut);
            digitalOut.grabPartner(auxOut);
            stimOut = new StimBuffer(INNERBUFFSIZE, STIM_SAMPLING_FREQ, sampblanking, queueThreshold, robust);
            this.debugger = debugger;

            this.sampleFrequencyHz = Convert.ToDouble(STIM_SAMPLING_FREQ);
            this.dacPollingPeriodSec = Properties.Settings.Default.DACPollingPeriodSec;
            this.dacPollingPeriodSamples = Convert.ToInt32(dacPollingPeriodSec * STIM_SAMPLING_FREQ);
        }

        //create the counter, and set up the buffers based on the hardware configuration
        internal void Setup()
        {
            ConfigureCounter();
            //masterLoad = buffLoadTask.COChannels[0].PulseTerminal;
            //assign tasks to buffers
            if (Properties.Settings.Default.UseAODO)
            {
                //ConfigureAODO(true, masterTask);
                auxOut.immortal = true;
                digitalOut.immortal = true;

                //DigitalOut.Setup(buffLoadTask,  debugger, masterTask);
                auxOut.grabPartner(digitalOut);
                digitalOut.grabPartner(auxOut);
                auxOut.Setup(buffLoadTask, debugger, masterTask);
                auxOut.Start();
                //DigitalOut.Start();
            }

            if (Properties.Settings.Default.UseStimulator)
            {
                //ConfigureStim(masterTask);
                stimOut.immortal = true;
                stimOut.Setup(buffLoadTask, debugger, masterTask);
                stimOut.Start();
            }
        }

        internal void StartAllTasks()
        {
            buffLoadTask.Start();
        }

        internal void StopAllBuffers()
        {
            if (auxOut != null)
                auxOut.Stop();

            if (digitalOut != null)
                digitalOut.Stop();
            if (stimOut != null)
                stimOut.Stop();
            Console.WriteLine("StimSrv: StimSrv output Buffers Stopped");
        }

        internal void StopLoading()
        {

            if (buffLoadTask != null)
            {
                buffLoadTask.Stop();
                buffLoadTask.Dispose();
                buffLoadTask = null;
            }
            Console.WriteLine("StimSrv: buffLoadTask is no more");
            //lock(AuxOut)
            //    lock (DigitalOut)
            //    {
            //        AuxOut.Stop();// Kill();
            //        DigitalOut.Stop();//Kill();
            //    }
            //Console.WriteLine("StimSrv: auxTasks are no more");
            //lock (StimOut)
            //    StimOut.Stop();
            //Console.WriteLine("StimSrv: stimTasks are no more");

        }

        //the counter is a timing signal that goes off once per daq loading period.  It is used to time the start of different tasks, as well as the time
        // that the hardware buffers are filled with user commands. Lastly, the counter serves to trigger events created by the user for closed loop stuff.
        // the counter pumps out it's signal on the 'main' device (the same device that spikes are recorded on), and it in turn started by the main
        //recording task (ie, it starts when you start recording)
        private void ConfigureCounter()
        {
            //configure counter
            if (buffLoadTask != null) { buffLoadTask.Dispose(); buffLoadTask = null; }

            buffLoadTask = new Task("stimBufferTask");

            // Trigger a buffer load event off every edge of this channel
            buffLoadTask.COChannels.CreatePulseChannelFrequency(Properties.Settings.Default.AnalogInDevice[0] + "/ctr1",
                "BufferLoadCounter", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, ((double)STIM_SAMPLING_FREQ / (double)INNERBUFFSIZE) / 2.0, 0.5);
            buffLoadTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);
            buffLoadTask.SynchronizeCallbacks = false;
            buffLoadTask.Timing.ReferenceClockSource = "OnboardClock";
            buffLoadTask.Control(TaskAction.Verify);

            // Syncronize the start to the master recording task
            buffLoadTask.Triggers.ArmStartTrigger.ConfigureDigitalEdgeTrigger(
                masterTask.Triggers.StartTrigger.Terminal, DigitalEdgeArmStartTriggerEdge.Rising);

        }

        #region Accessors
        /// <summary>
        /// Analog event output buffer.
        /// </summary>
        public AuxBuffer AuxOut
        {
            get
            {
                return auxOut;
            }
        }

        /// <summary>
        /// Digital event output buffer.
        /// </summary>
        public DigitalBuffer DigitalOut
        {
            get
            {
                return digitalOut;
            }
        }

        /// <summary>
        /// Electical stimulus (for use with NR's stimulator) output buffer.
        /// </summary>
        public StimBuffer StimOut
        {
            get
            {
                return stimOut;
            }
        }

        /// <summary>
        /// The DAC sampling frequency in Hz for all forms of output.
        /// </summary>
        public double SampleFrequencyHz
        {
            get
            {
                return sampleFrequencyHz;
            }
        }

        /// <summary>
        /// The DAC polling periods in seconds.
        /// </summary>
        public double DACPollingPeriodSec
        {
            get
            {
                return dacPollingPeriodSec;
            }
        }

        /// <summary>
        /// The DAC polling period in samples.
        /// </summary>
        public int DACPollingPeriodSamples
        {
            get
            {
                return dacPollingPeriodSamples;
            }
        }


        #endregion

    }
}
