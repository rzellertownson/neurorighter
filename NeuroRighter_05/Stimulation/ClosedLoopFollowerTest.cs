using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;

namespace NeuroRighter.Stimulation
{
    sealed class ClosedLoopFollowerTest
    {
        private StimPulse pulse;
        private List<int> channels;
        private double voltage;
        private double blankingTime;
        private Random rand;
        private BackgroundWorker _bgWorker;
        private bool _isCancelled;
        private Task StimAnalogTask;
        private Task StimDigitalTask;
        private AnalogMultiChannelWriter StimAnalogWriter;
        private DigitalSingleChannelWriter StimDigitalWriter;
        private StreamWriter outputFileWriter;

        private double testFreq;
        private double freqStep;
        private double prevValue;
        private double currValue;
        private double prevDiff;
        private double lastDirection; //+1 is positive, -1 is negative
        private List<SpikeWaveform> waveforms;

        private const double PERCENT_THRESHOLD = 0.01; //How close things have to be to be done with algorithm
        private const double START_FREQ = 1; //in Hz
        private const double START_FREQ_STEP = 10; //in Hz
        private const double MIN_FREQUENCY = 0.1;
        private const double MAX_FREQUENCY = 300;
        private const int MAX_NUM_ITERATIONS = 100;
        private const int PULSE_WIDTH = 400;
        private const int INTERPULSE_DURATION = 0;
        private const double OFFSET_VOLTAGE = 0.0;
        private const int PADDING = 100;
        private const int MIN_NUM_PULSES = 10;
        private const double MIN_DURATION = 20; //in seconds
        private const int TIME_BETWEEN_TRAINS = 20 * 1000; //in ms

        internal delegate void ProgressChangedHandler(object sender, int channel, double frequency, double metric, double metricChanged);
        internal event ProgressChangedHandler alertProgressChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler alertAllFinished;

        private NeuroRighter.spikesAcquiredHandler eventHandler;

        internal ClosedLoopFollowerTest(List<int> channels, double voltage, Task StimAnalogTask, Task StimDigitalTask, 
            AnalogMultiChannelWriter StimAnalogWriter, DigitalSingleChannelWriter StimDigitalWriter, double blankingTime)
        {
            //Save tasks and writers locally
            this.StimAnalogTask = StimAnalogTask;
            this.StimDigitalTask = StimDigitalTask;
            this.StimAnalogWriter = StimAnalogWriter;
            this.StimDigitalWriter = StimDigitalWriter;

            this.voltage = voltage;
            this.channels = channels;
            this.blankingTime = blankingTime; //in seconds

            waveforms = new List<SpikeWaveform>();
        }

        internal void Start()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "txt";
            sfd.Filter = "Text files (*.txt)|*.txt";
            sfd.FileName = "Experiment001";

            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                outputFileWriter = new StreamWriter(sfd.OpenFile());

                rand = new Random();

