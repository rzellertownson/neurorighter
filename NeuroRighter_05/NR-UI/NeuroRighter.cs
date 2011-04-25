// NEURORIGHTER.CS
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
using NeuroRighter.Aquisition;
using rawType = System.Double;
using NeuroRighter.SpikeDetection;
using NeuroRighter.FileWriting;
using ExtensionMethods;

namespace NeuroRighter
{

    ///<summary>Main Form for NeuroRighter application.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        // Neurorighter object constructor
        public NeuroRighter()
        {
            InitializeComponent();

            //Set version number
            this.Text += System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text += " (BETA)";

            //Set default values for certain controls
            comboBox_numChannels.SelectedItem = Properties.Settings.Default.DefaultNumChannels;

            //this.comboBox_numChannels.SelectedIndex = 0; //Default of 16 channels
            this.numChannels = Convert.ToInt32(comboBox_numChannels.SelectedItem);
            this.numChannelsPerDev = (numChannels < 32 ? numChannels : 32);
            spikeBufferLength = Convert.ToInt32(DEVICE_REFRESH * Convert.ToDouble(textBox_spikeSamplingRate.Text));
            this.currentRef = new int[2];

            // Create a new spike detection form so we can access its parameters
            spikeDet = new SpikeDetSettings(spikeBufferLength, numChannels, DEVICE_REFRESH, spikeSamplingRate);
            spikeDet.SettingsHaveChanged += new SpikeDetSettings.resetSpkDetSettingsHandler(spikeDet_SettingsHaveChanged);
            spikeDet.SetSpikeDetector();
            this.numPre = spikeDet.numPre;
            this.numPost = spikeDet.numPost;

            // Create a raw-scaler so that we can store the doubles produced by the NI tasks as 16-bit integers
            neuralDataScaler = new RawScale();
            auxDataScaler = new RawScale();
            
            //Setup default filename and create recordingSettings object
            this.filenameOutput = "test0001";
            recordingSettings = new RecordingSetup();
            recordingSettings.SettingsHaveChanged += new RecordingSetup.resetRecordingSettingsHandler(recordingSettings_SettingsHaveChanged);
            recordingSettings.SetSpikeFiltAccess(checkBox_spikesFilter.Checked);
            recordingSettings.RecallDefaultSettings();

            this.comboBox_LFPGain.Enabled = Properties.Settings.Default.SeparateLFPBoard;
            if (Properties.Settings.Default.SeparateLFPBoard)
                this.comboBox_LFPGain.SelectedIndex = 2;
            if (Properties.Settings.Default.UseEEG)
            {
                comboBox_eegGain.SelectedIndex = Properties.Settings.Default.EEGGain;
                textBox_eegSamplingRate.Text = Properties.Settings.Default.EEGSamplingRate.ToString();
                switch (Properties.Settings.Default.EEGNumChannels)
                {
                    case 1:
                        comboBox_eegNumChannels.SelectedIndex = 0; break;
                    case 2:
                        comboBox_eegNumChannels.SelectedIndex = 1; break;
                    case 3:
                        comboBox_eegNumChannels.SelectedIndex = 2; break;
                    case 4:
                        comboBox_eegNumChannels.SelectedIndex = 3; break;
                    default:
                        comboBox_eegNumChannels.SelectedIndex = 1; break;
                }
                updateSettings();
            }

            //Create plots
            try
            {
                //Create plot colormap
                NRBrainbow = numChannels.GenerateBrainbow();

                double gain = 20.0 / Convert.ToInt32(comboBox_SpikeGain.SelectedItem);

                spikeGraph = new GridGraph();
                spikeGraph.setup(4, 4, 100, false, 1 / 4.0, gain);
                spikeGraph.Resize += new EventHandler(spikeGraph.resize);
                spikeGraph.VisibleChanged += new EventHandler(spikeGraph.resize);
                spikeGraph.Parent = tabPage_spikes;
                spikeGraph.Dock = DockStyle.Fill;

                resetSpkWfm();
            }
            catch (InvalidOperationException e)
            {
                MessageBox.Show("Your graphics card is unsupported. Recording will be disabled." + e.Message);
                buttonStart.Enabled = false;
            }

            //Load gain settings
            comboBox_SpikeGain.SelectedIndex = Properties.Settings.Default.Gain;

            //Enable channel output button, if appropriate
            channelOut.Enabled = Properties.Settings.Default.UseSingleChannelPlayback;

            //Switch referencing scheme to last used
            switch (Properties.Settings.Default.SpikesReferencingScheme)
            {
                case 0:
                    radioButton_spikesReferencingCommonAverage.Checked = true;
                    break;
                case 1:
                    radioButton_spikesReferencingCommonMedian.Checked = true;
                    break;
                case 2:
                    radioButton_spikesReferencingCommonMedianLocal.Checked = true;
                    break;
                case 3:
                    radioButton_spikeReferencingNone.Checked = true;
                    break;
            }

            //Load saved filter settings
            SpikeLowCut.Value = Convert.ToDecimal(Properties.Settings.Default.SpikesLowCut);
            SpikeHighCut.Value = Convert.ToDecimal(Properties.Settings.Default.SpikesHighCut);
            SpikeFiltOrder.Value = Convert.ToDecimal(Properties.Settings.Default.SpikesNumPoles);
            LFPLowCut.Value = Convert.ToDecimal(Properties.Settings.Default.LFPLowCut);
            LFPHighCut.Value = Convert.ToDecimal(Properties.Settings.Default.LFPHighCut);
            LFPFiltOrder.Value = Convert.ToDecimal(Properties.Settings.Default.LFPNumPoles);

        }

