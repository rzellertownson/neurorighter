// CLNUP.CS
// Copyright (c) 2008-2011 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

//#define USE_LOG_FILE
//#define DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.Analysis;
using NationalInstruments.Analysis.Dsp;
using NationalInstruments.Analysis.Dsp.Filters;
using NationalInstruments.Analysis.Math;
using NationalInstruments.Analysis.SignalGeneration;
using csmatio.types;
using csmatio.io;
using rawType = System.Double;


namespace NeuroRighter
{

    ///<summary>Methods for cleaning up NI Tasks and UI when stop button is pressed.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        //Called after data acq. is complete, resets buttons and stops tasks.
        private void reset()
        {
            // Start by resetting the hardware settings
            updateRecSettings();

            //Grab display gains for later use
            if (spikePlotData != null)
                if (spikePlotData.getGain() != null)
                    Properties.Settings.Default.SpikeDisplayGain = spikePlotData.getGain();
            if (Properties.Settings.Default.UseLFPs & (lfpPlotData!=null))
                Properties.Settings.Default.LFPDisplayGain = lfpPlotData.getGain();
            if (waveformPlotData != null)
                if (waveformPlotData.getGain() != null)
                    Properties.Settings.Default.SpkWfmDisplayGain = waveformPlotData.getGain();
            Console.WriteLine("reset: gains saved");
            taskRunning = false;
            if (triggerWriter != null)
            {
                byte[] b_array = new byte[3] { 0, 0, 0 };
                DigitalWaveform wfm = new DigitalWaveform(3, 8, DigitalState.ForceDown);
                wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                triggerTask = new Task("TriggerTask");
                triggerTask.DOChannels.CreateChannel(Properties.Settings.Default.CineplexDevice + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines);
                triggerWriter = new DigitalSingleChannelWriter(triggerTask.Stream);
                triggerWriter.WriteWaveform(true, wfm);
                triggerTask.WaitUntilDone();
            }
            Console.WriteLine("reset: trigger cleared");
            // Kill the background workers
            lock (this)
            {
                Console.WriteLine("reset: entered lock");
                if (bwSpikes != null)
                {
                    try
                    {
                        for (int i = 0; i < bwSpikes.Count; ++i)
                            //block while bw finishes
                            if (bwSpikes[i] != null)
                            {
                                Console.WriteLine("reset: " + bwSpikes[i].ToString() + " " +i.ToString() + "is busy");
                                while (bwSpikes[i].IsBusy)
                                {
                                    Application.DoEvents();
                                }
                                Console.WriteLine("reset: " + bwSpikes[i].ToString() + " " + i.ToString() + "finished");
                            }

                    }
                    catch
                    {
                        Console.WriteLine("reset: error while clearing spike tasks");
                        //All the bw workers are done, so we'll kill them
                        for (int i = 0; i < bwSpikes.Count; ++i)
                            bwSpikes[i].Dispose();
                        bwSpikes.Clear();
                        bwSpikes = null;
                    }
                }
                Console.WriteLine("reset: left lock");
            }
            Console.WriteLine("reset: spike tasks cleared");

            if (waveformPlotData != null) waveformPlotData.stop();
            if (Properties.Settings.Default.SeparateLFPBoard && lfpTask != null) lfpTask.Dispose();
            if (Properties.Settings.Default.UseEEG && eegTask != null) eegTask.Dispose();
            if (BNCOutput != null) { BNCOutput.Dispose(); BNCOutput = null; }
            if (stimTimeTask != null) stimTimeTask.Dispose();
            if (triggerTask != null) triggerTask.Dispose();
            if (auxAnInTask != null) auxAnInTask.Dispose();
            if (auxDigInTask != null) auxDigInTask.Dispose();
            Console.WriteLine("reset: tasks disposed of");
            buttonStop.Enabled = false;
            buttonStart.Enabled = true;
            comboBox_numChannels.Enabled = true;
            comboBox_SpikeGain.Enabled = true;
            spikeDet.numPreSamples.Enabled = true;
            spikeDet.numPostSamples.Enabled = true;
            settingsToolStripMenuItem.Enabled = true;
            comboBox_SpikeGain.Enabled = true;
            button_Train.Enabled = true;
            button_SetRecordingStreams.Enabled = true;
            switch_record.Enabled = true;
            //processingSettingsToolStripMenuItem.Enabled = true;
            textBox_spikeSamplingRate.Enabled = true;
            textBox_lfpSamplingRate.Enabled = true;
            textBox_MUASamplingRate.Enabled = true;
            button_startStimFromFile.Enabled = true;
            numericUpDown_NumSnipsDisplayed.Enabled = true;
            button_stopClosedLoopStim.Enabled = false;
            button_startClosedLoopStim.Enabled = true;
            
            if (Properties.Settings.Default.UseEEG)
            {
                comboBox_eegNumChannels.Enabled = true;
                comboBox_eegGain.Enabled = true;
                textBox_eegSamplingRate.Enabled = true;
            }
            if (Properties.Settings.Default.SeparateLFPBoard)
                comboBox_LFPGain.Enabled = true;
            Console.WriteLine("reset: gui updated");
            // Clean up data streams
            recordingSettings.Flush();
            Console.WriteLine("reset: recording streams flushed");
            if (triggerWriter != null) triggerWriter = null;
            channelOut.Enabled = Properties.Settings.Default.UseSingleChannelPlayback;

            led_recording.OnColor = Color.Lime;
            if (!button_startStimFromFile.Enabled) { button_startStimFromFile.Enabled = true; }


            //debugger
            if (Debugger != null)
            {
                Debugger.Close();
                Debugger = null;
            }

            timer_timeElapsed.Enabled = false;
            Console.WriteLine("Reset Complete");
        }

