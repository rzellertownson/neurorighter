using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.StimSrv;
using NeuroRighter.DatSrv;
using NeuroRighter.dbg;
using NeuroRighter;

namespace NeuroRighter.StimSrv
{
    /// <summary>
    /// NeuroRighter's abstract class for user defined closed loop experiments.
    /// <author> Riley Zeller-Townson</author>
    /// </summary>
    public abstract class NRTask
    {
        protected NRDataSrv DatSrv;
        protected NRStimSrv StimSrv;
        protected RealTimeDebugger Debugger;
        //public bool Running;
        protected double[] cannedWaveform;
        protected int fs;
        protected string NRFilePath;
        protected bool NRRecording;
        private NeuroRighter NR;

        internal void Grab(NRDataSrv DatSrv, NRStimSrv StimSrv,RealTimeDebugger Debugger ,int fs,string NRFilePath,bool NRRecording, NeuroRighter NR)
        {
            this.DatSrv = DatSrv;
            this.StimSrv = StimSrv;
            this.Debugger = Debugger;
            this.fs = fs;
            this.NRFilePath = NRFilePath;
            this.NRRecording = NRRecording;
            this.NR = NR;//we need a reference back to NR to initiate the stop sequence
        }

        internal void GrabWave(double[] waveform)
        {
            cannedWaveform = waveform;
        }

        /// <summary>
        /// stops the task as if you had clicked the stop button.  Stimulation is stopped, and then the cleanup() method is called
        /// </summary>
        internal protected void Stop()
        {
            NR.killClosedLoop();
        }

        

        // USER OVERRIDEN CLOSED LOOP METHODS

        /// <summary>
        /// Abstract method to initialize the closed-loop protocol.  Executes upon clicking the start button, and finishes execution before
        /// recording, stimulation or the Loop() method execute.  You probably want to use this for setting up a GUI, initializing
        /// data structures, and constructing larger objects/initializing secondary threads.
        /// </summary>
        internal protected abstract void Setup();

        /// <summary>
        /// The meat.  This abstract method gets called repeatedly, after Setup() has completed.  It is triggered off of a clock in the
        /// NI DAQ, and given high priority relative to other threads.  Use this method to update stimulation parameters, grab data
        /// off of the NR data streams, and alert GUIs that updates are available.  Try to offload really intensive processing to 
        /// secondary threads, and just use Loop() for the absolutely essential high-speed updates
        /// </summary>
        internal protected abstract void Loop(object sender, EventArgs e);
       

        /// <summary>
        /// This abstract method will be executed last out of the three overriden methods in this class.  It is called either by clicking the
        /// 'stop' button during closed loop execution, or by calling it directly through some other closed loop method (which will also
        /// force the closed loop protocol to close).  Use this method to close off file streams, dispose objects.
        /// </summary>
        internal protected abstract void Cleanup();
        

    }
}