                _bgWorker = new BackgroundWorker();
                _bgWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
                _bgWorker.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
                _bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                _bgWorker.WorkerSupportsCancellation = true;
                _bgWorker.WorkerReportsProgress = true;
                _bgWorker.RunWorkerAsync();
            }
        }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            int tries = 0;
            bool isDone;

            //Print file header info
            outputFileWriter.WriteLine("Closed-loop Follower Experiment\r\n\r\nProgrammed by John Rolston (rolston2@gmail.com)\r\n\r\n");

            //Get starting time
            outputFileWriter.WriteLine("Starting time: " + DateTime.Now);
            outputFileWriter.Flush();

            while (channels.Count > 0 && !_isCancelled && !_bgWorker.CancellationPending)
            {
                //Reset some variables
                prevValue = double.NaN;
                currValue = double.NaN;
                lastDirection = 1.0;
                prevDiff = double.NaN;
                testFreq = START_FREQ;
                freqStep = START_FREQ_STEP;
                tries = 0;
                isDone = false;

                //Pick a stim channel combo
                int index = rand.Next(0, channels.Count);
                outputFileWriter.WriteLine("\r\n\r\nChannel #: " + channels[index] + "\r\n");

                while (!isDone && !_isCancelled && !_bgWorker.CancellationPending)
                {
                    lock (this)
                    {
                        if (!_isCancelled && !_bgWorker.CancellationPending)
                        {
                            //Ensure we have either MIN_NUM_PULSES or MIN_DURATION of train, then create pulse
                            int numPulses = (int)(testFreq * MIN_DURATION < MIN_NUM_PULSES ? MIN_NUM_PULSES : testFreq * MIN_DURATION);
                            pulse = new StimPulse(PULSE_WIDTH, PULSE_WIDTH, voltage, -voltage, channels[index], numPulses, testFreq, OFFSET_VOLTAGE,
                                INTERPULSE_DURATION, PADDING, PADDING, false);

                            //Write to file
                            outputFileWriter.WriteLine("Test Frequency (Hz): " + testFreq);
                            outputFileWriter.WriteLine("# Pulses: " + numPulses);
                            outputFileWriter.Flush();

                            //Populate with trigger
                            pulse.populate(true);

                            //Set timing of tasks
                            StimAnalogTask.Timing.SamplesPerChannel = (int)(pulse.numPulses * StimPulse.STIM_SAMPLING_FREQ / pulse.rate);
                            StimDigitalTask.Timing.SamplesPerChannel = (int)(pulse.numPulses * StimPulse.STIM_SAMPLING_FREQ / pulse.rate);

                            //Deliver pulse
                            StimAnalogWriter.WriteMultiSample(true, pulse.analogPulse);
                            if (Properties.Settings.Default.StimPortBandwidth == 32)
                                StimDigitalWriter.WriteMultiSamplePort(true, pulse.digitalData);
                            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                                StimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(pulse.digitalData));
                            //StimDigitalTask.WaitUntilDone();
                            //StimAnalogTask.WaitUntilDone();
                            while (!StimDigitalTask.IsDone && !_bgWorker.CancellationPending && !_isCancelled) Thread.Sleep(10);
                            StimAnalogTask.Stop();
                            StimDigitalTask.Stop();
                        }


                        if (!_bgWorker.CancellationPending && !_isCancelled)
                        {
                            //Calculate metric
                            currValue = APRAW();

                            //Calculate difference between current and last iteration
                            double diff = double.NaN;
                            if (!double.IsNaN(prevValue))
                                diff = (currValue - prevValue) / prevValue;

                            //Report progress
                            _bgWorker.ReportProgress(0, new double[] { channels[index], testFreq, currValue, diff });

                            //Write to file
                            outputFileWriter.WriteLine("\tMetric Value: " + currValue.ToString("F2"));
                            outputFileWriter.WriteLine("\tDifference (%): " + (diff * 100.0).ToString("F2") + "\r\n");
                            outputFileWriter.Flush();

                            //See if we're done
                            if (++tries > MAX_NUM_ITERATIONS || Math.Abs(diff) < PERCENT_THRESHOLD) isDone = true;
                            else
                            {
                                if (!double.IsNaN(prevDiff) && diff * prevDiff < 0) //Change of signs
                                    freqStep *= 0.75; //Cut frequency step

                                if (double.IsNaN(diff) || diff > 0)
                                {
                                    testFreq += freqStep * lastDirection;
                                    //lastDirection stays same
                                }
                                else
                                {
                                    testFreq -= freqStep * lastDirection;
                                    lastDirection *= -1;
                                }

                                if (testFreq < MIN_FREQUENCY) testFreq = MIN_FREQUENCY;
                                else if (testFreq > MAX_FREQUENCY) testFreq = MAX_FREQUENCY;

                                //Store previous values
                                prevDiff = diff;
                                prevValue = currValue;
                            }
                        }
                    }
                }
                channels.RemoveAt(index);
            }
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double[] values = e.UserState as double[];
            alertProgressChanged(this, (int)values[0], values[1], values[2], values[3] * 100.0);
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Close file
            outputFileWriter.Flush();
            outputFileWriter.Close();
            outputFileWriter.Dispose();

            if (alertAllFinished != null)
                alertAllFinished(this);
        }

        internal void Stop(NeuroRighter nr)
        {
            nr.spikesAcquired -= eventHandler;

            _isCancelled = true;
            _bgWorker.CancelAsync();

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

            outputFileWriter.Flush();
            outputFileWriter.Close();
            outputFileWriter.Dispose();
        }

        internal void linkToSpikes(NeuroRighter nr)
        {
            eventHandler = new NeuroRighter.spikesAcquiredHandler(spikeAcquired);
            nr.spikesAcquired += eventHandler;
        }
        private void spikeAcquired(object sender, bool inTrigger)
        {
            if (!_isCancelled && _bgWorker!= null && !_bgWorker.CancellationPending)
            {

                NeuroRighter nr = (NeuroRighter)sender;
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
            }
        }

        private double APRAW()
        {
            double length = (pulse.analogPulse.GetLength(1) * (double)pulse.numPulses) / (double)StimPulse.STIM_SAMPLING_FREQ; //in seconds
            length -= blankingTime * pulse.numPulses; //discount for time when spikes could not have been detected

            double value = waveforms.Count / length;

            //Clear waveforms
            waveforms.Clear();

            return value;
        }
    }
}
