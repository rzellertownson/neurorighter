using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.StimSrv;
using NeuroRighter.DatSrv;
using NeuroRighter.dbg;
using NeuroRighter;

namespace NeuroRighter.Output
{
    /// <summary>
    /// NeuroRighter's abstract class for user defined closed loop experiments.
    /// <author> Riley Zeller-Townson</author>
    /// </summary>
    public abstract class ClosedLoopExperiment
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

        internal protected void Stop()
        {
            NR.killClosedLoop();
        }

        

        // USER OVERRIDEN CLOSED LOOP METHODS

        /// <summary>
        /// This method must be overriden in a derived closed-loop class.
        /// </summary>
        internal protected abstract void Setup();

        /// <summary>
        /// This method must be overriden in a derived closed-loop class.
        /// </summary>
        internal protected virtual void Loop(object sender, EventArgs e)
        { }

        /// <summary>
        /// This method must be overriden in a derived closed-loop class.
        /// </summary>
        internal protected virtual void Cleanup() {}
        

    }
}
