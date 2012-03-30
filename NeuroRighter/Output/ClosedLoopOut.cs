using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using NeuroRighter.StimSrv;
using NeuroRighter.DatSrv;
using NationalInstruments.DAQmx;
using NeuroRighter.dbg;
using NeuroRighter;

namespace NeuroRighter.Output
{
    class ClosedLoopOut
    {
        private ClosedLoopExperiment CLE;
        private int outputSampFreq;
        private NRDataSrv DatSrv;
        private NRStimSrv StimSrv;
        private RealTimeDebugger Debugger;

        // waveform that gets ripped off NR UI
        bool useManStimWave;
        internal double[] guiWave;

        //private variables
        
        private Task buffLoadTask;
        private string NRFilePath;
        private bool NRRecording;
        private NeuroRighter NR;

        internal ClosedLoopOut(ClosedLoopExperiment CLE, int fs, NRDataSrv DatSrv, NRStimSrv StimSrv, Task buffLoadTask, RealTimeDebugger Debugger, string NRFilePath, bool NRRecording,NeuroRighter NR)
        {
            this.CLE = CLE;
            this.outputSampFreq = fs;
            this.DatSrv = DatSrv;
            this.StimSrv = StimSrv;
            this.useManStimWave = false;
            this.buffLoadTask = buffLoadTask;
            this.Debugger = Debugger;
            this.NRFilePath = NRFilePath;
            this.NRRecording = NRRecording;
            this.NR = NR;
            
        }

        internal ClosedLoopOut(ClosedLoopExperiment CLE, int fs, NRDataSrv DatSrv, NRStimSrv StimSrv, Task buffLoadTask, RealTimeDebugger Debugger, string NRFilePath, bool NRRecording, NeuroRighter NR, double[] standardWave)
            : this(CLE, fs, DatSrv, StimSrv, buffLoadTask,Debugger,NRFilePath, NRRecording, NR)
        {
            this.guiWave = standardWave;
            this.useManStimWave = true;
        }

        internal void Start()
        {
            try
            {
                CLE.Grab(DatSrv, StimSrv, Debugger, outputSampFreq, NRFilePath, NRRecording,NR);
                if (useManStimWave)
                    CLE.GrabWave(guiWave);
                CLE.Setup();//run the user code
                buffLoadTask.CounterOutput += new CounterOutputEventHandler(CLE.Loop);
            }
            catch (Exception me)
            {
                MessageBox.Show("***Error while running closed loop experiment.*** \r \r" + me.Message);
            }
        }

        internal void Stop()
        {
            //buffLoadTask.Stop();
            CLE.Cleanup();
            Console.WriteLine("Closed loop program has admitted that it's time has come");
        }
    }
}
