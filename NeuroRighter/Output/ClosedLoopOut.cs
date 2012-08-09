using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using NeuroRighter.Server;
using NationalInstruments.DAQmx;
using NeuroRighter.Log;
using NeuroRighter;
using NeuroRighter.NeuroRighterTask;

namespace NeuroRighter.Output
{
    class ClosedLoopOut
    {
        private NRTask CLE;
        private int outputSampFreq;
        private DataSrv DatSrv;
        private StimSrv StimSrv;
        private Logger Debugger;
        
        // waveform that gets ripped off NR UI
        bool useManStimWave;
        internal double[] guiWave;

        //private variables
        
        private Task buffLoadTask;
        private string NRFilePath;
        private bool NRRecording;
        private NeuroRighter NR;

        internal ClosedLoopOut(NRTask CLE, int fs, DataSrv DatSrv, StimSrv StimSrv, Task buffLoadTask, Logger Debugger, string NRFilePath, bool NRRecording, NeuroRighter NR)
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

        internal ClosedLoopOut(NRTask CLE, int fs, DataSrv DatSrv, StimSrv StimSrv, Task buffLoadTask, Logger Debugger, string NRFilePath, bool NRRecording, NeuroRighter NR, double[] standardWave)
            : this(CLE, fs, DatSrv, StimSrv, buffLoadTask,Debugger,NRFilePath, NRRecording, NR)
        {
            this.guiWave = standardWave;
            this.useManStimWave = true;
        }

        internal void Start()
        {
            try
            {
                CLE.Grab(DatSrv, StimSrv, Debugger, NRFilePath, NRRecording,NR);
                if (useManStimWave)
                    CLE.GrabWave(guiWave);
                CLE.Setup();//run the user code
                if (buffLoadTask != null)//if the buffloadtask is running, we need to grab on to it.
                {
                    buffLoadTask.CounterOutput += new CounterOutputEventHandler(CLE.Loop);
                   
                }
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
