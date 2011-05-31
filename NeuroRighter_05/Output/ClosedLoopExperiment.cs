using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.StimSrv;
using NeuroRighter.DatSrv;

namespace NeuroRighter.Output
{
    internal abstract class ClosedLoopExperiment
    {
        protected NRDataSrv DatSrv;
        protected NRStimSrv StimSrv;
        protected bool Running;
        protected double[] cannedWaveform;
        protected int fs;
        internal void Grab(NRDataSrv DatSrv, NRStimSrv StimSrv,int fs)
        {
            this.DatSrv = DatSrv;
            this.StimSrv = StimSrv;
            this.fs = fs;
        }
        internal void GrabWave(double[] waveform)
        {
            cannedWaveform = waveform;
        }
        internal void Stop()
        {
            Running = false;
        }
        internal void Start()
        {
            Running = true;
        }

        internal abstract void Run();

        internal virtual void BuffLoadEvent(object sender, EventArgs e)
        { }
        

    }
}