        // Called when stimulation is stopped
        private void resetStim()
        {
            //Zero out IvsV and dispose
            stimIvsVTask = new Task("stimIvsV");
            stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                ChannelLineGrouping.OneChannelForAllLines);
            stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
            stimIvsVTask.Control(TaskAction.Verify);
            stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
            stimIvsVTask.WaitUntilDone();
            stimIvsVTask.Stop();
            stimIvsVTask.Dispose();

            // Sero out stim digital output and dispose
            if (stimDigitalTask != null)
                stimDigitalTask.Dispose();
            stimDigitalTask = new Task("stimDigitalTask_formClosing");
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);
            bool[] fData = new bool[Properties.Settings.Default.StimPortBandwidth];
            stimDigitalWriter.WriteSingleSampleMultiLine(true, fData);
            stimDigitalTask.WaitUntilDone();
            stimDigitalTask.Stop();
            Console.WriteLine("resetStim completed");
        }

        // Look at the recording hardware settings and create NI Tasks that reflect the user's choices
        private void updateRecSettings()
        {

            // update the recordingSettings object
            recordingSettings.Refresh();

            // update recording type to nominal
            isNormalRecording = true;

            // Refresh all the NI Tasks
            try
            {
                if (spikeTask != null)
                {
                    for (int i = 0; i < spikeTask.Count; ++i)
                        spikeTask[i].Dispose();
                    spikeTask.Clear(); spikeTask = null;
                }
                if (stimTimeTask != null) { stimTimeTask.Dispose(); stimTimeTask = null; }
                if (stimIvsVTask != null) { stimIvsVTask.Dispose(); stimIvsVTask = null; }
                if (serialOut != null) { serialOut.Close(); serialOut.Dispose(); }
                if (Properties.Settings.Default.UseCineplex)
                {
                    if (videoTask == null)
                    {
                        videoTask = new Task("videoTask");
                        videoTask.COChannels.CreatePulseChannelFrequency(Properties.Settings.Default.CineplexDevice + "/ctr0", "",
                            COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, 1000, 0.5);
                        videoTask.Control(TaskAction.Verify);
                        videoTask.Timing.ReferenceClockSource = "OnboardClock";
                        videoTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples, 10);
                        videoTask.Start();
                    }
                    checkBox_video.Enabled = true;
                }
                else
                    checkBox_video.Enabled = false;

                comboBox_LFPGain.Enabled = Properties.Settings.Default.SeparateLFPBoard;

                if (Properties.Settings.Default.UseProgRef)
                {
                    serialOut = new SerialPort(Properties.Settings.Default.SerialPortDevice, 38400, Parity.None, 8, StopBits.One);
                    serialOut.Open();
                    serialOut.Write("#0140/0\r"); //Reset everything to power-up state
                    groupBox_plexonProgRef.Visible = true;
                }
                else { groupBox_plexonProgRef.Visible = false; }
                this.drawOpenLoopStimPulse();

                //Add LFP tab, if applicable
                if (Properties.Settings.Default.UseLFPs && !tabControl.TabPages.Contains(tabPage_LFPs))
                {
                    tabPage_LFPs = new TabPage("LFPs");
                    tabControl.TabPages.Insert(2, tabPage_LFPs);
                }
                else if (!Properties.Settings.Default.UseLFPs && tabControl.TabPages.Contains(tabPage_LFPs)) tabControl.TabPages.Remove(tabPage_LFPs);

                //Add MUA tab, if applicable
                if (Properties.Settings.Default.ProcessMUA && !tabControl.TabPages.Contains(tabPage_MUA))
                {
                    tabPage_MUA = new TabPage("MUA");
                    tabControl.TabPages.Insert((Properties.Settings.Default.UseLFPs ? 3 : 2), tabPage_MUA);
                }
                else if (!Properties.Settings.Default.ProcessMUA && tabControl.TabPages.Contains(tabPage_MUA)) 
                    tabControl.TabPages.Remove(tabPage_MUA);

                // Save sampling rates
                Properties.Settings.Default.RawSampleFrequency = Convert.ToDouble(textBox_spikeSamplingRate.Text);
                Properties.Settings.Default.LFPSampleFrequency = Convert.ToDouble(textBox_lfpSamplingRate.Text);
                Properties.Settings.Default.MUASampleFrequency = Convert.ToDouble(textBox_MUASamplingRate.Text);
                Console.WriteLine("updateRecSettings finished");
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message); //Display Errors
                reset();
            }
        }

        // Look at the stimulation hardware settings and create NI Tasks that reflect the user's choices
        private void updateStimSettings()
        {
            try
            {
                if (stimPulseTask != null) { stimPulseTask.Dispose(); stimPulseTask = null; }
                if (stimDigitalTask != null) { stimDigitalTask.Dispose(); stimDigitalTask = null; }
                if (Properties.Settings.Default.UseStimulator)
                {
                    if (stimDigitalTask == null)
                    {
                        stimDigitalTask = new Task("stimDigitalTask");
                        stimPulseTask = new Task("stimPulseTask");
                        if (Properties.Settings.Default.StimPortBandwidth == 32)
                            stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                                ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
                        else if (Properties.Settings.Default.StimPortBandwidth == 8)
                            stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                                ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
                        if (Properties.Settings.Default.StimPortBandwidth == 32)
                        {
                            stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                            stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                            stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao2", "", -10.0, 10.0, AOVoltageUnits.Volts); //Actual Pulse
                            stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao3", "", -10.0, 10.0, AOVoltageUnits.Volts); //Timing
                        }
                        else if (Properties.Settings.Default.StimPortBandwidth == 8)
                        {
                            stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -10.0, 10.0, AOVoltageUnits.Volts);
                            stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -10.0, 10.0, AOVoltageUnits.Volts);
                        }


                        if (Properties.Settings.Default.UseCineplex)
                        {
                            stimPulseTask.Timing.ReferenceClockSource = videoTask.Timing.ReferenceClockSource;
                            stimPulseTask.Timing.ReferenceClockRate = videoTask.Timing.ReferenceClockRate;
                        }
                        else
                        {
                            string tmp1 = Properties.Settings.Default.StimulatorDevice.ToString();
                            string tmp2 = Properties.Settings.Default.AnalogInDevice[0].ToString();
                            if (tmp1.Equals(tmp2))
                            {
                                stimPulseTask.Timing.ReferenceClockSource = "OnboardClock";
                            }
                            else
                            {
                                stimPulseTask.Timing.ReferenceClockSource = "/" + Properties.Settings.Default.StimulatorDevice.ToString() + "/PFI0";
                                stimPulseTask.Timing.ReferenceClockRate = 10000000.0; //10 MHz timebase
                            }
                        }

                        stimDigitalTask.Timing.ConfigureSampleClock("100KHzTimebase", STIM_SAMPLING_FREQ,
                           SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                        stimPulseTask.Timing.ConfigureSampleClock("100KHzTimebase", STIM_SAMPLING_FREQ,
                            SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                        stimDigitalTask.SynchronizeCallbacks = false;
                        stimPulseTask.SynchronizeCallbacks = false;

                        stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);
                        stimPulseWriter = new AnalogMultiChannelWriter(stimPulseTask.Stream);

                        stimPulseTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                            "/" + Properties.Settings.Default.StimulatorDevice + "/PFI6", DigitalEdgeStartTriggerEdge.Rising);

                        stimDigitalTask.Control(TaskAction.Verify);
                        stimPulseTask.Control(TaskAction.Verify);

                        //Check to ensure one of the I/V buttons is checked
                        if (!radioButton_impCurrent.Checked && !radioButton_impVoltage.Checked)
                        {
                            radioButton_impCurrent.Checked = true;
                            radioButton_impVoltage.Checked = false;
                            radioButton_stimCurrentControlled.Checked = true;
                            radioButton_stimVoltageControlled.Checked = false;
                        }

                        if (Properties.Settings.Default.UseStimulator)
                        {
                            stimIvsVTask = new Task("stimIvsV");
                            stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                                ChannelLineGrouping.OneChannelForAllLines);
                            stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                            stimIvsVTask.Control(TaskAction.Verify);
                            if (radioButton_impCurrent.Checked) stimIvsVWriter.WriteSingleSampleSingleLine(true, true);
                            else stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
                            stimIvsVTask.WaitUntilDone();
                            //stimIvsVTask.Stop();
                            stimIvsVTask.Dispose();
                        }
                    }

                    button_stim.Enabled = true;
                    button_stimExpt.Enabled = true;
                    openLoopStart.Enabled = true;
                    radioButton_impCurrent.Enabled = true;
                    radioButton_impVoltage.Enabled = true;
                    radioButton_stimCurrentControlled.Enabled = true;
                    radioButton_stimVoltageControlled.Enabled = true;
                    button_impedanceTest.Enabled = true;
                }
                else
                {
                    button_stim.Enabled = false;
                    button_stimExpt.Enabled = false;
                    openLoopStart.Enabled = false;
                    radioButton_impCurrent.Enabled = false;
                    radioButton_impVoltage.Enabled = false;
                    radioButton_stimCurrentControlled.Enabled = false;
                    radioButton_stimVoltageControlled.Enabled = false;
                    button_impedanceTest.Enabled = false;
                }
                Console.WriteLine("updateStimSettings completed");
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message); //Display Errors
                reset();
            }
        }

        // Update all Settings
        private void updateSettings()
        {
            updateStimSettings();
            updateRecSettings();
        }

        //call this method after changing stimulation settings, or finishing a stimulation experiment
        //includes code to set dc offsets back to zero
        private void updateStim()
        {
            lock (this)
            {
                bool placedzeros = false;

                if (stimPulseTask != null || stimDigitalTask != null)
                {
                    try
                    {
                        // If we were ruuning a closed loop or open-loop protocol, this will zero the outputs
                        double[,] AnalogBuffer = new double[stimPulseTask.AOChannels.Count, STIMBUFFSIZE]; // buffer for analog channels
                        UInt32[] DigitalBuffer = new UInt32[STIMBUFFSIZE];

                        stimPulseTask.Stop();
                        stimDigitalTask.Stop();

                        stimPulseWriter.WriteMultiSample(true, AnalogBuffer);
                        stimDigitalWriter.WriteMultiSamplePort(true, DigitalBuffer);

                        stimPulseTask.WaitUntilDone(20);
                        stimDigitalTask.WaitUntilDone(20);

                        stimPulseTask.Stop();
                        stimDigitalTask.Stop();
                        placedzeros = true;
                    }
                    catch (Exception ex)
                    {
                        placedzeros = false;
                    }
                }
                if (stimDigitalTask != null)
                {
                    stimDigitalTask.Dispose();
                    stimDigitalTask = null;
                }
                if (stimPulseTask != null)
                {
                    stimPulseTask.Dispose();
                    stimPulseTask = null;
                }

                if (Properties.Settings.Default.UseStimulator)
                {
                    stimPulseTask = new Task("stimPulseTask");
                    stimDigitalTask = new Task("stimDigitalTask");
                    if (Properties.Settings.Default.StimPortBandwidth == 32)
                        stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                            ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
                    else if (Properties.Settings.Default.StimPortBandwidth == 8)
                        stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                            ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
                    if (Properties.Settings.Default.StimPortBandwidth == 32)
                    {
                        stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                        stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                        stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao2", "", -10.0, 10.0, AOVoltageUnits.Volts); //Actual Pulse
                        stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao3", "", -10.0, 10.0, AOVoltageUnits.Volts); //Timing
                    }
                    else if (Properties.Settings.Default.StimPortBandwidth == 8)
                    {
                        stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao0", "", -10.0, 10.0, AOVoltageUnits.Volts);
                        stimPulseTask.AOChannels.CreateVoltageChannel(Properties.Settings.Default.StimulatorDevice + "/ao1", "", -10.0, 10.0, AOVoltageUnits.Volts);
                    }

                    if (Properties.Settings.Default.UseCineplex)
                    {
                        stimPulseTask.Timing.ReferenceClockSource = videoTask.Timing.ReferenceClockSource;
                        stimPulseTask.Timing.ReferenceClockRate = videoTask.Timing.ReferenceClockRate;
                    }
                    else
                    {
                        stimPulseTask.Timing.ReferenceClockSource = "OnboardClock";
                        //stimPulseTask.Timing.ReferenceClockRate = 10000000.0; //10 MHz timebase
                    }
                    stimDigitalTask.Timing.ConfigureSampleClock("100kHzTimebase", STIM_SAMPLING_FREQ,
                       SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                    stimPulseTask.Timing.ConfigureSampleClock("100kHzTimebase", STIM_SAMPLING_FREQ,
                        SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                    stimDigitalTask.SynchronizeCallbacks = false;
                    stimPulseTask.SynchronizeCallbacks = false;

                    stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);
                    stimPulseWriter = new AnalogMultiChannelWriter(stimPulseTask.Stream);

                    stimPulseTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                        "/" + Properties.Settings.Default.StimulatorDevice + "/PFI6", DigitalEdgeStartTriggerEdge.Rising);

                    stimDigitalTask.Control(TaskAction.Verify);
                    stimPulseTask.Control(TaskAction.Verify);

                    //Check to ensure one of the I/V buttons is checked
                    if (!radioButton_impCurrent.Checked && !radioButton_impVoltage.Checked)
                    {
                        radioButton_impCurrent.Checked = true;
                        radioButton_impVoltage.Checked = false;
                        radioButton_stimCurrentControlled.Checked = true;
                        radioButton_stimVoltageControlled.Checked = false;
                    }

                    if (Properties.Settings.Default.UseStimulator)
                    {
                        stimIvsVTask = new Task("stimIvsV");
 
                        stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                            ChannelLineGrouping.OneChannelForAllLines);
                        stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                        //stimIvsVTask.Timing.ConfigureSampleClock("100kHztimebase", 100000,
                        //    SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                        stimIvsVTask.Control(TaskAction.Verify);
                        //byte[] b_array;
                        //if (radioButton_impCurrent.Checked)
                        //    b_array = new byte[5] { 255, 255, 255, 255, 255 };
                        //else 
                        //    b_array = new byte[5] { 0, 0, 0, 0, 0 };
                        //DigitalWaveform wfm = new DigitalWaveform(5, 8, DigitalState.ForceDown);
                        //wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                        //stimIvsVWriter.WriteWaveform(true, wfm);
                        if (radioButton_impCurrent.Checked) stimIvsVWriter.WriteSingleSampleSingleLine(true, true);
                        else stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
                        stimIvsVTask.WaitUntilDone();
                        stimIvsVTask.Stop();
                        stimIvsVTask.Dispose();

                        if (!placedzeros)//try again
                        {

                            double[,] AnalogBuffer = new double[stimPulseTask.AOChannels.Count, STIMBUFFSIZE]; // buffer for analog channels
                            UInt32[] DigitalBuffer = new UInt32[STIMBUFFSIZE];

                            stimPulseTask.Stop();
                            stimDigitalTask.Stop();

                            stimPulseWriter.WriteMultiSample(true, AnalogBuffer);
                            stimDigitalWriter.WriteMultiSamplePort(true, DigitalBuffer);

                            //stimPulseTask.Start();

                            //stimDigitalTask.Start();
                            //stimPulseTask.WaitUntilDone();
                            stimPulseTask.Stop();
                            stimDigitalTask.Stop();

                        }
                    }

                    button_stim.Enabled = true;
                    button_stimExpt.Enabled = true;
                    openLoopStart.Enabled = true;
                    radioButton_impCurrent.Enabled = true;
                    radioButton_impVoltage.Enabled = true;
                    radioButton_stimCurrentControlled.Enabled = true;
                    radioButton_stimVoltageControlled.Enabled = true;
                    button_impedanceTest.Enabled = true;
                }
                else
                {
                    button_stim.Enabled = false;
                    button_stimExpt.Enabled = false;
                    openLoopStart.Enabled = false;
                    radioButton_impCurrent.Enabled = false;
                    radioButton_impVoltage.Enabled = false;
                    radioButton_stimCurrentControlled.Enabled = false;
                    radioButton_stimVoltageControlled.Enabled = false;
                    button_impedanceTest.Enabled = false;
                }
            }
            Console.WriteLine("updateStim");
        }


    }
}