        // Start button
        internal void buttonStart_Click(object sender, EventArgs e)
        {
            //Ensure that, if recording is setup, that it has been done properly
            //Retrain Spike detector if required
            if (checkBox_RetrainOnRestart.Checked)
                spikeDet.SetSpikeDetector();

            // Call the recording setup/start functions
            NRAcquisitionSetup();
            NRStartRecording();

        }

        // Method to set up the recording side of neurorighter
        private void NRAcquisitionSetup()
        {
            lock (this)
            {
                updateRecSettings();
                if (!taskRunning)
                {
                    try
                    {
                        // Modify the UI, so user doesn't try running multiple instances of tasks
                        this.Cursor = Cursors.WaitCursor;
                        comboBox_numChannels.Enabled = false;
                        spikeDet.numPreSamples.Enabled = false;
                        spikeDet.numPostSamples.Enabled = false;
                        settingsToolStripMenuItem.Enabled = false;
                        comboBox_SpikeGain.Enabled = false;
                        button_Train.Enabled = false;
                        button_SetRecordingStreams.Enabled = false;
                        switch_record.Enabled = false;
                        processingSettingsToolStripMenuItem.Enabled = false;
                        button_spikeSamplingRate.PerformClick(); // updata samp freq
                        textBox_spikeSamplingRate.Enabled = false;
                        button_lfpSamplingRate.PerformClick();
                        textBox_lfpSamplingRate.Enabled = false;
                        textBox_MUASamplingRate.Enabled = false;
                        button_startStimFromFile.Enabled = false;
                        if (Properties.Settings.Default.SeparateLFPBoard)
                            comboBox_LFPGain.Enabled = false;
                        numericUpDown_NumSnipsDisplayed.Enabled = false;

                        if (switch_record.Value)
                        {
                            // Create file name
                            if (filenameBase == null) //user hasn't specified a file
                                button_BrowseOutputFile_Click(null, null); //call file selection routine
                            if (filenameBase == null) //this happens if the user pressed cancel for the dialog
                            {
                                MessageBox.Show("An output file must be selected before recording."); //display an error message
                                return;
                            }

                            // If the user is just doing repeated recordings
                            if (checkbox_repeatRecord.Checked)
                            {
                                DateTime nowDate = DateTime.Now;//Get current time (local to computer);
                                string datePrefix = nowDate.ToString("'-'yyyy'-'MM'-'dd'-'HH'-'mm'-'ss");
                                filenameBase = originalNameBase + datePrefix;
                            }

                            if (File.Exists(filenameBase + "*"))
                            {
                                DialogResult dr = MessageBox.Show("File " + filenameOutput + " exists. Overwrite?",
                                    "NeuroRighter Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                                if (dr == DialogResult.No)
                                    button_BrowseOutputFile_Click(null, null); //call file selection routine
                                else if (dr == DialogResult.Cancel)
                                    return;
                            }

                            // Set file base name + number of channels
                            recordingSettings.SetFID(filenameBase);
                            recordingSettings.SetNumElectrodes(numChannels);
                        }


                        // Find out how many devs and channels/dev we are going to need
                        int numDevices = (numChannels > 32 ? Properties.Settings.Default.AnalogInDevice.Count : 1);
                        numChannelsPerDev = (numChannels < 32 ? numChannels : 32);

                        // Create spike aquisition task list
                        spikeTask = new List<Task>(numDevices);
                        NRAIChannelCollection spikeAqSet = new NRAIChannelCollection(numDevices, numChannelsPerDev);
                        spikeAqSet.SetupSpikeCollection(ref spikeTask);

                        // Check audio and video properties
                        if (Properties.Settings.Default.UseSingleChannelPlayback)
                            spikeOutTask = new Task("spikeOutTask"); //For audio output
                        if (checkBox_video.Checked) //NB: This can't be checked unless video is enabled (no need to check properties)
                            triggerTask = new Task("triggerTask");

                        // Set MUA sample rate
                        int muaSamplingRate = spikeSamplingRate / MUA_DOWNSAMPLE_FACTOR;

                        //Add LFP channels, if configured
                        if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                        {
                            lfpTask = new Task("lfpTask");
                            for (int i = 0; i < Convert.ToInt32(comboBox_numChannels.SelectedItem); ++i)
                                lfpTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.LFPDevice + "/ai" + i.ToString(), "",
                                    AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                            setGain(lfpTask, comboBox_LFPGain);
                            lfpTask.Control(TaskAction.Verify);
                        }


                        //Add EEG channels, if configured
                        if (Properties.Settings.Default.UseEEG)
                        {
                            comboBox_eegNumChannels.Enabled = false;
                            comboBox_eegGain.Enabled = false;
                            textBox_eegSamplingRate.Enabled = false;
                            eegTask = new Task("eegTask");
                            for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                                eegTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.EEGDevice + "/ai" +
                                    (i).ToString(), "", AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                            setGain(eegTask, comboBox_eegGain);
                            eegTask.Control(TaskAction.Verify);
                            eegSamplingRate = Convert.ToInt32(textBox_eegSamplingRate.Text);
                        }

                        //Add channel to control Cineplex, if configured
                        if (checkBox_video.Checked)
                            triggerTask.DOChannels.CreateChannel(Properties.Settings.Default.CineplexDevice + "/Port0/line0:7", "",
                                ChannelLineGrouping.OneChannelForAllLines);

                        //Change gain based on comboBox values (1-100)
                        for (int i = 0; i < spikeTask.Count; ++i)
                            setGain(spikeTask[i], comboBox_SpikeGain);

                        //Verify the Tasks
                        for (int i = 0; i < spikeTask.Count; ++i)
                            spikeTask[i].Control(TaskAction.Verify);
                        //if (Properties.Settings.Default.UseSingleChannelPlayback)
                        //    spikeOutTask.Control(TaskAction.Verify);

                        //Get sampling rates, set to private variables
                        spikeSamplingRate = Convert.ToInt32(textBox_spikeSamplingRate.Text);
                        lfpSamplingRate = Convert.ToInt32(textBox_lfpSamplingRate.Text);

                        //Version with videoTask as master clock
                        if (Properties.Settings.Default.UseCineplex)
                        {
                            for (int i = 0; i < spikeTask.Count; ++i)
                            {
                                spikeTask[i].Timing.ReferenceClockSource = videoTask.Timing.ReferenceClockSource;
                                spikeTask[i].Timing.ReferenceClockRate = videoTask.Timing.ReferenceClockRate;
                            }
                        }
                        else
                        {
                            string masterclock = "/" + Properties.Settings.Default.AnalogInDevice[0].ToString() + "/10MhzRefClock";//"OnboardClock";//
                            if (!Properties.Settings.Default.UseStimulator)
                            {
                                //Deal with non M-series devices (these can't use "ReferenceClockSource"
                                Device analogInDevice = DaqSystem.Local.LoadDevice(Properties.Settings.Default.AnalogInDevice[0]);

                                if (analogInDevice.ProductCategory == ProductCategory.MSeriesDaq || analogInDevice.ProductCategory == ProductCategory.XSeriesDaq)
                                    spikeTask[0].Timing.ReferenceClockSource = masterclock; //This will be the master clock
                            }
                            else
                            {
                                spikeTask[0].Timing.ReferenceClockSource = masterclock;//stimPulseTask.Timing.ReferenceClockSource;
                                spikeTask[0].Timing.ReferenceClockRate = 10000000.0; //stimPulseTask.Timing.ReferenceClockRate;
                            }
                            for (int i = 1; i < spikeTask.Count; ++i) //Set other analog in tasks to master clock
                            {
                                spikeTask[i].Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                                spikeTask[i].Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
                            }
                        }
                        spikeTask[0].Timing.ConfigureSampleClock("", spikeSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_spikeSamplingRate.Text) / 2));
                        for (int i = 1; i < spikeTask.Count; ++i)
                        {
                            //Pipe ai dev0's sample clock to slave devices
                            spikeTask[i].Timing.ConfigureSampleClock("/" + Properties.Settings.Default.AnalogInDevice[0] + "/ai/SampleClock", spikeSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_spikeSamplingRate.Text) / 2));

                            //Trigger off of ai dev0's trigger
                            spikeTask[i].Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" + Properties.Settings.Default.AnalogInDevice[0] +
                                "/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                        }
                        //if (Properties.Settings.Default.UseSingleChannelPlayback)
                        //{
                        //    spikeOutTask.Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                        //    spikeOutTask.Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
                        //    spikeOutTask.Timing.ConfigureSampleClock("", spikeTask[0].Timing.SampleClockRate,
                        //        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, spikeBufferLength);
                        //}

                        if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                        {
                            lfpTask.Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                            lfpTask.Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
                            lfpTask.Timing.ConfigureSampleClock("", lfpSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_lfpSamplingRate.Text) / 2));
                        }

