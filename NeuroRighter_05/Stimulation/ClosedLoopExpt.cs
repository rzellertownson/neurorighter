using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.Threading;
using pnpCL;

namespace NeuroRighter
{
    //contains background worker stuff.  creates a pnpCL class that uses methods provided by the closedloop utilities file.
    public partial class ClosedLoopExpt
    {
        //private variables
        private BackgroundWorker bw;
        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;
        public Boolean isCancelled;
        private AutoResetEvent _blockExecution = new AutoResetEvent(false);
        private List<SpikeWaveform> waveforms;
        private pnpClosedLoopAbs pnpcl;

        //variables for wavestimming
        internal int[] timeVec; //interstim times (NX1 vector)
        internal int[] channelVec; // stimulation locations (NX1 vector)
        internal double[,] waveMat; // stimulation waveforms (NXM vector, M samples per waveform)

        //Event Handling
        internal delegate void ProgressChangedHandler(object sender, int percentage);
        internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler AlertAllFinished;

        //constructor
        public ClosedLoopExpt(int STIM_SAMPLING_FREQ, Int32 STIMBUFFSIZE,Task stimDigitalTask, Task stimPulseTask, DigitalSingleChannelWriter stimDigitalWriter, AnalogMultiChannelWriter stimAnalogWriter, pnpClosedLoopAbs pnpcl )
        {
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
            this.BUFFSIZE = STIMBUFFSIZE;
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimPulseTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
            this.pnpcl = pnpcl;
        }
       
        //start
            //create backgroundworker
        public void start()
        {
            // Setup BGW
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;
            isCancelled = false;
            waveforms = new List<SpikeWaveform>(100);
            // Run Worker
            bw.RunWorkerAsync();
        }
        //stop
        public void stop()
        {
            isCancelled = true;
            bw.CancelAsync();
        }

        internal void linkToSpikes(NeuroRighter nr) { nr.spikesAcquired += new NeuroRighter.spikesAcquiredHandler(spikeAcquired); }

        private void spikeAcquired(object sender, bool inTrigger)
        {
            NeuroRighter nr = (NeuroRighter)sender;
            lock (this)
            {
                lock (nr)
                {
                    //Add all waveforms to local buffer
                    while (nr.waveforms.Count > 0)
                    {
                        waveforms.Add(nr.waveforms[0]);
#if (DEBUG_LOG)
                        nr.logFile.WriteLine("[BakkumExpt] Waveform added, index: " + nr.waveforms[0].index + "\n\r\tTime: " 
                            + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
                        nr.logFile.Flush();
#endif
                        nr.waveforms.RemoveAt(0);
                    }
                }
                if (!inTrigger)//if the trigger is currently not active
                    _blockExecution.Set();
            }
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
            
            //pnpcl = new pnpClosedLoop();
            pnpcl.grab(this);
            pnpcl.run();
            //simpleExample();
            //spikeCounter();
        }

        #region example experiments
        private void simpleExample()
        {

            initializeStim();//sets stim params for wavestimming
            //int percentProgress = 0;
            List<SpikeWaveform> recording;
            //initialize experiment
            //stimWave sw = new stimWave(100);
            while (!isCancelled)
            {
                //stuff


                

                //set timeVec, channelVec, waveMat, lengthWave for this round
                timeVec = new int[3];
                channelVec = new int[3];
                waveMat = new double[3,90];
                int current_time = 0;
                for (int i = 0; i < 3; i++)
                {
                    timeVec[i] = current_time;
                    current_time += 2000;
                    channelVec[i] = i * 10+1;
                    for (int j = 0; j < 90; j++)
                    {
                        waveMat[i, j] = j/100-0.45;
                    }
                }

                waveStim(timeVec,channelVec,waveMat);//takes the timeVec, channelVec, waveMat and lengWave values and stims with them, as if
                //a .olstim file with those params had been loaded.
                

                //record spikes for 100ms
                recording = record(100);
                //update progress bar
                //percentProgress+=10;
                //percentProgress %= 100;
                bw.ReportProgress(recording.Count%100);
            }
        }

        private void spikeCounter()
        {

            //todo:figure out how the thing is triggered!
            
            while (!isCancelled)
            {
                record(100);   
                int firingRate = 0;
                while (waveforms.Count > 0)
                {
                    ++firingRate; //channels are 0-based
                    waveforms.RemoveAt(0);
                }
                bw.ReportProgress(firingRate);
            }
        }

        #endregion

    }
}
