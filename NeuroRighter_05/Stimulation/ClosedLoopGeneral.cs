using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.Threading;

namespace NeuroRighter
{
    //contains code for an actual closed loop experiment.  uses methods provided by the closedloop utilities class.
    class ClosedLoopExpt
    {
        //private variables
        private BackgroundWorker bw;
        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;
        private Boolean isCancelled;
        private AutoResetEvent _blockExecution = new AutoResetEvent(false);
        //constructor
        public ClosedLoopExpt(Task stimDigitalTask, Task stimPulseTask, DigitalSingleChannelWriter stimDigitalWriter, AnalogMultiChannelWriter stimAnalogWriter)
        {
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimPulseTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
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
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            while (!isCancelled)
            { 
                //stuff
            }
        }
        

    }
}
