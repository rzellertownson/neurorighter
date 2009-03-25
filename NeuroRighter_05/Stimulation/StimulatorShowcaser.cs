using System;
using System.Collections.Generic;
using System.Text;
using NationalInstruments.DAQmx;
using NationalInstruments;
using NationalInstruments.Analysis;
using System.IO;

namespace NeuroRighter.Stimulation
{
    internal sealed class StimulatorShowcaser
    {
        private Task stimDigitalTask, stimAnalogTask, recordingTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;
        private AnalogSingleChannelReader singleChanReader;
        private AnalogMultiChannelReader multiChanReader;
        
        internal StimulatorShowcaser(Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
        {
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogWriter = stimAnalogWriter;
            this.stimDigitalWriter = stimDigitalWriter;

        }

        /// <summary>
        /// This function sends out a few sample stimulus waveforms, and reads them to a single
        /// analog input channel (ai2 of Cineplex device).
        /// </summary>
        internal void makeSampleWaveforms()
        {
            try
            {
                #region Output previously recorded LFP
                //Load sample LFP data
                BinaryReader br = new BinaryReader(File.Open("D:\\Data\\DEFEPI3\\090120a.lfp", FileMode.Open));
                br.BaseStream.Seek(54, SeekOrigin.Begin); //Seek past header
                int lfpDataLength = 10000;
                double[] lfpData = new double[lfpDataLength];
                double gain = 1000.0 * (5.0 / Int16.MaxValue) / 160.0;
                for (int i = 0; i < lfpDataLength; ++i)
                {
                    lfpData[i] = gain * (double)br.ReadInt16();
                    br.BaseStream.Seek(15 * 2, SeekOrigin.Current); //go past remaining channels
                }
                br.Close();

                //Change stim writer to self-triggering
                stimAnalogTask.Dispose();
                stimDigitalTask.Dispose();
                stimDigitalWriter = null;
                stimAnalogWriter = null;
                stimAnalogTask = new Task("Showcase Stim Output Task");
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao2", "", -5.0, 5.0, AOVoltageUnits.Volts);
                stimAnalogTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.CineplexDevice + "/ai/SampleClock", 2000.0, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimAnalogTask.Timing.SamplesPerChannel = lfpDataLength;
                stimAnalogTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" + Properties.Settings.Default.CineplexDevice + "/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                AnalogSingleChannelWriter writer = new AnalogSingleChannelWriter(stimAnalogTask.Stream);

                //Make reader
                recordingTask = new Task("Showcase Recording Task");
                //Read Stim output
                recordingTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.CineplexDevice + "/ai2", "",
                    AITerminalConfiguration.Nrse, -5.0, 5.0, AIVoltageUnits.Volts);
                recordingTask.Timing.ConfigureSampleClock("", 2000.0, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                recordingTask.Timing.SamplesPerChannel = lfpDataLength;
                recordingTask.Control(TaskAction.Verify);
                stimAnalogTask.Control(TaskAction.Verify);

                //Create reader
                singleChanReader = new AnalogSingleChannelReader(recordingTask.Stream);

                //Write analog pulse
                writer.WriteMultiSample(true, lfpData);

                //Start reading
                AnalogWaveform<Double> aw = singleChanReader.ReadWaveform(lfpDataLength);

                //Open output file
                Stream outStream = new FileStream("SampleLFPWaveform.raw", FileMode.Create, FileAccess.Write, FileShare.None, 1024, false);
                DateTime dt = DateTime.Now; //Get current time (local to computer)

                //Write header info: #chs, sampling rate, gain, date/time
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(1)), 0, 2); //Int: Num channels
                outStream.Write(BitConverter.GetBytes(Convert.ToInt32(recordingTask.Timing.SampleClockRate)), 0, 4); //Int: Sampling rate
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / recordingTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8); //Double: Scaling coefficients
                outStream.Write(BitConverter.GetBytes(recordingTask.AIChannels.All.RangeHigh / Int16.MaxValue), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

                //Write data
                double oneOverResolution = Int16.MaxValue / recordingTask.AIChannels.All.RangeHigh;
                for (int i = 0; i < aw.Samples.Count; ++i)
                {
                    double tempVal = Math.Round(aw.Samples[i].Value * oneOverResolution);
                    if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                    else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                    else { tempVal = Int16.MinValue; }
                    outStream.Write(BitConverter.GetBytes((short)tempVal), 0, 2);
                }

                outStream.Close();

                //Wait for stim to finish
                stimAnalogTask.WaitUntilDone();
                recordingTask.WaitUntilDone();
                stimAnalogTask.Stop();
                recordingTask.Stop();
                #endregion

                #region Square Pulses
                //Cteate biphasic stim pulse
                StimPulse sp = new StimPulse(400, 400, 1, -1, 4, 0, 0, 100, 100, true);
                //Reconfigure timing of analog output task
                stimAnalogTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.CineplexDevice + "/ai/SampleClock", StimPulse.STIM_SAMPLING_FREQ, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimAnalogTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" + Properties.Settings.Default.CineplexDevice + "/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                stimAnalogTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1);
                //Reconfigure timing of analog input task
                recordingTask.Timing.ConfigureSampleClock("", StimPulse.STIM_SAMPLING_FREQ, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                recordingTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1);

