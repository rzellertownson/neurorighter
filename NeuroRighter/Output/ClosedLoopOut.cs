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
        private BackgroundWorker bw;
       
        public Boolean isCancelled;
        private Boolean bw_returned = false;
        private AutoResetEvent _blockExecution = new AutoResetEvent(false);
        private Task buffLoadTask;
        private string NRFilePath;
        private bool NRRecording;

        //Event Handling
        internal delegate void ProgressChangedHandler(object sender, int percentage);
        internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler AlertAllFinished;


        internal ClosedLoopOut(ClosedLoopExperiment CLE, int fs, NRDataSrv DatSrv, NRStimSrv StimSrv, Task buffLoadTask, RealTimeDebugger Debugger, string NRFilePath, bool NRRecording)
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
            
        }

        internal ClosedLoopOut(ClosedLoopExperiment CLE, int fs, NRDataSrv DatSrv, NRStimSrv StimSrv, Task buffLoadTask, RealTimeDebugger Debugger, string NRFilePath, bool NRRecording, double[] standardWave)
            : this(CLE, fs, DatSrv, StimSrv, buffLoadTask,Debugger,NRFilePath, NRRecording)
        {
            this.guiWave = standardWave;
            this.useManStimWave = true;
        }
        internal void Start()
        {
            // Setup BGW
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;

            try
            {

                bw_returned = false;
                CLE.Grab(DatSrv, StimSrv, Debugger, outputSampFreq, NRFilePath, NRRecording);

                if (useManStimWave)
                    CLE.GrabWave(guiWave);

                CLE.Setup();//run the user code
                buffLoadTask.CounterOutput += new CounterOutputEventHandler(CLE.Loop);
                CLE.Start();//set the 'running' bool to true;


            }
            catch (Exception me)
            {
                MessageBox.Show("***Error while running closed loop experiment.*** \r \r" + me.Message);
            }
            
            bw.RunWorkerAsync();
        }
        internal void Stop()
        {
            CLE.Stop();
            bw.CancelAsync();
            while (!bw_returned)//wait for the pnpcl to say that it is done
            {
                Console.WriteLine("Waiting for Closed loop program to come to grips with it's own mortality...");
                System.Threading.Thread.Sleep(1000);
                
            }
            Console.WriteLine("Closed loop program has admitted that it's time has come");
        }
        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (AlertAllFinished != null) AlertAllFinished(this);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double[] state = (double[])e.UserState;
            if (AlertProgChanged != null) AlertProgChanged(this, e.ProgressPercentage);
        }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {

            while (CLE.Running)
            {
                System.Threading.Thread.Sleep(1000);
            }
            bw_returned = true;

            
        }

    }
}
