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
using rawType = System.Double;


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

            //Check for RTSI cables


            //this.comboBox_numChannels.SelectedIndex = 0; //Default of 16 channels
            this.comboBox_spikeDetAlg.SelectedIndex = 2; //Default spike det. algorithm is fixed RMS
            this.numChannels = Convert.ToInt32(comboBox_numChannels.SelectedItem);
            this.numChannelsPerDev = (numChannels < 32 ? numChannels : 32);
            //this.spikeBufferLength = Convert.ToInt32(Convert.ToDouble(textBox_spikeSamplingRate.Text) / 8); //Take quarter second samples
            spikeBufferLength = Convert.ToInt32(DEVICE_REFRESH * Convert.ToDouble(textBox_spikeSamplingRate.Text));
            this.currentRef = new int[2];
            this.numPost = Convert.ToInt32(numPostSamples.Value);
            this.numPre = Convert.ToInt32(numPreSamples.Value);

            //Setup default filename
            this.filenameOutput = "test0001";

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


            //Set the spike detector
            setSpikeDetector();

            //Create plots
            try
            {
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
        private void buttonStart_Click(object sender, EventArgs e)
        {
            trackingreads = new int[2];
            //trackingreads = {0 ,0};
            trackingproc = new int[2];
            //trackingproc = [0 ,0];
            //Ensure that, if recording is setup, that it has been done properly

            //Retrain Spike detector if required
            if (checkBox_RetrainOnRestart.Checked)
                setSpikeDetector();

            if (switch_record.Value)
            {
                if (filenameBase == null) //user hasn't specified a file
                    button_BrowseOutputFile_Click(null, null); //call file selection routine
                if (filenameBase == null) //this happens if the user pressed cancel for the dialog
                {
                    MessageBox.Show("An output file must be selected before recording."); //display an error message
                    return;
                }

                // If the user is just doing a single recording
                if (checkbox_repeatRecord.Checked)
                {
                    DateTime nowDate = DateTime.Now;//Get current time (local to computer);
                    string datePrefix = nowDate.ToString("'-'yyyy'-'MM'-'dd'-'HH'-'mm'-'ss");
                    filenameBase = originalNameBase + datePrefix;
                }

                filenameSpks = filenameBase + ".spk";
                if (Properties.Settings.Default.UseStimulator)
                    filenameStim = filenameBase + ".stim";
                if (Properties.Settings.Default.UseEEG)
                    filenameEEG = filenameBase + ".eeg";

                if (File.Exists(filenameSpks))
                {
                    DialogResult dr = MessageBox.Show("File " + filenameOutput + " exists. Overwrite?",
                        "NeuroRighter Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (dr == DialogResult.No)
                        button_BrowseOutputFile_Click(null, null); //call file selection routine
                    else if (dr == DialogResult.Cancel)
                        return;
                }
                NRAquire();
            }
            else
            {
                NRAquire();
            }

        }

        // Method to start the recording side of neurorighter
        private void NRAquire()
        {
            updateRecSettings();
            if (!taskRunning)
            {
                try
                {
                    // Modify the UI, so user doesn't try running multiple instances of tasks
                    this.Cursor = Cursors.WaitCursor;
                    buttonStop.Enabled = true;
                    buttonStart.Enabled = false;
                    comboBox_numChannels.Enabled = false;
                    numPreSamples.Enabled = false;
                    numPostSamples.Enabled = false;
                    settingsToolStripMenuItem.Enabled = false;
                    comboBox_SpikeGain.Enabled = false;
                    button_Train.Enabled = false;
                    checkBox_SaveRawSpikes.Enabled = false;
                    switch_record.Enabled = false;
                    processingSettingsToolStripMenuItem.Enabled = false;
                    button_spikeSamplingRate.PerformClick(); // updata samp freq
                    textBox_spikeSamplingRate.Enabled = false;
                    button_lfpSamplingRate.PerformClick();
                    textBox_lfpSamplingRate.Enabled = false;
                    textBox_MUASamplingRate.Enabled = false;
                    if (Properties.Settings.Default.SeparateLFPBoard)
                        comboBox_LFPGain.Enabled = false;

                    //Create new tasks
                    int numDevices = (numChannels > 32 ? Properties.Settings.Default.AnalogInDevice.Count : 1);
                    spikeTask = new List<Task>(numDevices);
                    for (int i = 0; i < numDevices; ++i)
                    {
                        spikeTask.Add(new Task("analogInTask_" + i));
                        //Create virtual channels for analog input
                        numChannelsPerDev = (numChannels < 32 ? numChannels : 32);
                        for (int j = 0; j < numChannelsPerDev; ++j)
                        {
                            spikeTask[i].AIChannels.CreateVoltageChannel(Properties.Settings.Default.AnalogInDevice[i] + "/ai" + j.ToString(),
                                "", AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                        }
                    }
                    //if (Properties.Settings.Default.UseSingleChannelPlayback)
                    //    spikeOutTask = new Task("spikeOutTask"); //For audio output
                    if (checkBox_video.Checked) //NB: This can't be checked unless video is enabled (no need to check properties)
                        triggerTask = new Task("triggerTask");


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
                        if (!Properties.Settings.Default.UseStimulator)
                        {
                            //Deal with non M-series devices (these can't use "ReferenceClockSource"
                            Device analogInDevice = DaqSystem.Local.LoadDevice(Properties.Settings.Default.AnalogInDevice[0]);

                            if (analogInDevice.ProductCategory == ProductCategory.MSeriesDaq || analogInDevice.ProductCategory == ProductCategory.XSeriesDaq)
                                spikeTask[0].Timing.ReferenceClockSource = "OnboardClock"; //This will be the master clock
                        }
                        else
                        {
                            spikeTask[0].Timing.ReferenceClockSource = "OnboardClock";//stimPulseTask.Timing.ReferenceClockSource;
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


                    #region Setup_Plotting
                    /**************************************************
                    /*   Setup plotting
                    /**************************************************/
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
                        numRows, numCols, MAX_SPK_WFMS, Properties.Settings.Default.ChannelMapping);
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

                    #region Setup_FileWriting
                    if (switch_record.Value)
                    {
                        DateTime dt = DateTime.Now; //Get current time (local to computer)
                        try
                        {
                            //fsSpks = new FileStream(filenameSpks, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, false);
                            fsSpks = new SpikeFileOutput(filenameBase, numChannels, spikeSamplingRate, Convert.ToInt32(numPreSamples.Value + numPostSamples.Value) + 1,
                                spikeTask[0], ".spk");
                            if (Properties.Settings.Default.UseStimulator)
                                fsStim = new FileStream(filenameStim, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, false);
                            if (checkBox_SaveRawSpikes.Checked) //If raw spike traces are to be saved
                            {
                                if (numChannels == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                                    rawFile = new FileOutputRemapped(filenameBase, numChannels, (int)spikeTask[0].Timing.SampleClockRate, 1, spikeTask[0], ".raw", Properties.Settings.Default.PreAmpGain);
                                else
                                    rawFile = new FileOutput(filenameBase, numChannels, (int)spikeTask[0].Timing.SampleClockRate, 1, spikeTask[0], ".raw", Properties.Settings.Default.PreAmpGain);
                            }

                            //File for clipped waveforms and spike times
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(numChannels)), 0, 2); //Int: Num channels
                            //fsSpks.Write(BitConverter.GetBytes(spikeSamplingRate), 0, 4); //Int: Sampling rate
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(Convert.ToInt16(numPreSamples.Value) + Convert.ToInt16(numPostSamples.Value) + 1)), 0, 2); //Int: Samples per waveform
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / spikeTask[0].AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                            //fsSpks.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

                            if (Properties.Settings.Default.UseLFPs)
                            {
                                if (Properties.Settings.Default.SeparateLFPBoard)
                                    lfpFile = new FileOutput(filenameBase, numChannels, lfpSamplingRate, 0, lfpTask, ".lfp", Properties.Settings.Default.PreAmpGain);
                                else //Using spikes A/D card to capture LFP data, too.
                                {
                                    if (numChannels == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                                        lfpFile = new FileOutputRemapped(filenameBase, numChannels, lfpSamplingRate, 1, spikeTask[0], ".lfp", Properties.Settings.Default.PreAmpGain);
                                    else
                                        lfpFile = new FileOutput(filenameBase, numChannels, lfpSamplingRate, 1, spikeTask[0], ".lfp", Properties.Settings.Default.PreAmpGain);
                                }
                            }

                            if (Properties.Settings.Default.UseStimulator)
                            {
                                fsStim.Write(BitConverter.GetBytes(spikeSamplingRate), 0, 4); //Int: Sampling rate
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                                fsStim.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond
                            }

                            if (Properties.Settings.Default.UseEEG)
                            {
                                //File for raw EEG traces
                                fsEEG = new FileStream(filenameEEG, FileMode.Create, FileAccess.Write, FileShare.None, 256 * 1024, false);
                                //Write header info: #chs, sampling rate, gain, date/time
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(eegTask.AIChannels.Count)), 0, 2); //Int: Num channels
                                fsEEG.Write(BitConverter.GetBytes(eegSamplingRate), 0, 4); //Int: Sampling rate
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / eegTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
                                fsEEG.Write(BitConverter.GetBytes(scalingCoeffsEEG[0]), 0, 8); //Double: Scaling coefficients
                                fsEEG.Write(BitConverter.GetBytes(scalingCoeffsEEG[1]), 0, 8);
                                fsEEG.Write(BitConverter.GetBytes(scalingCoeffsEEG[2]), 0, 8);
                                fsEEG.Write(BitConverter.GetBytes(scalingCoeffsEEG[3]), 0, 8);
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
                                fsEEG.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond
                            }
                        }
                        catch (System.IO.IOException ex)
                        {
                            MessageBox.Show(ex.Message);
                            reset();
                        }
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
                        //if (Properties.Settings.Default.SeparateLFPBoard)
                        //    filtLFPData[i] = new rawType[lfpBufferLength];
                        //else
                        //    filtLFPData[i] = new rawType[spikeBufferLength];
                        if (Properties.Settings.Default.UseLFPs)
                            finalLFPData[i] = new rawType[lfpBufferLength];
                    }
                    if (Properties.Settings.Default.UseStimulator)
                    {
                        stimDataBuffer = new double[STIM_BUFFER_LENGTH];
                        stimJump = (double)spikeSamplingRate * 0.0001; //num. indices in 100 us of data
                    }
                    _waveforms = new List<SpikeWaveform>(10); //Initialize to store threshold crossings
                    //newWaveforms = new List<SpikeWaveform>(10);

                    numPre = Convert.ToInt32(numPreSamples.Value);
                    numPost = Convert.ToInt32(numPostSamples.Value);

                    stimIndices = new List<StimTick>(5);
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
                    #endregion


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


                    //Start tasks (start LFP first, since it's triggered off spikeTask) and timer (for file writing)
                    if (checkBox_video.Checked)
                    {
                        byte[] b_array = new byte[3] { 255, 255, 255 };
                        DigitalWaveform wfm = new DigitalWaveform(3, 8, DigitalState.ForceDown);
                        wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                        triggerWriter.BeginWriteWaveform(true, wfm, null, null);
                    }
                    if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                        stimTimeTask.Start();
                    if (Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
                        lfpTask.Start();
                    if (Properties.Settings.Default.UseEEG)
                        eegTask.Start();
                    for (int i = spikeTask.Count - 1; i >= 0; --i) spikeTask[i].Start(); //Start first task last, since it has master clock

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

#if (USE_LOG_FILE)
                    logFile = new StreamWriter("log.txt");
    logFile.WriteLine("NeuroRighter Log File\r\n" + DateTime.Now + "\r\n\r\n");
#endif

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

        // Stop data aquisition and clean up
        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (taskRunning) reset();
            updateRecSettings();
        }

        

    }
}