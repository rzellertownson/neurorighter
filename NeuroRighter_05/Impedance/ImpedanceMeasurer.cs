using System;
using System.Collections.Generic;
using System.Text;
using NationalInstruments.DAQmx;
using NationalInstruments.Analysis.Dsp;
using NationalInstruments.Analysis.SignalGeneration;
using NationalInstruments.Analysis.Dsp.Filters;
using System.ComponentModel;
using System.Windows.Forms;
using csmatio.common;
using csmatio.io;
using csmatio.types;

namespace NeuroRighter.Impedance
{
    internal class ImpedanceMeasurer
    {
        private Task impedanceRecord, stimDigitalTask, stimAnalogTask;
        private AnalogSingleChannelReader impedanceReader;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;
        private double[][] impedance; //Stores impedances for each channel, for multiple frequencies
        private double[] freqs;
        private double RCurr, RMeas, RGain;
        private double commandVoltage;
        private int startChannel;
        private int numChannels;
        private double numPeriods;
        private bool isCurrentControlled;
        private bool useBandpassFilter;
        private bool useMatchedFilter;
        private BackgroundWorker bgWorker;

        private const int IMPEDANCE_SAMPLING_RATE = 100000;  //9-24-08: noted that higher sampling rate improves accuracy
                                                             //12-02-09: noted that having this too high (>100000) will cause DAQ buffer problems
                        
        private const double RESOLUTION = 1.5; //Gather frequencies in multiples of RESOLUTION

        internal delegate void ProgressChangedHandler(object sender, int percentage, int channel, double frequency);
        internal event ProgressChangedHandler alertProgressChanged;
        internal delegate void ChannelFinishedHandler(object sender, int channelIndex, int channel, double[][] impedance, double[] freqs);
        internal event ChannelFinishedHandler alertChannelFinished;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler alertAllFinished;

        internal ImpedanceMeasurer()
        {

        }

        internal void getImpedance(double startFreq, double stopFreq, double numPeriods, bool isCurrentControlled, int startChannel, int numChannels,
            double RCurr, double RMeas, double RGain, double commandVoltage, bool useBandpassFilter, bool useMatchedFilter)
        {
            this.numPeriods = numPeriods;
            this.startChannel = startChannel;
            this.numChannels = numChannels;
            this.RCurr = RCurr;
            this.RGain = RGain;
            this.RMeas = RMeas;
            this.commandVoltage = commandVoltage;
            this.isCurrentControlled = isCurrentControlled;
            this.useBandpassFilter = useBandpassFilter;
            this.useMatchedFilter = useMatchedFilter;

            //StartChannel is 1-based
            if (startFreq == stopFreq)
                freqs = new double[1];
            else if ((stopFreq/startFreq)%RESOLUTION == 0) //stopFreq is a RESOLUTION multiple of startFreq
                freqs = new double[Convert.ToInt32(Math.Floor(Math.Log(stopFreq / startFreq) / Math.Log(RESOLUTION))) + 1]; //This determines the number of frequencies counting by doublings
            else //not an exact multiple
                freqs = new double[Convert.ToInt32(Math.Floor(Math.Log(stopFreq / startFreq) / Math.Log(RESOLUTION))) + 2]; //This determines the number of frequencies counting by doublings

            //Populate freqs vector
            freqs[0] = startFreq;
            for (int i = 1; i < freqs.GetLength(0); ++i)
                freqs[i] = freqs[i - 1] * RESOLUTION;
            if (freqs[freqs.Length - 1] > stopFreq) freqs[freqs.Length - 1] = stopFreq;

            //Setup tasks
            impedanceRecord = new Task("Impedance Analog Input Task");
            stimDigitalTask = new Task("stimDigitalTask_impedance");
            stimAnalogTask = new Task("stimAnalogTask_impedance");

            //Choose appropriate input for current/voltage-controlled stimulation
            String inputChannel = (isCurrentControlled ? "/ai2" : "/ai3");
            //if (isCurrentControlled) inputChannel = "/ai2";
            //else inputChannel = "/ai3";
            impedanceRecord.AIChannels.CreateVoltageChannel(Properties.Settings.Default.ImpedanceDevice + inputChannel, "",
                AITerminalConfiguration.Rse, -5.0, 5.0, AIVoltageUnits.Volts);
            //try delaying sampling
            impedanceRecord.Timing.DelayFromSampleClock = 10;
            impedanceRecord.Timing.ConfigureSampleClock("", IMPEDANCE_SAMPLING_RATE, SampleClockActiveEdge.Rising,
                SampleQuantityMode.FiniteSamples);
            impedanceReader = new AnalogSingleChannelReader(impedanceRecord.Stream);
            impedanceRecord.Timing.ReferenceClockSource = "OnboardClock";

            //Configure stim digital output task
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);