                //Verify tasks (generate errors)
                recordingTask.Control(TaskAction.Verify);
                stimAnalogTask.Control(TaskAction.Verify);

                //Write analog pulse
                double[] biphasicPulse = new double[sp.analogPulse.GetLength(1)];
                for (int i = 0; i < sp.analogPulse.GetLength(1); ++i)
                    biphasicPulse[i] = sp.analogPulse[2,i];
                writer.WriteMultiSample(true, biphasicPulse);

                //Start reading
                aw = singleChanReader.ReadWaveform(sp.analogPulse.GetLength(1));

                //Open output file
                outStream = new FileStream("SampleBiphasicWaveform.raw", FileMode.Create, FileAccess.Write, FileShare.None, 1024, false);
                dt = DateTime.Now; //Get current time (local to computer)

                //Write header info: #chs, sampling rate, gain, date/time
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(1)), 0, 2); //Int: Num channels
                outStream.Write(BitConverter.GetBytes(Convert.ToInt32(recordingTask.Timing.SampleClockRate)), 0, 4); //Int: Sampling rate
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / recordingTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8); //Double: Scaling coefficients
                outStream.Write(BitConverter.GetBytes(recordingTask.AIChannels.All.RangeHigh / Int16.MaxValue), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

                //Write data
                for (int i = 0; i < aw.Samples.Count; ++i)
                {
                    double tempVal = Math.Round(aw.Samples[i].Value * oneOverResolution);
                    if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                    else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                    else { tempVal = Int16.MinValue; }
                    outStream.Write(BitConverter.GetBytes((short)tempVal), 0, 2);
                }
                outStream.Close();

                //Wait for stim to finish
                stimAnalogTask.WaitUntilDone();
                recordingTask.WaitUntilDone();
                stimAnalogTask.Stop();
                recordingTask.Stop();
                #endregion

                #region Generate sine wave
                //Set up sine wave in "sineWave" array
                double sineWaveFrequency = 100.0;
                double sineWaveAmplitude = 1.0;
                int numPeriods = 5;
                NationalInstruments.Analysis.SignalGeneration.SineSignal sineGenerator = 
                    new NationalInstruments.Analysis.SignalGeneration.SineSignal(sineWaveFrequency, sineWaveAmplitude);
                double[] sineWave = sineGenerator.Generate( StimPulse.STIM_SAMPLING_FREQ, (long)(numPeriods * StimPulse.STIM_SAMPLING_FREQ / sineGenerator.Frequency));

                //Reconfigure timing of analog output task
                stimAnalogTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.CineplexDevice + "/ai/SampleClock", StimPulse.STIM_SAMPLING_FREQ, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimAnalogTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" + Properties.Settings.Default.CineplexDevice + "/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                stimAnalogTask.Timing.SamplesPerChannel = sineWave.Length;
                //Reconfigure timing of analog input task
                recordingTask.Timing.SamplesPerChannel = sineWave.Length;

                //Verify tasks (generate errors)
                recordingTask.Control(TaskAction.Verify);
                stimAnalogTask.Control(TaskAction.Verify);

                //Write analog pulse
                writer.WriteMultiSample(true, sineWave);

                //Start reading
                aw = singleChanReader.ReadWaveform(sineWave.Length);

                //Open output file
                outStream = new FileStream("SampleSineWaveform.raw", FileMode.Create, FileAccess.Write, FileShare.None, 1024, false);
                dt = DateTime.Now; //Get current time (local to computer)

                //Write header info: #chs, sampling rate, gain, date/time
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(1)), 0, 2); //Int: Num channels
                outStream.Write(BitConverter.GetBytes(Convert.ToInt32(recordingTask.Timing.SampleClockRate)), 0, 4); //Int: Sampling rate
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / recordingTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8); //Double: Scaling coefficients
                outStream.Write(BitConverter.GetBytes(recordingTask.AIChannels.All.RangeHigh / Int16.MaxValue), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

                //Write data
                for (int i = 0; i < aw.Samples.Count; ++i)
                {
                    double tempVal = Math.Round(aw.Samples[i].Value * oneOverResolution);
                    if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                    else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                    else { tempVal = Int16.MinValue; }
                    outStream.Write(BitConverter.GetBytes((short)tempVal), 0, 2);
                }
                outStream.Close();

                //Wait for stim to finish
                stimAnalogTask.WaitUntilDone();
                recordingTask.WaitUntilDone();
                stimAnalogTask.Stop();
                recordingTask.Stop();
                #endregion

            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
            finally
            {
                if (stimAnalogTask != null)
                    stimAnalogTask.Dispose();
                if (recordingTask != null)
                    recordingTask.Dispose();
            }
        }

        /// <summary>
        /// This function sends outs a current and voltage controlled pulse, recording
        /// the corresponding voltage and current, respectively.  The recorded V and I
        /// are on Cineplex Device, ai2 and ai3 (V,I, respectively).  V-controlled pulse
        /// is delivered first.
        /// </summary>
        /// <param name="currentControlled">True is experiment is current-controlled, false if voltage-controlled</param>
        internal void makeDualVIWaveforms(bool currentControlled)
        {
            const double channel = 1;

            try
            {
                //Refresh stim digital task
                stimDigitalTask.Dispose();
                stimDigitalTask = new Task("stimDigitalTask_Showcaser");
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
                stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);
                stimDigitalTask.Control(TaskAction.Verify);
                UInt32 digData = StimPulse.channel2MUX(channel);

                //Setup digital waveform, open MUX channel
                stimDigitalWriter.WriteSingleSamplePort(true, digData);
                stimDigitalTask.WaitUntilDone();
                stimDigitalTask.Stop();


                #region Output Pulse
                //Make pulse
                double amp;
                if (currentControlled)
                    amp = 1;
                else
                    amp = 0.5;
                StimPulse sp = new StimPulse(400, 400, amp, -amp, 0, 0.0, 0, 100, 100, true);

                //Change stim writer to self-triggering
                stimAnalogTask.Dispose();
                stimAnalogWriter = null;
                stimAnalogTask = new Task("Showcase Stim Output Task");
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -5.0, 5.0, AOVoltageUnits.Volts);
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -5.0, 5.0, AOVoltageUnits.Volts);
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao2", "", -5.0, 5.0, AOVoltageUnits.Volts);
                stimAnalogTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao3", "", -5.0, 5.0, AOVoltageUnits.Volts);
                //stimAnalogTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.CineplexDevice + "/ai/SampleClock", StimPulse.STIM_SAMPLING_FREQ, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimAnalogTask.Timing.ConfigureSampleClock("", StimPulse.STIM_SAMPLING_FREQ, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimAnalogTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" + Properties.Settings.Default.CineplexDevice + "/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                stimAnalogWriter = new AnalogMultiChannelWriter(stimAnalogTask.Stream);
                stimAnalogTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1);

                //Make reader
                recordingTask = new Task("Showcase Recording Task");
                recordingTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.CineplexDevice + "/ai2", "",
                    AITerminalConfiguration.Nrse, -5.0, 5.0, AIVoltageUnits.Volts); //For voltage waveform
                recordingTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.CineplexDevice + "/ai3", "",
                    AITerminalConfiguration.Nrse, -5.0, 5.0, AIVoltageUnits.Volts); //For current waveform
                recordingTask.Timing.ConfigureSampleClock("", 500000, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                recordingTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1)*5;
                recordingTask.Control(TaskAction.Verify);
                stimAnalogTask.Control(TaskAction.Verify);

                //Create reader
                multiChanReader = new AnalogMultiChannelReader(recordingTask.Stream);

                //Write analog pulse
                stimAnalogWriter.WriteMultiSample(true, sp.analogPulse);

                //Start reading
                double[,] aw = multiChanReader.ReadMultiSample(sp.analogPulse.GetLength(1)*5);
                
                //Open output file
                String filename = (currentControlled ? "DualIV_I_Controlled" : "DualIV_V_Controlled");
                Stream outStream = new FileStream(filename + ".raw", FileMode.Create, FileAccess.Write, FileShare.None, 1024, false);
                DateTime dt = DateTime.Now; //Get current time (local to computer)

                //Write header info: #chs, sampling rate, gain, date/time
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(2)), 0, 2); //Int: Num channels
                outStream.Write(BitConverter.GetBytes(Convert.ToInt32(recordingTask.Timing.SampleClockRate)), 0, 4); //Int: Sampling rate
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / recordingTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8); //Double: Scaling coefficients
                outStream.Write(BitConverter.GetBytes(recordingTask.AIChannels.All.RangeHigh / Int16.MaxValue), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

                //Write data
                double oneOverResolution = Int16.MaxValue / recordingTask.AIChannels.All.RangeHigh;
                for (int i = 0; i < aw.GetLength(1); ++i)
                {
                    //Channel 0
                    double tempVal = Math.Round(aw[0, i] * oneOverResolution);
                    if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                    else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                    else { tempVal = Int16.MinValue; }
                    outStream.Write(BitConverter.GetBytes((short)tempVal), 0, 2);

                    //Channel 1
                    tempVal = Math.Round(aw[1, i] * oneOverResolution);
                    if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                    else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                    else { tempVal = Int16.MinValue; }
                    outStream.Write(BitConverter.GetBytes((short)tempVal), 0, 2);
                }

                outStream.Close();

                //Wait for stim to finish
                stimAnalogTask.WaitUntilDone();
                recordingTask.WaitUntilDone();
                stimAnalogTask.Stop();
                recordingTask.Stop();
                #endregion


            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
            finally
            {
                if (stimAnalogTask != null)
                    stimAnalogTask.Dispose();
                if (recordingTask != null)
                    recordingTask.Dispose();

                //Close MUX
                if (stimDigitalTask != null)
                {
                    bool[] fData = new bool[Properties.Settings.Default.StimPortBandwidth];
                    stimDigitalWriter.WriteSingleSampleMultiLine(true, fData);
                    stimDigitalTask.WaitUntilDone();
                    stimDigitalTask.Stop();
                    stimDigitalTask.Dispose();
                }
            }
        }

        private void singleChannelCallback(IAsyncResult ar)
        {

        }
    }
}