                        if (Properties.Settings.Default.UseEEG)
                        {
                            eegTask.Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                            eegTask.Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
                            eegTask.Timing.ConfigureSampleClock("", eegSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_eegSamplingRate.Text) / 2));
                        }

                        if (Properties.Settings.Default.UseCineplex)
                        {
                            if (checkBox_video.Checked)
                            {
                                triggerTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.AnalogInDevice[0] + "/ai/SampleClock",
                                    spikeSamplingRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples,
                                    3);
                            }
                            if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                            {
                                lfpTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" +
                                    Properties.Settings.Default.AnalogInDevice[0] + "/ai/StartTrigger",
                                    DigitalEdgeStartTriggerEdge.Rising);
                            }
                            if (Properties.Settings.Default.UseEEG)
                            {
                                eegTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" +
                                    Properties.Settings.Default.AnalogInDevice[0] + "/ai/StartTrigger",
                                    DigitalEdgeStartTriggerEdge.Rising);
                            }
                        }

                        if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                        {
                            numStimReads = new List<int>(numDevices);
                            for (int i = 0; i < spikeTask.Count; ++i)
                                numStimReads.Add(0);
                            stimTimeTask = new Task("stimTimeTask");
                            stimTimeTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.StimInfoDevice + "/ai16", "",
                                AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                            stimTimeTask.AIChannels.CreateVoltageChannel(Properties.Settings.Default.StimInfoDevice + "/ai0", "", AITerminalConfiguration.Nrse,
                                -10.0, 10.0, AIVoltageUnits.Volts); //For triggers

                            stimTimeTask.Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                            stimTimeTask.Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
                            stimTimeTask.Timing.ConfigureSampleClock("", spikeSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_spikeSamplingRate.Text) / 2));
                            stimTimeTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                                "/" + Properties.Settings.Default.AnalogInDevice[0] + "/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                        }

                        //Setup scaling coefficients (to convert digital values to voltages)
                        scalingCoeffsSpikes = new List<double[]>(spikeTask.Count);
                        for (int i = 0; i < spikeTask.Count; ++i)
                            scalingCoeffsSpikes.Add(spikeTask[0].AIChannels[0].DeviceScalingCoefficients);
                        if (Properties.Settings.Default.SeparateLFPBoard)
                            scalingCoeffsLFPs = lfpTask.AIChannels[0].DeviceScalingCoefficients;
                        if (Properties.Settings.Default.UseEEG)
                            scalingCoeffsEEG = eegTask.AIChannels[0].DeviceScalingCoefficients;

                        // Setup auxiliary recording tasks
                        if (Properties.Settings.Default.useAuxAnalogInput)
                        {

                            auxAnInTask = new Task("AuxiliaryAnalogInput");
                            NRAIChannelCollection auxChanSet = new NRAIChannelCollection(Properties.Settings.Default.auxAnalogInChan);
                            auxChanSet.SetupAuxCollection(ref auxAnInTask);

                            auxAnInTask.Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                            auxAnInTask.Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;

                            //Pipe ai dev0's sample clock to slave devices
                            auxAnInTask.Timing.ConfigureSampleClock("", spikeSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_spikeSamplingRate.Text) / 2));

                            //Trigger off of ai dev0's trigger
                            // THIS DOES NOT WORK
                            // NEED TO TURN AUX AN INPUT INTO SAME TASK AS STIM TIMING.
                            if (Properties.Settings.Default.auxAnalogInDev == Properties.Settings.Default.StimInfoDevice
                                && Properties.Settings.Default.RecordStimTimes
                                || Properties.Settings.Default.auxAnalogInDev == Properties.Settings.Default.AnalogInDevice[0]
                                || Properties.Settings.Default.auxAnalogInDev == Properties.Settings.Default.AnalogInDevice[1])
                            {
                                MessageBox.Show("NeuroRighter does not currently support the recording of raw auxiliary signals on a device being used for other" +
                                                    " forms of analog input (e.g. stimulaiton timing or neural data). If you do not need to record stimulation timing," +
                                                    " then deselect that recording from the recording streams menu and send your auxiliary signals to AI lines on the device being used to record stimulation timing.");
                            }
                            else
                            {
                                auxAnInTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/Dev1/ai/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                            }
                        }
                        if (Properties.Settings.Default.useAuxDigitalInput)
                        {
                            auxDigInTask = new Task("AuxiliaryDigitalInput");
                            auxDigInTask.DIChannels.CreateChannel(Properties.Settings.Default.auxDigitalInPort,
                                "Auxiliary Digitial In", ChannelLineGrouping.OneChannelForAllLines);

                            auxDigInTask.Timing.ConfigureSampleClock("", spikeSamplingRate,
                                SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(Convert.ToDouble(textBox_spikeSamplingRate.Text) / 2));
                            auxDigInTask.Timing.SampleClockSource = spikeTask[0].Timing.SampleClockTerminal;
                        }



                        #region Setup_Plotting
                        /**************************************************
                    /*   Setup plotting
                    /**************************************************/

                        numSnipsDisplayed = (int)numericUpDown_NumSnipsDisplayed.Value;

                        if (Properties.Settings.Default.UseEEG)
                        {
                            eegGraph.ClearData();
                            eegGraph.Plots.RemoveAll();
                            eegGraph.Plots.Add();
                            eegGraph.Plots.Item(1).YAxis.SetMinMax(eegTask.AIChannels.All.RangeLow * (Convert.ToInt32(comboBox_eegNumChannels.SelectedItem) * 2 - 1),
                                eegTask.AIChannels.All.RangeHigh);
                            eegDownsample = 2;
                            eegDisplayGain = 1;
                            eegBufferLength = Convert.ToInt32(Convert.ToDouble(textBox_eegSamplingRate.Text) / 4);

                            setupEEGOffset();
                        }

                        #region PlotData_Buffers
                        //***********************
                        //Make PlotData buffers
                        //***********************
                        int downsample, numRows, numCols;
                        const double spikeplotlength = 0.25; //in seconds
                        switch (Convert.ToInt32(comboBox_numChannels.SelectedItem))
                        {
                            case 16:
                                numRows = numCols = 4;
                                downsample = 10;
                                break;
                            case 32:
                                numRows = numCols = 6;
                                downsample = 15;
                                break;
                            case 64:
                                numRows = numCols = 8;
                                downsample = 20; //if this gets really small, LFP data won't plot
                                break;
                            default:
                                numRows = numCols = 4;
                                downsample = 5;
                                break;
                        }

                        //Initialize graphs
                        if (spikeGraph != null) { spikeGraph.Dispose(); spikeGraph = null; }
                        spikeGraph = new GridGraph();
                        int samplesPerPlot = (int)(Math.Ceiling(DEVICE_REFRESH * spikeSamplingRate / downsample) * (spikeplotlength / DEVICE_REFRESH));
                        spikeGraph.setup(numRows, numCols, samplesPerPlot, false, 1 / 4.0, spikeTask[0].AIChannels.All.RangeHigh * 2.0);
                        spikeGraph.setMinMax(0, (float)(samplesPerPlot * numCols) - 1,
                            (float)(spikeTask[0].AIChannels.All.RangeLow * (numRows * 2 - 1)), (float)(spikeTask[0].AIChannels.All.RangeHigh));
                        spikeGraph.Dock = DockStyle.Fill;
                        spikeGraph.Parent = tabPage_spikes;

                        if (Properties.Settings.Default.UseLFPs)
                        {
                            if (lfpGraph != null) { lfpGraph.Dispose(); lfpGraph = null; }
                            lfpGraph = new RowGraph();
                            lfpGraph.setup(numChannels, (int)((Math.Ceiling(DEVICE_REFRESH * lfpSamplingRate / downsample) * (5 / DEVICE_REFRESH))),
                                5.0, spikeTask[0].AIChannels.All.RangeHigh * 2.0);
                            if (Properties.Settings.Default.SeparateLFPBoard)
                                lfpGraph.setMinMax(0, 5 * (int)(Math.Ceiling(DEVICE_REFRESH * lfpSamplingRate / downsample) / DEVICE_REFRESH) - 1,
                                    (float)(lfpTask.AIChannels.All.RangeLow * (numChannels * 2 - 1)), (float)(lfpTask.AIChannels.All.RangeHigh));
                            else
                                lfpGraph.setMinMax(0, 5 * (int)(Math.Ceiling(DEVICE_REFRESH * lfpSamplingRate / downsample) / DEVICE_REFRESH) - 1,
                                    (float)(spikeTask[0].AIChannels.All.RangeLow * (numChannels * 2 - 1)), (float)(spikeTask[0].AIChannels.All.RangeHigh));
                            lfpGraph.Dock = DockStyle.Fill;
                            lfpGraph.Parent = tabPage_LFPs;
                        }

                        if (Properties.Settings.Default.ProcessMUA)
                        {
                            if (muaGraph != null) { muaGraph.Dispose(); muaGraph = null; }
                            muaGraph = new RowGraph();
                            muaGraph.setup(numChannels, (int)((Math.Ceiling(DEVICE_REFRESH * muaSamplingRate / downsample) * (5 / DEVICE_REFRESH))),
                                5.0, spikeTask[0].AIChannels.All.RangeHigh * 2.0);
                            muaGraph.setMinMax(0, 5 * (int)(Math.Ceiling(DEVICE_REFRESH * muaSamplingRate / downsample) / DEVICE_REFRESH) - 1,
                                    (float)(spikeTask[0].AIChannels.All.RangeLow * (numChannels * 2 - 1)), (float)(spikeTask[0].AIChannels.All.RangeHigh));
                            muaGraph.Dock = DockStyle.Fill;
                            muaGraph.Parent = tabPage_MUA;

                            muaPlotData = new PlotDataRows(numChannels, downsample, muaSamplingRate * 5, muaSamplingRate,
                                    (float)spikeTask[0].AIChannels.All.RangeHigh * 2F, 0.5, 5, DEVICE_REFRESH);
                            //muaPlotData.setGain(Properties.Settings.Default.LFPDisplayGain);

                            //muaGraph.setDisplayGain(Properties.Settings.Default.LFPDisplayGain);
                            muaPlotData.dataAcquired += new PlotData.dataAcquiredHandler(muaPlotData_dataAcquired);
                        }

                        resetSpkWfm(); //Take care of spike waveform graph

                        double ampdec = (1 / Properties.Settings.Default.PreAmpGain);

                        spikePlotData = new PlotDataGrid(numChannels, downsample, spikeSamplingRate, spikeSamplingRate,
                            (float)(spikeTask[0].AIChannels.All.RangeHigh * 2.0), numRows, numCols, spikeplotlength,
                            Properties.Settings.Default.ChannelMapping, DEVICE_REFRESH);
                        spikePlotData.dataAcquired += new PlotData.dataAcquiredHandler(spikePlotData_dataAcquired);
                        spikePlotData.setGain(Properties.Settings.Default.SpikeDisplayGain);
                        spikeGraph.setDisplayGain(Properties.Settings.Default.SpikeDisplayGain);

                        if (Properties.Settings.Default.UseLFPs)
                        {
                            if (Properties.Settings.Default.SeparateLFPBoard)
                                lfpPlotData = new PlotDataRows(numChannels, downsample, lfpSamplingRate * 5, lfpSamplingRate,
                                    (float)lfpTask.AIChannels.All.RangeHigh * 2F, 0.5, 5, DEVICE_REFRESH);
                            else lfpPlotData = new PlotDataRows(numChannels, downsample, lfpSamplingRate * 5, lfpSamplingRate,
                                    (float)spikeTask[0].AIChannels.All.RangeHigh * 2F, 0.5, 5, DEVICE_REFRESH);
                            lfpPlotData.setGain(Properties.Settings.Default.LFPDisplayGain);

                            lfpGraph.setDisplayGain(Properties.Settings.Default.LFPDisplayGain);
                            lfpPlotData.dataAcquired += new PlotData.dataAcquiredHandler(lfpPlotData_dataAcquired);
                        }

                        waveformPlotData = new EventPlotData(numChannels, numPre + numPost + 1, (float)(spikeTask[0].AIChannels.All.RangeHigh * 2F),
                            numRows, numCols, numSnipsDisplayed, Properties.Settings.Default.ChannelMapping);
                        waveformPlotData.setGain(Properties.Settings.Default.SpkWfmDisplayGain);
                        spkWfmGraph.setDisplayGain(Properties.Settings.Default.SpkWfmDisplayGain);
                        waveformPlotData.dataAcquired += new EventPlotData.dataAcquiredHandler(waveformPlotData_dataAcquired);
                        waveformPlotData.start();
                        #endregion

                        spikeBufferLength = Convert.ToInt32(DEVICE_REFRESH * Convert.ToDouble(textBox_spikeSamplingRate.Text));
                        lfpBufferLength = Convert.ToInt32(DEVICE_REFRESH * Convert.ToDouble(textBox_lfpSamplingRate.Text));

                        if (Properties.Settings.Default.UseEEG)
                        {
                            eegGraph.Plots.Item(1).XAxis.SetMinMax(1 / Convert.ToDouble(textBox_eegSamplingRate.Text), 5);
                            eegPlotData = new double[Convert.ToInt32(comboBox_eegNumChannels.SelectedItem),
                                Convert.ToInt32(Convert.ToDouble(textBox_eegSamplingRate.Text) * 5 / eegDownsample)]; //five seconds of data
                        }
                        #endregion



                        #region Setup_Filters
                        //Setup filters, based on user's input
                        resetSpikeFilter();
                        if (Properties.Settings.Default.UseLFPs) resetLFPFilter();
                        resetEEGFilter();

                        muaFilter = new Filters.MUAFilter(numChannels, spikeSamplingRate, spikeBufferLength, 0.1, 100.0, MUA_DOWNSAMPLE_FACTOR, DEVICE_REFRESH);
                        #endregion

                        #region Setup_DataStorage
                        //Initialize data storing matrices
                        numChannels = Convert.ToInt32(comboBox_numChannels.SelectedItem);

                        numSpikeReads = new int[spikeTask.Count];

                        filtSpikeData = new rawType[numChannels][];

                        if (Properties.Settings.Default.UseLFPs)
                        {
                            filtLFPData = new rawType[numChannels][];
                            finalLFPData = new rawType[numChannels][];
                            for (int i = 0; i < filtSpikeData.GetLength(0); ++i)
                            {
                                if (Properties.Settings.Default.SeparateLFPBoard)
                                    filtLFPData[i] = new rawType[lfpBufferLength];
                                else
                                    filtLFPData[i] = new rawType[spikeBufferLength];
                            }
                        }

                        if (Properties.Settings.Default.ProcessMUA)
                        {
                            muaData = new double[numChannels][];
                            for (int c = 0; c < numChannels; ++c)
                                muaData[c] = new double[spikeBufferLength / MUA_DOWNSAMPLE_FACTOR];
                        }

                        if (Properties.Settings.Default.UseEEG)
                        {
                            filtEEGData = new double[Convert.ToInt32(comboBox_eegNumChannels.SelectedItem)][];
                            for (int i = 0; i < filtEEGData.GetLength(0); ++i)
                            {
                                filtEEGData[i] = new double[eegBufferLength];
                            }
                        }

                        for (int i = 0; i < filtSpikeData.GetLength(0); ++i)
                        {
                            filtSpikeData[i] = new rawType[spikeBufferLength];
                            if (Properties.Settings.Default.UseLFPs)
                                finalLFPData[i] = new rawType[lfpBufferLength];
                        }

                        if (Properties.Settings.Default.UseStimulator)
                        {
                            stimDataBuffer = new double[STIM_BUFFER_LENGTH];
                            stimJump = (double)spikeSamplingRate * 0.0001; //num. indices in 100 us of data
                        }

                        // Storage from spike waveforms
                        _waveforms = new List<SpikeWaveform>(10); //Initialize to store threshold crossings
                        numPre = Convert.ToInt32(spikeDet.numPreSamples.Value);
                        numPost = Convert.ToInt32(spikeDet.numPostSamples.Value);

                        stimIndices = new List<StimTick>(5);

                        //// Storage for aux input plotting and filtering
                        //if (Properties.Settings.Default.recordAuxAnalog)
                        //{
                        //    for (int i = 0; i < auxAnInTask.AIChannels.Count; ++i)
                        //        auxAnData[i] = new rawType[spikeBufferLength];
                        //}
                        //if (Properties.Settings.Default.recordAuxDigital)
                        //{
                        //        auxDigData = new uint[spikeBufferLength];
                        //}

                        #endregion

                        #region Verify Tasks
                        if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                            stimTimeTask.Control(TaskAction.Verify);
                        if (Properties.Settings.Default.UseEEG)
                            eegTask.Control(TaskAction.Verify);
                        if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                            lfpTask.Control(TaskAction.Verify);
                        if (checkBox_video.Checked)
                            triggerTask.Control(TaskAction.Verify);
                        for (int i = 0; i < spikeTask.Count; ++i)
                            spikeTask[i].Control(TaskAction.Verify);
                        if (Properties.Settings.Default.useAuxAnalogInput)
                            auxAnInTask.Control(TaskAction.Verify);
                        if (Properties.Settings.Default.useAuxDigitalInput)
                            auxDigInTask.Control(TaskAction.Verify);
                        #endregion

                        SetupFileWriting();

                        //Set callbacks for data acq.
                        taskRunning = true;
                        spikeReader = new List<AnalogMultiChannelReader>(spikeTask.Count);
                        for (int i = 0; i < spikeTask.Count; ++i)
                        {
                            spikeReader.Add(new AnalogMultiChannelReader(spikeTask[i].Stream));
                            spikeReader[i].SynchronizeCallbacks = true;
                        }
                        //if (Properties.Settings.Default.UseSingleChannelPlayback)
                        //    spikeOutWriter = new AnalogSingleChannelWriter(spikeOutTask.Stream);
                        if (checkBox_video.Checked)
                            triggerWriter = new DigitalSingleChannelWriter(triggerTask.Stream);

                        //if (Properties.Settings.Default.UseSingleChannelPlayback)
                        //    spikeOutWriter.SynchronizeCallbacks = false; //These don't use UI, so they don't need to be synched

                        spikeCallback = new AsyncCallback(AnalogInCallback_spikes);

                        if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                        {
                            stimTimeReader = new AnalogMultiChannelReader(stimTimeTask.Stream);
                        }

                        if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                        {
                            lfpReader = new AnalogUnscaledReader(lfpTask.Stream);
                            lfpReader.SynchronizeCallbacks = true;
                            lfpCallback = new AsyncCallback(AnalogInCallback_LFPs);
                        }
                        if (Properties.Settings.Default.UseEEG)
                        {
                            eegReader = new AnalogUnscaledReader(eegTask.Stream);
                            eegReader.SynchronizeCallbacks = true;
                            eegCallback = new AsyncCallback(AnalogInCallback_EEG);
                        }

                        if (Properties.Settings.Default.useAuxAnalogInput)
                        {
                            auxAnReader = new AnalogUnscaledReader(auxAnInTask.Stream);
                            auxAnReader.SynchronizeCallbacks = true;
                            auxAnCallback = new AsyncCallback(AnalogInCallback_AuxAn);
                        }

                        if (Properties.Settings.Default.useAuxDigitalInput)
                        {
                            auxDigReader = new DigitalSingleChannelReader(auxDigInTask.Stream);
                            auxDigReader.SynchronizeCallbacks = true;
                            auxDigCallback = new AsyncCallback(AnalogInCallback_AuxDig);
                        }

                        //Setup background workers for data processing
                        bwSpikes = new List<BackgroundWorker>(spikeTask.Count);
                        for (int i = 0; i < spikeTask.Count; ++i)
                        {
                            bwSpikes.Add(new BackgroundWorker());
                            bwSpikes[i].DoWork += new DoWorkEventHandler(bwSpikes_DoWork);
                            bwSpikes[i].RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwSpikes_RunWorkerCompleted);
                            bwSpikes[i].WorkerSupportsCancellation = true;
                        }


                        //Make persistent buffers for spikeData
                        spikeData = new List<AnalogWaveform<double>[]>(spikeReader.Count);
                        for (int i = 0; i < spikeReader.Count; ++i)
                        {
                            spikeData.Add(new AnalogWaveform<double>[numChannelsPerDev]);
                            for (int j = 0; j < numChannelsPerDev; ++j)
                                spikeData[i][j] = new AnalogWaveform<double>(spikeBufferLength);
                        }

                        //Make channel playback task
                        if (Properties.Settings.Default.UseSingleChannelPlayback)
                            BNCOutput = new ChannelOutput(spikeSamplingRate, 0.1, DEVICE_REFRESH, spikeTask[0],
                                Properties.Settings.Default.SingleChannelPlaybackDevice, 0);

                        this.Cursor = Cursors.Default;
                    }
                    catch (DaqException exception)
                    {
                        //Display Errors
                        this.Cursor = Cursors.Default;
                        MessageBox.Show(exception.Message);
                        reset();
                    }
                }
            }
        }

        // Start all the tasks having to do with recording
        private void NRStartRecording()
        {
            lock (this)
            {
                try
                {
                    // Take care of buttons
                    buttonStop.Enabled = true;
                    buttonStart.Enabled = false;

                    // integers tracking the number of reads performed
                    trackingReads = new int[2];
                    trackingProc = new int[2];
                    trackingDigReads = new int();

                    //Start tasks (start LFP first, since it's triggered off spikeTask) and timer (for file writing)
                    if (checkBox_video.Checked)
                    {
                        byte[] b_array = new byte[3] { 255, 255, 255 };
                        DigitalWaveform wfm = new DigitalWaveform(3, 8, DigitalState.ForceDown);
                        wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                        triggerWriter.BeginWriteWaveform(true, wfm, null, null);
                    }


                    if (Properties.Settings.Default.useAuxDigitalInput)
                        auxDigInTask.Start();
                    if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                        stimTimeTask.Start();
                    if (Properties.Settings.Default.useAuxAnalogInput)
                        auxAnInTask.Start();
                    if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                        lfpTask.Start();
                    if (Properties.Settings.Default.UseEEG)
                        eegTask.Start();
                    for (int i = spikeTask.Count - 1; i >= 0; --i)
                        spikeTask[i].Start(); //Start first task last, since it has master clock

                    // Start data collection
                    if (Properties.Settings.Default.useAuxAnalogInput)
                        auxAnReader.BeginReadInt16(spikeBufferLength, auxAnCallback, auxAnReader);
                    if (Properties.Settings.Default.useAuxDigitalInput)
                        auxDigReader.BeginReadMultiSamplePortUInt32(spikeBufferLength, auxDigCallback, auxDigReader);
                    if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                        lfpReader.BeginReadInt16(lfpBufferLength, lfpCallback, lfpReader);
                    if (Properties.Settings.Default.UseEEG)
                        eegReader.BeginReadInt16(eegBufferLength, eegCallback, eegReader);
                    for (int i = 0; i < spikeReader.Count; ++i)
                        spikeReader[i].BeginMemoryOptimizedReadWaveform(spikeBufferLength, spikeCallback, i,
                            spikeData[i]);

                    //Set start time
                    experimentStartTime = DateTime.Now;
                    double sec2add = 60 * Convert.ToDouble(numericUpDown_timedRecordingDuration.Value) + Convert.ToDouble(numericUpDown_timedRecordingDurationSeconds.Value);
                    timedRecordingStopTime = DateTime.Now.AddSeconds(sec2add);
                    timer_timeElapsed.Enabled = true;

                    if (checkBox_video.Checked)
                    {
                        triggerTask.WaitUntilDone();
                        triggerTask.Dispose();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }
        }

        // Instantiate the data stream writers within the recordingSettings object
        private void SetupFileWriting()
        {
            lock (this)
            {
                // Formate the raw data scalers, needed regardless of whether user is recording or not
                neuralDataScaler.Set16BitResolution(
                    spikeTask[0].AIChannels.All.RangeHigh / (Int16.MaxValue * Properties.Settings.Default.PreAmpGain));

                if (Properties.Settings.Default.useAuxAnalogInput)
                    auxDataScaler.Set16BitResolution(auxAnInTask.AIChannels.All.RangeHigh / Int16.MaxValue);

                if (switch_record.Value)
                {
                    DateTime dt = DateTime.Now; //Get current time (local to computer)
                    try
                    {
                        // Tell NR that that its about to encounter the first write
                        firstRawWrite = new bool[spikeTask.Count];
                        for (int i = 0; i < firstRawWrite.Length; ++i)
                        {
                            firstRawWrite[i] = true;
                        }

                        // 1. spk stream
                        recordingSettings.Setup("spk", spikeTask[0], numPre, numPost);

                        // 2. raw streams
                        recordingSettings.Setup("raw", spikeTask[0]);
                        recordingSettings.Setup("salpa", spikeTask[0]);
                        recordingSettings.Setup("spkflt", spikeTask[0]);
                        if (Properties.Settings.Default.SeparateLFPBoard)
                            recordingSettings.Setup("lfp", lfpTask, lfpSamplingRate);
                        else
                            recordingSettings.Setup("lfp", spikeTask[0], lfpSamplingRate);
                        recordingSettings.Setup("eeg", eegTask, eegSamplingRate);

                        // 3. other
                        recordingSettings.Setup("stim", spikeTask[0]);
                        if (auxAnInTask != null)
                            recordingSettings.Setup("aux", auxAnInTask);
                        recordingSettings.Setup("dig", spikeTask[0]);
                    }
                    catch (System.IO.IOException ex)
                    {
                        MessageBox.Show(ex.Message);
                        reset();
                    }
                }
            }
        }

        // Stop data aquisition and clean up
        private void buttonStop_Click(object sender, EventArgs e)
        {
            Thread.Sleep(200); // Let file writing etc. finish
            if (taskRunning)
                reset();
            updateRecSettings();
        }

        
    }
}