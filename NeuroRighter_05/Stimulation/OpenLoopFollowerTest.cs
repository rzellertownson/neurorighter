using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using NationalInstruments.DAQmx;

namespace NeuroRighter.Stimulation
{
    sealed class OpenLoopFollowerTest
    {
        private List<StimPulse> pulses;
        private BackgroundWorker _bgWorker;
        private bool _isCancelled;
        private Random rand;
        private Task StimAnalogTask;
        private Task StimDigitalTask;
        private AnalogMultiChannelWriter StimAnalogWriter;
        private DigitalSingleChannelWriter StimDigitalWriter;
        private double currentPercentComplete;
        private double percentIncrement;

        private const int PULSE_WIDTH = 400;
        private const int INTERPULSE_DURATION = 0;
        private const double OFFSET_VOLTAGE = 0.0;
        private const int PADDING = 100;
        private const int MIN_NUM_PULSES = 100;
        private const double MIN_DURATION = 60; //in seconds
        private const int TIME_BETWEEN_TRAINS = 30 * 1000; //in ms

        internal delegate void ProgressChangedHandler(object sender, int percentage, int channel, double frequency);
        internal event ProgressChangedHandler alertProgressChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler alertAllFinished;

        internal OpenLoopFollowerTest(List<int> channels, List<double> frequencies, double voltage, Task StimAnalogTask, Task StimDigitalTask,
            AnalogMultiChannelWriter StimAnalogWriter, DigitalSingleChannelWriter StimDigitalWriter)
        {
            //Save tasks and writers locally
            this.StimAnalogTask = StimAnalogTask;
            this.StimDigitalTask = StimDigitalTask;
            this.StimAnalogWriter = StimAnalogWriter;
            this.StimDigitalWriter = StimDigitalWriter;

            //Create pulses, but don't actually generate data
            pulses = new List<StimPulse>(channels.Count * frequencies.Count);
            for (int c = 0; c < channels.Count; ++c)
                for (int f = 0; f < frequencies.Count; ++f)
                {
                    //Ensure we have either MIN_NUM_PULSES or MIN_DURATION of train
                    int numPulses = (int)(frequencies[f] * MIN_DURATION < MIN_NUM_PULSES ? MIN_NUM_PULSES : frequencies[f] * MIN_DURATION);
                    pulses.Add(new StimPulse(PULSE_WIDTH, PULSE_WIDTH, voltage, -voltage, channels[c], numPulses, frequencies[f], OFFSET_VOLTAGE,
                        INTERPULSE_DURATION, PADDING, PADDING, false));
                }

            percentIncrement = 100.0 / pulses.Count;
        }

        internal void Start()
        {
            rand = new Random();

            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
            _bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            _bgWorker.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            _bgWorker.WorkerReportsProgress = true;
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.RunWorkerAsync();
        }

        internal void Stop() 
        { 
            _bgWorker.CancelAsync(); 
            _isCancelled = true; 
            
            //Stop tasks if running
            //if (!StimAnalogTask.IsDone) StimAnalogTask.Stop();
            //if (!StimDigitalTask.IsDone) StimDigitalTask.Stop();
            //StimDigitalTask.Control(TaskAction.Abort);
            //StimDigitalTask.Stop();
            //StimAnalogTask.Control(TaskAction.Abort);
            //StimAnalogTask.Stop();

            lock (this)
            {
                if (!StimDigitalTask.IsDone) StimDigitalTask.Stop();
            }

            //De-select channel on mux
            StimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
            StimDigitalTask.Timing.SamplesPerChannel = 3;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                StimDigitalWriter.WriteMultiSamplePort(true, new UInt32[] { 0, 0, 0 });
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                StimDigitalWriter.WriteMultiSamplePort(true, new byte[] { 0, 0, 0 });
            StimDigitalTask.WaitUntilDone();
            StimDigitalTask.Stop();
        }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            while (pulses.Count > 0 && !_isCancelled && !_bgWorker.CancellationPending)
            {
                double[] stateData = new double[2];
                
                lock (this)
                {
                    if (!_isCancelled && !_bgWorker.CancellationPending)
                    {

                        //Pick a stim channel/freq combo
                        int index = rand.Next(0, pulses.Count);

                        //Save state data
                        stateData[0] = pulses[index].channel;
                        stateData[1] = pulses[index].rate;

                        _bgWorker.ReportProgress((int)currentPercentComplete, stateData);

                        //Populate
                        pulses[index].populate();

                        //Set timing of tasks
                        StimAnalogTask.Timing.SamplesPerChannel = (int)(pulses[index].numPulses * StimPulse.STIM_SAMPLING_FREQ / pulses[index].rate);
                        StimDigitalTask.Timing.SamplesPerChannel = (int)(pulses[index].numPulses * StimPulse.STIM_SAMPLING_FREQ / pulses[index].rate);

                        //Deliver pulse
                        StimAnalogWriter.WriteMultiSample(true, pulses[index].analogPulse);
                        if (Properties.Settings.Default.StimPortBandwidth == 32)
                            StimDigitalWriter.WriteMultiSamplePort(true, pulses[index].digitalData);
                        else if (Properties.Settings.Default.StimPortBandwidth == 8)
                            StimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(pulses[index].digitalData));
                        //StimDigitalTask.WaitUntilDone();
                        //StimAnalogTask.WaitUntilDone();
                        while (!StimDigitalTask.IsDone && !_bgWorker.CancellationPending && !_isCancelled) Thread.Sleep(50);
                        StimAnalogTask.Stop();
                        StimDigitalTask.Stop();

                        //Remove pulse
                        pulses[index] = null;
                        pulses.RemoveAt(index);

                    }
                }

                //Report progress
                currentPercentComplete += percentIncrement;
                _bgWorker.ReportProgress((int)currentPercentComplete, stateData);

                //Wait
                if (!_bgWorker.CancellationPending && !_isCancelled && pulses.Count > 0) //If no pulses are left, no nead for waiting period
                    Thread.Sleep(TIME_BETWEEN_TRAINS);
            }
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (alertProgressChanged != null)
            {
                double[] data = (double[])e.UserState;
                alertProgressChanged(this, e.ProgressPercentage, (int)data[0], data[1]);
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (alertAllFinished != null)
                alertAllFinished(this);
        }
    }
}
