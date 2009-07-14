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

        private const int PULSE_WIDTH = 400;
        private const int INTERPULSE_DURATION = 0;
        private const double OFFSET_VOLTAGE = 0.0;
        private const int PADDING = 100;
        private const int MIN_NUM_PULSES = 100;
        private const double MIN_DURATION = 60; //in seconds
        private const int TIME_BETWEEN_TRAINS = 30 * 1000; //in ms

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
        }

        internal void Start()
        {
            rand = new Random();

            _bgWorker = new BackgroundWorker();
            _bgWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.RunWorkerAsync();
        }

        internal void Stop() { _bgWorker.CancelAsync(); _isCancelled = true; }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            while (pulses.Count > 0 && !_isCancelled && !_bgWorker.CancellationPending)
            {
                //Pick a stim channel/freq combo
                int index = rand.Next(0, pulses.Count);

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
                StimDigitalTask.WaitUntilDone();
                StimAnalogTask.WaitUntilDone();
                StimAnalogTask.Stop();
                StimDigitalTask.Stop();

                //Remove pulse
                pulses[index] = null;
                pulses.RemoveAt(index);

                //Wait
                Thread.Sleep(TIME_BETWEEN_TRAINS);
            }
        }
    }
}