            //Configure stim analog output task
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao2", "", -10.0, 10.0, AOVoltageUnits.Volts); //Actual Pulse
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao3", "", -10.0, 10.0, AOVoltageUnits.Volts); //Timing
            }
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
            {
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -10.0, 10.0, AOVoltageUnits.Volts); //Actual pulse
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -10.0, 10.0, AOVoltageUnits.Volts); //Timing
            }
            stimAnalogTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.ImpedanceDevice + "/ai/SampleClock",
                IMPEDANCE_SAMPLING_RATE, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
            stimAnalogTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" +
                Properties.Settings.Default.ImpedanceDevice + "/ai/StartTrigger",
                DigitalEdgeStartTriggerEdge.Rising);
            impedanceRecord.Control(TaskAction.Verify);
            stimAnalogTask.Timing.ReferenceClockSource = impedanceRecord.Timing.ReferenceClockSource;
            stimAnalogTask.Timing.ReferenceClockRate = impedanceRecord.Timing.ReferenceClockRate;
            stimAnalogWriter = new AnalogMultiChannelWriter(stimAnalogTask.Stream);
            
            //Verify tasks
            impedanceRecord.Control(TaskAction.Verify);
            stimDigitalTask.Control(TaskAction.Verify);
            stimAnalogTask.Control(TaskAction.Verify);

            //Setup storage variable
            impedance = new double[numChannels][];
            for (int c = 0; c < numChannels; ++c)
            {
                impedance[c] = new double[freqs.GetLength(0)];
                for (int f = 0; f < freqs.Length; ++f)
                    impedance[c][f] = double.NaN;
            }

            //Setup background worker
            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.WorkerReportsProgress = true;

            //Run worker
            bgWorker.RunWorkerAsync();
        }

        void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double[] state = (double[])e.UserState;
            if (alertProgressChanged != null) alertProgressChanged(this, e.ProgressPercentage, (int)state[0], state[1]);
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stimAnalogTask.Dispose();
            stimDigitalTask.Dispose();
            impedanceRecord.Dispose();
            if (alertAllFinished != null) alertAllFinished(this);   
        }

        void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Measure each channel
            for (int c = 0; c < numChannels && !bgWorker.CancellationPending; ++c)
            {
                UInt32 MuxData = StimPulse.channel2MUX(Convert.ToDouble(startChannel + c));

                //Setup digital waveform, open MUX channel
                stimDigitalWriter.WriteSingleSamplePort(true, MuxData);
                stimDigitalTask.WaitUntilDone();
                stimDigitalTask.Stop();

                double numPeriodsUsed = numPeriods;
                for (int f = 0; f < freqs.GetLength(0) && !bgWorker.CancellationPending; ++f)
                {
                    //Update progress bars
                    bgWorker.ReportProgress(100 * (f + (c * freqs.Length)) / (numChannels * freqs.Length), new double[] { startChannel + c, freqs[f] });

                    //Create test wave
                    double numSeconds = 1 / freqs[f];
                    if (numSeconds * numPeriods < 0.1)
                        numPeriodsUsed = Math.Ceiling(0.1 * freqs[f]);
                    SineSignal testWave = new SineSignal(freqs[f], commandVoltage);  //Generate a 100 mV sine wave at 1000 Hz
                    double[] testWaveValues = testWave.Generate(IMPEDANCE_SAMPLING_RATE, (long)Math.Round(numSeconds * (double)IMPEDANCE_SAMPLING_RATE));

                    int size = Convert.ToInt32(numSeconds * IMPEDANCE_SAMPLING_RATE);
                    double[,] analogPulse = new double[4, size];

                    for (int i = 0; i < size; ++i)
                        analogPulse[0 + 2, i] = testWaveValues[i];

                    impedanceRecord.Timing.SamplesPerChannel = (long)(numPeriodsUsed * size);
                    stimAnalogTask.Timing.SamplesPerChannel = (long)(numPeriodsUsed * size); //Do numperiods cycles of sine wave

                    //Deliver pulse
                    stimAnalogWriter.WriteMultiSample(true, analogPulse);
                    double[] data = impedanceReader.ReadMultiSample((int)(numPeriodsUsed * size));

                    #region Calculate Impedance
                    //Remove DC offset
                    double mData = 0.0;
                    for (int i = 0; i < data.Length; ++i) mData += data[i];
                    mData /= data.Length;
                    for (int i = 0; i < data.Length; ++i) data[i] -= mData;

                    //Filter data with Butterworth, if checked
                    if (useBandpassFilter)
                    {
                        ButterworthBandpassFilter bwfilt = new ButterworthBandpassFilter(1, IMPEDANCE_SAMPLING_RATE, freqs[f] - freqs[f] / 4, freqs[f] + freqs[f] / 4);
                        data = bwfilt.FilterData(data);
                    }

                    //Use matched filter to reduce noise, if checked (slow)
                    if (useMatchedFilter)
                    {
                        SineSignal wave = new SineSignal(freqs[f], 1.0);  //Create a sine wave at test frequency of amplitude 1
                        double[] h; //filter
                        //If data is very long, subsample by an order of magnitude
                        if (data.Length > 1E6)
                        {
                            double[] dataNew = new double[(int)Math.Floor((double)data.Length / 10)];
                            for (int i = 0; i < dataNew.Length; ++i) dataNew[i] = data[i * 10];
                            data = dataNew;
                            dataNew = null;
                            h = wave.Generate(IMPEDANCE_SAMPLING_RATE / 10, (long)Math.Round((double)IMPEDANCE_SAMPLING_RATE / (freqs[f] * 10))); //Generate one period
                        }

                        else
                        {
                            h = wave.Generate(IMPEDANCE_SAMPLING_RATE, (long)Math.Round((double)IMPEDANCE_SAMPLING_RATE / freqs[f])); //Generate one period
                        }
                        wave = null;
                        //Compute filter power
                        double phh = 0.0;
                        for (int i = 0; i < h.Length; ++i) phh += h[i] * h[i];
                        //Normalize filter so power is 1
                        for (int i = 0; i < h.Length; ++i) h[i] /= phh;

                        //sw.Start();
                        double[] x = NationalInstruments.Analysis.Dsp.SignalProcessing.Convolve(data, h);
                        //sw.Stop();
                        //TimeSpan ts = sw.Elapsed;
                        //System.Diagnostics.Debug.WriteLine("ms = " + ts.Milliseconds + "\t s = " + ts.Seconds + "\t min = " + ts.Minutes);

                        int offset = (int)(h.Length * 0.5);
                        for (int i = 0; i < data.Length; ++i) data[i] = x[i + offset]; //Take center values
                    }

                    double rms = rootMeanSquared(data);

                    if (isCurrentControlled)  //Current-controlled
                    {
                        impedance[c][f] = rms / (0.707106704695506 * commandVoltage / RCurr);
                        //Account for 6.8 MOhm resistor in parallel
                        impedance[c][f] = 1.0 / (1.0 / impedance[c][f] - 1.0 / 6800000.0);
                    }
                    else  //Voltage-controlled
                    {
                        double gain = 1.0 + (49400.0 / RGain); //Based on LT in-amp
                        impedance[c][f] = (0.707106704695506 * commandVoltage) / ((rms / gain) / RMeas);
                    }
                    #endregion

                    //Wait until recording and stim are finished
                    stimAnalogTask.WaitUntilDone();
                    impedanceRecord.WaitUntilDone();
                    stimAnalogTask.Stop();
                    impedanceRecord.Stop();
                }

                //De-select channel on mux
                stimDigitalWriter.WriteSingleSamplePort(true, 0);
                stimDigitalTask.WaitUntilDone();
                stimDigitalTask.Stop();

                //Notify that channel is done
                if (alertChannelFinished != null)
                    alertChannelFinished(this, c, startChannel + c, impedance, freqs);

                
            }
            //Reset muxes
            bool[] fData = new bool[Properties.Settings.Default.StimPortBandwidth];
            stimDigitalWriter.WriteSingleSampleMultiLine(true, fData);
            stimDigitalTask.WaitUntilDone();
            stimDigitalTask.Stop();
        }

        private void analogInCallback_impedance(IAsyncResult ar)
        {
            double[] state = (double[])ar.AsyncState;
            int ch = (int)state[0];
            double f = state[1];

            double[] data = impedanceReader.EndReadMultiSample(ar);

            //Remove DC offset
            double mData = 0.0;
            for (int i = 0; i < data.Length; ++i) mData += data[i];
            mData /= data.Length;
            for (int i = 0; i < data.Length; ++i) data[i] -= mData;

            //Filter data with Butterworth, if checked
            if (useBandpassFilter)
            {
                ButterworthBandpassFilter bwfilt = new ButterworthBandpassFilter(1, IMPEDANCE_SAMPLING_RATE, f - f / 4, f + f / 4);
                data = bwfilt.FilterData(data);
            }

            //Use matched filter to reduce noise, if checked (slow)
            if (useMatchedFilter)
            {
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                SineSignal wave = new SineSignal(f, 1.0);  //Create a sine wave at test frequency of amplitude 1
                double[] h; //filter
                //If data is very long, subsample by an order of magnitude
                if (data.Length > 1E6)
                {
                    double[] dataNew = new double[(int)Math.Floor((double)data.Length / 10)];
                    for (int i = 0; i < dataNew.Length; ++i) dataNew[i] = data[i * 10];
                    data = dataNew;
                    dataNew = null;
                    h = wave.Generate(IMPEDANCE_SAMPLING_RATE / 10, (long)Math.Round((double)IMPEDANCE_SAMPLING_RATE / (f * 10))); //Generate one period
                }

                else
                {
                    h = wave.Generate(IMPEDANCE_SAMPLING_RATE, (long)Math.Round((double)IMPEDANCE_SAMPLING_RATE / f)); //Generate one period
                }
                wave = null;
                //GC.Collect(); //this uses a lot of memory
                //Compute filter power
                double phh = 0.0;
                for (int i = 0; i < h.Length; ++i) phh += h[i] * h[i];
                //Normalize filter so power is 1
                for (int i = 0; i < h.Length; ++i) h[i] /= phh;

                //sw.Start();
                double[] x = NationalInstruments.Analysis.Dsp.SignalProcessing.Convolve(data, h);
                //sw.Stop();
                //TimeSpan ts = sw.Elapsed;
                //System.Diagnostics.Debug.WriteLine("ms = " + ts.Milliseconds + "\t s = " + ts.Seconds + "\t min = " + ts.Minutes);

                int offset = (int)(h.Length * 0.5);
                for (int i = 0; i < data.Length; ++i) data[i] = x[i + offset]; //Take center values
            }

            double rms = rootMeanSquared(data);

            if (Convert.ToBoolean(state[3]))  //Current-controlled
            {
                impedance[ch][(int)state[2]] = rms / (0.707106704695506 * commandVoltage / RCurr);
                //Account for 6.8 MOhm resistor in parallel
                impedance[ch][(int)state[2]] = 1.0 / (1.0 / impedance[ch][(int)state[2]] - 1.0 / 6800000.0);
            }
            else  //Voltage-controlled
            {
                double gain = 1.0 + (49400.0 / RGain); //Based on LT in-amp
                impedance[ch][(int)state[2]] = (0.707106704695506 * commandVoltage) / ((rms / gain) / RMeas);
            }
        }

        internal void cancel()
        {
            if (bgWorker != null && bgWorker.IsBusy)
                bgWorker.CancelAsync();
        }

        internal void saveAsMAT()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "MAT files (*.mat)|*.mat|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "mat";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filename = saveFileDialog.FileName;

                List<MLArray> mlList = new List<MLArray>();
                MLStructure structure = new MLStructure("imp", new int[] { 1, 1 });
                structure["f", 0] = new MLDouble("", freqs, freqs.Length);

                //Only add non-null (sampled) channels
                int numNonNull = 0;
                List<int> goodChannels = new List<int>();
                for (int i = 0; i < impedance.Length; ++i)
                    if (impedance[i] != null)
                    {
                        ++numNonNull;
                        goodChannels.Add(i);
                    }
                double[][] nonNullImpedance = new double[numNonNull][];
                for (int i = 0; i < numNonNull; ++i) nonNullImpedance[i] = impedance[goodChannels[i]];

                structure["z", 0] = new MLDouble("", nonNullImpedance);
                mlList.Add(structure);

                try
                {
                    MatFileWriter mfw = new MatFileWriter(filename, mlList, true);
                }
                catch (Exception err)
                {
                    MessageBox.Show("There was an error when creating the MAT-file: \n" + err.ToString(),
                        "MAT-File Creation Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        //Compute the RMS of an array.  Use this rather than a stock method, since it has no error checking and is faster.  Error checking is for pansies!
        private static double rootMeanSquared(double[] data)
        {
            double rms = 0.0;
            for (int i = 0; i < data.Length; ++i)
                rms += data[i] * data[i];
            rms /= data.Length;
            return Math.Sqrt(rms);
        }
    }
}
