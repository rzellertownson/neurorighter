// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
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

namespace NeuroRighter
{
    using rawType = System.Double;

    ///<summary>Main Form for NeuroRighter application.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        #region Private_Variables
        /* I apologize for declaring so many private variables, but it helps decrease the program's
         * memory footprint and speeds up the performance, all at the expense of readability :(  */
        private List<Task> spikeTask;  //NI Tasks for reading data
        private Task lfpTask;
        private Task eegTask;
        private Task videoTask;  //To synch up Cineplex system
        private Task triggerTask; //To trigger everything simultaneously, using AO
        private Task stimDigitalTask;
        private Task stimPulseTask;
        private Task stimTimeTask; //Records timing of stim pulses
        private Task stimIvsVTask; //Determines whether stim is current or voltage controlled
        private List<AnalogMultiChannelReader> spikeReader;
        private List<AnalogWaveform<double>[]> spikeData;
        private AnalogUnscaledReader lfpReader;
        private AnalogUnscaledReader eegReader;
        private DigitalSingleChannelWriter triggerWriter;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimPulseWriter;
        private DigitalSingleChannelWriter stimFromFileDigitalWriter;
        private AnalogMultiChannelWriter stimFromFileAnalogWriter;
        private AnalogMultiChannelReader stimTimeReader;
        private DigitalSingleChannelWriter stimIvsVWriter;
        private AsyncCallback spikeCallback;
        private AsyncCallback lfpCallback;
        private AsyncCallback eegCallback;
        private bool taskRunning;  //Shows whether data are being acquired or not
        private string filenameOutput;
        private string filenameBase;
        private string filenameEEG;
        private string filenameSpks;  //Spike times and waveforms
        private string filenameStim; //Stim times
        //private FileStream fsSpks;
        private SpikeFileOutput fsSpks;
        private FileStream fsStim;
        private FileStream fsEEG;
        private double[,] eegPlotData;
        private int spikeBufferLength;  //How much data is acquired per read
        private int lfpBufferLength;
        private int eegBufferLength;
        private int eegDownsample;
        private BesselBandpassFilter[] eegFilter;
        private ButterworthFilter[] spikeFilter; //Testing my own filter
        private ButterworthFilter[] lfpFilter;
        private int[] numSpkWfms;   //Num. of plotted spike waveforms per channel
        private short[,] lfpData;
        private short[,] eegData;
        private rawType[][] filtSpikeData;
        private rawType[][] filtLFPData;
        private rawType[][] filtEEGData;
        private rawType[][] finalLFPData; //Stores filtered and downsampled LFP
        private double[] stimDataBuffer; //Stores some of the old samples for the next read (to avoid reading only half an encoding during a buffer read)
        private int spikeSamplingRate;
        private int lfpSamplingRate;
        private int eegSamplingRate;
        private int[] numSpikeReads; //Number of times the spike buffer has been read (for adding time stamps)
        private List<int> numStimReads;
        private double stimJump;  //Num. of indices to jump ahead during stim reads (make sure to round before using)
        private List<double[]> scalingCoeffsSpikes; //Scaling coefficients for NI-DAQs
        private double[] scalingCoeffsLFPs;
        private double[] scalingCoeffsEEG;
        private List<SpikeWaveform> _waveforms;  //Locations of threshold crossings
        internal List<SpikeWaveform> waveforms
        {
            get { return _waveforms; }
        }
        private int numPre;     //Num samples before threshold crossing to save
        private int numPost;    //Num samples after ' '
        private rawType[] thrSALPA; //Thresholds for SALPA
        private SALPA2 SALPAFilter;
        private int numChannels;
        private int numChannelsPerDev;
        private int[] currentRef;
        private DateTime stimStopTime;
        private ArrayList stimList; //List of stimPulse objects
        private System.Threading.Timer stimTimer;
        private double[] eegOffset; 
        private double eegDisplayGain;
        private SerialPort serialOut; //Serial port to control programmable referencing
        private List<StimTick> stimIndices; //For sending stim times to SALPA (or any other routine that wants them)
        private PlotData spikePlotData;
        private PlotData lfpPlotData;
        private PlotData muaPlotData;
        private EventPlotData waveformPlotData;
        private Filters.Referencer referncer;
        private DateTime experimentStartTime;
        private Filters.ArtiFilt artiFilt;
        private ChannelOutput BNCOutput;
        private DateTime timedRecordingStopTime;
        private Filters.MUAFilter muaFilter;
        private double[][] muaData;
        private int SALPA_WIDTH;

        //Plots
        private GridGraph spikeGraph;
        private GridGraph spkWfmGraph;
        private RowGraph lfpGraph;
        private RowGraph muaGraph;

        private FileOutput rawFile;
        private FileOutput lfpFile;
        private SpikeDetector spikeDetector;
        private delegate void plotData_dataAcquiredDelegate(object item); //Used for plotting callbacks, thread-safety
        #endregion

        #region DebugVariables
#if (USE_LOG_FILE)
        internal StreamWriter logFile; //internal so other classes can access
#endif
    
        #endregion

        #region Constants
        internal const double DEVICE_REFRESH = 0.01; //Time in seconds between reads of NI-DAQs
        private const int NUM_SECONDS_TRAINING = 3; //Num. seconds to train noise levels
        private const int MAX_SPK_WFMS = 10; //Max. num. of plotted spike waveforms, before clearing and starting over
        private int STIM_SAMPLING_FREQ = 100000; //Resolution at which stim pulse waveforms are generated
        private int STIMBUFFSIZE = 10000; // Number of samples delivered to DAQ per buffer load during stimulation from file
        private const int STIM_PADDING = 10; //Num. 0V samples on each side of stim. waveform 
        private const int STIM_BUFFER_LENGTH = 20;  //#pts. to keep in stim time reading buffer
        private const double VOLTAGE_EPSILON = 1E-7; //If two samples are within 100 nV, I'll call them "same"
        private const int MUA_DOWNSAMPLE_FACTOR = 50;
        #endregion

        #region Constructor
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
            this.comboBox_spikeDetAlg.SelectedIndex = 1; //Default spike det. algorithm is RMS
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
            }


            //Select default channels for Bakkum expt.
            for (int i = 0; i < listBox_closedLoopLearningPTSElectrodes.Items.Count; ++i)
                listBox_closedLoopLearningPTSElectrodes.SetSelected(i, true);

            updateSettings();

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

            //Set default for closed-loop follower expt.
            comboBox_ClosedLoopFollowerAlgorithm.SelectedIndex = 0;
        }
        #endregion

        /*********************************************************************
         * Select output files                                               *
         *********************************************************************/
        private void button_BrowseOutputFile_Click(object sender, EventArgs e)
        {
            // Set dialog's default properties
            SaveFileDialog saveFileDialog_OutputFile = new SaveFileDialog();
            saveFileDialog_OutputFile.DefaultExt = "*.spk";         //default extension is for spike files
            saveFileDialog_OutputFile.FileName = filenameOutput;    //default file name
            saveFileDialog_OutputFile.Filter = "NeuroRighter Files|*.spk|All Files|*.*";

            // Display Save File Dialog (Windows forms control)
            DialogResult result = saveFileDialog_OutputFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                filenameOutput = saveFileDialog_OutputFile.FileName;
                textBox_OutputFile.Text = filenameOutput;
                toolTip_outputFilename.SetToolTip(textBox_OutputFile, filenameOutput);
                filenameBase = filenameOutput.Substring(0, filenameOutput.Length - 4);
                filenameSpks = filenameBase + ".spk";
                if (Properties.Settings.Default.UseStimulator)
                    filenameStim = filenameBase + ".stim";
                if (Properties.Settings.Default.UseEEG)
                    filenameEEG = filenameBase + ".eeg";
            }
        }

        #region StartButton
        /********************************************************
         * buttonStart                                          *
         *                                                      *
         * This is where the data acquisition is intiated       *
         ********************************************************/
        //Main body of code in this function
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (!taskRunning)
            {
                //Ensure that, if recording is setup, that it has been done properly
                if (switch_record.Value)
                {
                    if (filenameBase == null) //user hasn't specified a file
                        button_BrowseOutputFile_Click(null, null); //call file selection routine
                    if (filenameBase == null) //this happens if the user pressed cancel for the dialog
                    {
                        MessageBox.Show("An output file must be selected before recording."); //display an error message
                        return;
                    }

                    //If file exists, verify over-writing
                    if (File.Exists(filenameOutput))
                    {
                        DialogResult dr = MessageBox.Show("File " + filenameOutput + " exists. Overwrite?", 
                            "NeuroRighter Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                        if (dr == DialogResult.No)
                            button_BrowseOutputFile_Click(null, null); //call file selection routine
                        else if (dr == DialogResult.Cancel)
                            return;
                    }
                }

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
                            
                            if (analogInDevice.ProductCategory == ProductCategory.MSeriesDaq) 
                                spikeTask[0].Timing.ReferenceClockSource = "OnboardClock"; //This will be the master clock
                        }
                        else
                        {
                            spikeTask[0].Timing.ReferenceClockSource = stimPulseTask.Timing.ReferenceClockSource;
                            spikeTask[0].Timing.ReferenceClockRate = stimPulseTask.Timing.ReferenceClockRate;
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

                    double ampdec = (1/Properties.Settings.Default.PreAmpGain);

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
                                    rawFile = new FileOutputRemapped(filenameBase, numChannels, (int)spikeTask[0].Timing.SampleClockRate, 1, spikeTask[0], ".raw");
                                else
                                    rawFile = new FileOutput(filenameBase, numChannels, (int)spikeTask[0].Timing.SampleClockRate, 1, spikeTask[0], ".raw");
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
                                    lfpFile = new FileOutput(filenameBase, numChannels, lfpSamplingRate, 0, lfpTask, ".lfp");
                                else //Using spikes A/D card to capture LFP data, too.
                                {
                                    if (numChannels == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                                        lfpFile = new FileOutputRemapped(filenameBase, numChannels, lfpSamplingRate, 1, spikeTask[0], ".lfp");
                                    else
                                        lfpFile = new FileOutput(filenameBase, numChannels, lfpSamplingRate, 1, spikeTask[0], ".lfp");
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
                        byte[] b_array = new byte[3] {255, 255, 255};
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
                    double sec2add = 60*Convert.ToDouble(numericUpDown_timedRecordingDuration.Value) + Convert.ToDouble(numericUpDown_timedRecordingDurationSeconds.Value);
                    timedRecordingStopTime = DateTime.Now.AddSeconds(sec2add) ;
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
        #endregion

        #region Spike_Acquisition
        /****************************************************
         * Spike data acquisition and plotting              *
         ****************************************************/
        private List<BackgroundWorker> bwSpikes;

        /// <summary>
        /// Keeps track of when a stimulus pulse occurred
        /// </summary>
        public struct StimTick
        {
            public int index;
            public int numStimReads;
            public StimTick(int index, int numStimReads)
            {
                this.index = index;
                this.numStimReads = numStimReads;
            }
        }

        //*************************
        //AnalogInCallback_spikes
        //*************************
        private double triggerStartTime = double.PositiveInfinity; //keep track of start of an integration trigger
        private double triggerStopTime = double.PositiveInfinity; //offset of integration trigger
        private bool inTrigger = false;
        private Object numStimReadsLock = new object();
        private void AnalogInCallback_spikes(IAsyncResult ar)
        {
            try
            {
                if (taskRunning)
                {
                    int taskNumber = (int)ar.AsyncState;

                    #region Stim_Timing_Acquisition
                    if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                    {
                        lock (numStimReadsLock)
                        {
                            bool getStimData = true;
                            for (int i = 0; i < numStimReads.Count; ++i)
                            {
                                if (numStimReads[taskNumber] < numStimReads[i]) //Test if all stim reads are equal
                                {
                                    getStimData = false;
                                    ++numStimReads[taskNumber];
                                    break;
                                }
                            }

                            if (getStimData)
                            {
                                //double[] stimData = stimTimeReader.ReadMultiSample(spikeBufferLength);
                                double[,] stimData = stimTimeReader.ReadMultiSample(spikeBufferLength);
                                //NB: Should make this read memory optimized...

                                //Copy new data into prepended data, to deal with edge effects
                                double[] prependedData = new double[spikeBufferLength + STIM_BUFFER_LENGTH];
                                for (int i = 0; i < STIM_BUFFER_LENGTH; ++i)
                                    prependedData[i] = stimDataBuffer[i];
                                for (int i = 0; i < spikeBufferLength; ++i)
                                    prependedData[i + STIM_BUFFER_LENGTH] = stimData[0, i];

                                int startTimeStim = numStimReads[taskNumber] * spikeBufferLength - STIM_BUFFER_LENGTH; //Used to mark stim time for file
                                //Encoding is [v1 v2 v3], each lasting 200us
                                //'v1' and 'v2' encode channel number, 'v3' is the stim voltage
                                //'v1' says "which group of eight" was stimulated, 'v2' says
                                //     "which electrode in the group of eight".  E.g., electrode
                                //     16 would have v1=2 and v2=8.  'v1' and 'v2' are always in
                                //     the range of 1-8 volts
                                for (int i = 0; i < spikeBufferLength; ++i)
                                {
                                    //Check for stimIndices (this uses a different buffer, so it's synced to each buffer read
                                    if (stimData[0,i] > 0.9) stimIndices.Add(new StimTick(i, numStimReads[taskNumber]));

                                    //Get appropriate data and write to file
                                    if (prependedData[i] > 0.8 && prependedData[i + (int)stimJump] > 0.8 && prependedData[i + (int)(2 * stimJump)] > 0.8)
                                    {
                                        //stimIndices.Add(new StimTick(i - STIM_BUFFER_LENGTH, numStimReads[taskNumber]));
                                        if (switch_record.Value)
                                        {
                                            fsStim.Write(BitConverter.GetBytes(startTimeStim + i), 0, 4); //Write time (index number)
                                            fsStim.Write(BitConverter.GetBytes((Convert.ToInt16((prependedData[i + 1] + prependedData[i + (int)stimJump]) / 2) - //average a couple values
                                                (short)1) * (short)8 +
                                                Convert.ToInt16((prependedData[i + (int)(2 * stimJump) + 1] +
                                                prependedData[i + (int)(3 * stimJump)]) / 2)), 0, 2); //channel (-1 since everything should be 0-based) // JN, CHANGED TO BE 1 BASED FOR NORMAL PEOPLE
                                            fsStim.Write(BitConverter.GetBytes(prependedData[i + (int)(5 * stimJump)]), 0, 8); //Stim voltage
                                            fsStim.Write(BitConverter.GetBytes(prependedData[i + (int)(7 * stimJump)]), 0, 8); //Stim pulse width (div by 100us)
                                        }
                                        //Overwrite data as 0s, to prevent detecting the middle of a stim pulse in the next buffer cycle
                                        for (int j = 0; j < (int)(8 * stimJump) + 1; ++j)
                                            prependedData[j + i] = 0;
                                        i += (int)(9 * stimJump); //Jump past rest of waveform
                                    }
                                }
                                if (!inTrigger) //Assumes trigger lasts longer than refresh time
                                {
                                    for (int i = 0; i < stimData.GetLength(1); ++i)
                                    {
                                        //Check if there's a trigger change
                                        if (stimData[1, i] > 2.5)
                                        {
                                            triggerStartTime = i + numStimReads[taskNumber] * spikeBufferLength;
                                            triggerStopTime = double.PositiveInfinity; //Do this to ensure that we capture spikes till 
                                            inTrigger = true;

#if (DEBUG1)
                                            logFile.WriteLine("Trigger start time: " + triggerStartTime);
#endif
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < stimData.GetLength(1); ++i)
                                    {
                                        if (stimData[1, i] < 2.5)
                                        {
                                            triggerStopTime = i + numStimReads[taskNumber] * spikeBufferLength;
                                            inTrigger = false;
#if (DEBUG1)
                                            logFile.WriteLine("Trigger stop time: " + triggerStopTime);
#endif
                                            break;
                                        }
                                    }
                                }

                                for (int i = spikeBufferLength; i < spikeBufferLength + STIM_BUFFER_LENGTH; ++i)
                                    stimDataBuffer[i - spikeBufferLength] = prependedData[i];
                                
                                //Clear out expired stimIndices
                                if (stimIndices.Count > 0)
                                {
                                    int oldestStimRead = numStimReads[0];
                                    for (int i = 1; i < numStimReads.Count; ++i)
                                    {
                                        if (numStimReads[i] < oldestStimRead)
                                            oldestStimRead = numStimReads[i];
                                    }
                                    for (int i = stimIndices.Count - 1; i >= 0; --i)
                                        if (stimIndices[i].numStimReads < oldestStimRead - 1) //Add -1 to buy us some breating room
                                            stimIndices.RemoveAt(i);
                                }
                                ++numStimReads[taskNumber];
                            }
                        }
                    }
                    #endregion

                    //Start background worker to process newly acquired data
                    bwSpikes[taskNumber].RunWorkerAsync(new Object[] { taskNumber, ar });
                }
            }
            catch (DaqException exception)
            {
                //Display Errors
                MessageBox.Show(exception.Message);
                reset();
            }
        }

        /// <summary>
        /// Process raw data (detect spikes, write to file, etc.)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bwSpikes_DoWork(object sender, DoWorkEventArgs e)
        {
            Object[] state = (Object[])e.Argument;
            int taskNumber = (int)state[0];

            //Copy data into a new buffer
            for (int i = 0; i < numChannelsPerDev; ++i)
                spikeData[taskNumber][i].GetRawData(0, spikeBufferLength, filtSpikeData[taskNumber * numChannelsPerDev + i], 0);
            
            //Account for Pre-amp gain
            double ampdec = (1/Properties.Settings.Default.PreAmpGain);
            for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                for (int j = 0; j < spikeBufferLength; ++j)
                    filtSpikeData[i][j] = ampdec * filtSpikeData[i][j];

            #region Write RAW data
            //Write data to file
            if (switch_record.Value && checkBox_SaveRawSpikes.Checked)
            {
                rawType oneOverResolution = Int16.MaxValue / spikeTask[0].AIChannels.All.RangeHigh; //Resolution of 16-bit signal; multiplication is much faster than division
                rawType tempVal;
                for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                    {
                        //This next section deals with the fact that NI's range is soft--i.e., values can exceed the max and min values of the range (but trying to convert these to shorts would crash the program)
                        tempVal = Math.Round(filtSpikeData[i][j] * oneOverResolution);
                        if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                        else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                        else { tempVal = Int16.MinValue; }
                        rawFile.read((short)tempVal, i);
                    }
            }

            #endregion

            #region LFP_Filtering
            //Filter for LFPs
            if (!Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
            {
                //Copy to new array
                for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                        filtLFPData[i][j] = filtSpikeData[i][j];

                #region ArtiFilt (interpolation filtering)
                if (checkBox_artiFilt.Checked)
                    artiFilt.filter(ref filtLFPData, stimIndices, taskNumber * numChannelsPerDev, numChannelsPerDev, numStimReads[taskNumber] - 1);
                #endregion

                if (checkBox_LFPsFilter.Checked)
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        lfpFilter[i].filterData(filtLFPData[i]);
                //Downsample for LFPs
                double dsFactor = (double)spikeSamplingRate / (double)lfpSamplingRate;
                if (dsFactor % 1 == 0) //If it's an integer
                {
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] = filtLFPData[i][(int)(dsFactor * j)];
                }
                else
                {
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] = filtLFPData[i][(int)(Math.Round(dsFactor * j))];
                }

                //Do IISZapper stuff
                if (IISDetected != null) IISDetected(this, finalLFPData, numSpikeReads[taskNumber]);

                #region WriteLFPFile
                if (switch_record.Value) //Convert to 16-bit ints, then write to file
                {
                    rawType oneOverResolution = Int16.MaxValue / spikeTask[0].AIChannels.All.RangeHigh; //Resolution of 16-bit signal; multiplication is much faster than division
                    rawType tempLFPVal;
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                        {
                            //This next section deals with the fact that NI's range is soft--i.e., values can exceed the max and min values of the range (but trying to convert these to shorts would crash the program)
                            tempLFPVal = Math.Round(finalLFPData[i][j] * oneOverResolution);
                            if (tempLFPVal <= Int16.MaxValue && tempLFPVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                            else if (tempLFPVal > Int16.MaxValue) { tempLFPVal = Int16.MaxValue; }
                            else { tempLFPVal = Int16.MinValue; }
                            lfpFile.read((short)tempLFPVal, i);
                        }
                }
                #endregion

                //Digital ref LFP signals
                if (!checkBox_digRefLFPs.Checked) { /* Do nothing, since prefetch makes if faster than else */ }
                else
                {
                    int refChan = Convert.ToInt16(numericUpDown_digRefLFPs.Value) - 1;
                    for (int i = 0; i < refChan; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] -= finalLFPData[refChan][j];
                    for (int i = refChan + 1; i < numChannels; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] -= finalLFPData[refChan][j];
                }

                //Post to PlotData buffer
                lfpPlotData.write(finalLFPData, taskNumber * numChannelsPerDev, numChannelsPerDev);
            }
            #endregion

            #region SALPA Filtering
            if (checkBox_SALPA.Checked)
                SALPAFilter.filter(ref filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev, thrSALPA, stimIndices, numStimReads[taskNumber] - 1);
            #endregion SALPA Filtering

            #region SpikeFiltering
            //Filter spike data
            if (checkBox_spikesFilter.Checked)
            {
                for (int i = numChannelsPerDev * taskNumber; i < numChannelsPerDev * (taskNumber + 1); ++i)
                    spikeFilter[i].filterData(filtSpikeData[i]);
            }
            #endregion

            //NEED TO FIX FOR MULTI DEVS
            #region Digital_Referencing_Spikes
            //Digital ref spikes signals
            if (checkBox_digRefSpikes.Checked)
            {
                int refChan = Convert.ToInt16(numericUpDown_digRefSpikes.Value) - 1;
                for (int i = 0; i < refChan; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                        filtSpikeData[i][j] -= filtSpikeData[refChan][j];
                for (int i = refChan + 1; i < numChannels; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                        filtSpikeData[i][j] -= filtSpikeData[refChan][j];
            }

            //Common average or median referencing
            if (referncer != null)
            {
                lock (this)
                    referncer.reference(filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev);
            }
            #endregion

            #region SpikeDetection
            ++(numSpikeReads[taskNumber]);

            SALPA_WIDTH = Convert.ToInt32(numericUpDown_salpa_halfwidth.Value);

            int startTime = (numSpikeReads[taskNumber] - 1) * spikeBufferLength; //Used to mark spike time for *.spk file
            if (checkBox_SALPA.Checked)
                startTime -= 2 * SALPA_WIDTH; //To account for delay of SALPA filter

            List<SpikeWaveform> newWaveforms = new List<SpikeWaveform>(100);
            for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                spikeDetector.detectSpikes(filtSpikeData[i], newWaveforms, i);

            #region SpikeValidation
            double Fs = Convert.ToDouble(textBox_spikeSamplingRate.Text);
            int numSamplesPeak = (int)Math.Ceiling(0.0005*Fs); //Search the first half millisecond after thresh crossing
            int numSamplesToSearch = 64;
            if ((numPre + numPost + 1) < numSamplesToSearch) numSamplesToSearch = (numPre + numPost + 1);

            if (checkBox_spikeValidation.Checked)
            {
                for (int w = 0; w < newWaveforms.Count; ++w) //For each waveform
                {
                    //Ensure that first and last few samples aren't blanked (this happens with artifact suppressions sometimes)
                    if ((newWaveforms[w].waveform[0] <= newWaveforms[w].waveform[1] + VOLTAGE_EPSILON && newWaveforms[w].waveform[0] >= newWaveforms[w].waveform[1] - VOLTAGE_EPSILON &&
                        newWaveforms[w].waveform[1] <= newWaveforms[w].waveform[2] + VOLTAGE_EPSILON && newWaveforms[w].waveform[1] >= newWaveforms[w].waveform[2] - VOLTAGE_EPSILON) ||
                        (newWaveforms[w].waveform[numPost + numPre] <= newWaveforms[w].waveform[numPost + numPre - 1] + VOLTAGE_EPSILON &&
                        newWaveforms[w].waveform[numPost + numPre] >= newWaveforms[w].waveform[numPost + numPre - 1] - VOLTAGE_EPSILON &&
                        newWaveforms[w].waveform[numPost + numPre - 1] <= newWaveforms[w].waveform[numPost + numPre - 2] + VOLTAGE_EPSILON &&
                        newWaveforms[w].waveform[numPost + numPre - 1] >= newWaveforms[w].waveform[numPost + numPre - 2] - VOLTAGE_EPSILON))
                    {
                        newWaveforms.RemoveAt(w);
                        --w;
                        continue;
                    }

                    //Find peak
                    double maxVal = 0.0;
                    for (int k = 0; k < numSamplesPeak; ++k)
                    {
                        if (Math.Abs(newWaveforms[w].waveform[k + numPre]) > maxVal)
                            maxVal = Math.Abs(newWaveforms[w].waveform[k + numPre]);
                    }
                    //Search pts. before and after for bigger, disqualifying if there are larger peaks
                    if (maxVal > 1e-6*Convert.ToDouble(textBox_AbsArtThresh.Text))
                        {
                            newWaveforms.RemoveAt(w);
                            --w;
                        }
                    else
                    {
                        for (int k = 0; k < numSamplesToSearch; ++k)
                        {

                            if (Math.Abs(newWaveforms[w].waveform[k]) > maxVal)
                            {
                                newWaveforms.RemoveAt(w);
                                --w;
                                break;
                            }
                      
                        }
                    }


                }
            }
            #endregion

            //Extract waveforms
            if (Properties.Settings.Default.ChannelMapping != "invitro" || numChannels != 64) //check this first, so we don't have to check it for each spike
            {
                for (int j = 0; j < newWaveforms.Count; ++j) //For each threshold crossing
                {
                    #region WriteSpikeWfmsToFile
                    rawType[] waveformData = newWaveforms[j].waveform;
                    if (switch_record.Value)
                    {
                        lock (fsSpks) //Lock so another NI card doesn't try writing at the same time
                        {
                            //fsSpks.Write(BitConverter.GetBytes((short)newWaveforms[j].channel), 0, 2); //Write channel num.
                            //fsSpks.Write(BitConverter.GetBytes(startTime + newWaveforms[j].index), 0, 4); //Write time (index number)
                            //for (int k = 0; k < numPre + numPost + 1; ++k)
                            //    fsSpks.Write(BitConverter.GetBytes(waveformData[k]), 0, 8); //Write value as double -- much easier than writing raw value, but takes more space
                            fsSpks.WriteSpikeToFile(newWaveforms[j].channel, startTime + newWaveforms[j].index,
                                newWaveforms[j].threshold, waveformData);
                        }
                    }
                    #endregion
                }
            }
            else //in vitro mappings
            {
                for (int j = 0; j < newWaveforms.Count; ++j) //For each threshold crossing
                {
                    #region WriteSpikeWfmsToFile
                    rawType[] waveformData = newWaveforms[j].waveform;
                    if (switch_record.Value)
                    {
                        lock (fsSpks) //Lock so another NI card doesn't try writing at the same time
                        {
                            //fsSpks.Write(BitConverter.GetBytes((short)newWaveforms[j].channel), 0, 2); //Write channel num.
                            //fsSpks.Write(BitConverter.GetBytes(MEAChannelMappings.channel2LinearCR(newWaveforms[j].channel)), 0, 2); //Write channel num.
                            //fsSpks.Write(BitConverter.GetBytes(startTime + newWaveforms[j].index), 0, 4); //Write time (index number)
                            //for (int k = 0; k < numPre + numPost + 1; ++k)
                            //    fsSpks.Write(BitConverter.GetBytes(waveformData[k]), 0, 8); //Write value as double -- much easier than writing raw value, but takes more space
                            fsSpks.WriteSpikeToFile(MEAChannelMappings.channel2LinearCR(newWaveforms[j].channel), startTime + newWaveforms[j].index,
                                newWaveforms[j].threshold, waveformData);
                        }
                    }
                    #endregion
                }
            }

            //Post to PlotData
            waveformPlotData.write(newWaveforms);

            #region WriteSpikeWfmsToListeningProcesses
            //Alert any listening processes that we have new spikes.  It's up to them to clear wavefroms periodically.
            //That's definitely not the best way to do it.
            if (spikesAcquired != null)
            {
                lock (this)
                {
                    //Check to see if spikes are within trigger
                    for (int i = 0; i < newWaveforms.Count; ++i)
                    {
                        if (newWaveforms[i].index + startTime >= triggerStartTime && newWaveforms[i].index + startTime <= triggerStopTime)
                        {
                            _waveforms.Add(newWaveforms[i]);
#if (DEBUG1)
                            logFile.WriteLine("Waveform in trigger, index: " + newWaveforms[i].index);
#endif
                        }
                    }
                }
                spikesAcquired(this, inTrigger);
            }
            #endregion

            //Clear new ones, since we're done with them.
            newWaveforms.Clear();
            #endregion

            #region BNC_Output
            //Send selected channel to BNC
            if (Properties.Settings.Default.UseSingleChannelPlayback)
            {
                int ch = (int)(channelOut.Value) - 1;
                if (ch >= numChannelsPerDev * taskNumber && ch < numChannelsPerDev * (taskNumber + 1))
                    //spikeOutWriter.BeginWriteMultiSample(true, filtSpikeData[ch], null, null);
                    BNCOutput.write(filtSpikeData[ch]);
                //spikeOutWriter.WriteMultiSample(true, filtSpikeData[ch - 1]);
            }
            #endregion

            //Write to PlotData buffer
            spikePlotData.write(filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev);

            #region MUA
            if (Properties.Settings.Default.ProcessMUA)
            {
                muaFilter.Filter(filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev, ref muaData);

                //Write to plot buffer
                muaPlotData.write(muaData, taskNumber * numChannelsPerDev, numChannelsPerDev);
            }
            #endregion

            e.Result = taskNumber;
        }

        internal delegate void spikesAcquiredHandler(object sender, bool inTrigger);
        internal event spikesAcquiredHandler spikesAcquired;


        //*************************
        //bwSpikes_RunWorkerCompleted
        //*************************
        private void bwSpikes_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int taskNumber = (int)e.Result;
            
            //Check whether timed recording is done
            if (!checkBox_enableTimedRecording.Checked || DateTime.Now < timedRecordingStopTime)
                //Setup next callback
                spikeReader[taskNumber].BeginMemoryOptimizedReadWaveform(spikeBufferLength, spikeCallback, taskNumber, spikeData[taskNumber]);
            else
                buttonStop.PerformClick();
        }

        //***********************
        //Callback for PlotData
        //***********************
        private short recordingLEDState = 0;
        private void spikePlotData_dataAcquired(object sender)
        {
            PlotData pd = (PlotData)sender;
            if (spikeGraph.Visible && !checkBox_freeze.Checked)
            {
                float[][] data = pd.read();
                for (int i = 0; i < data.Length; ++i)
                    spikeGraph.plotY(data[i], 0, 1, Microsoft.Xna.Framework.Graphics.Color.Lime, i);
                spikeGraph.Invalidate();
            }
            else { pd.skipRead(); }

            #region Recording_LED
            if (switch_record.Value)
            {
                //Toggle recording light
                if (recordingLEDState++ == 1)
                {
                    if (led_recording.OnColor == Color.Red)
                        led_recording.OnColor = Color.Lime;
                    else
                        led_recording.OnColor = Color.Red;
                }
                recordingLEDState %= 2;
            }
            #endregion
        }

        //*******************************
        //Callback for WaveformPlotData
        //******************************
        private void waveformPlotData_dataAcquired(object sender)
        {
            EventPlotData pd = (EventPlotData)sender;
            if (spkWfmGraph.Visible && !checkBox_freeze.Checked)
            {
                int maxWaveforms = pd.getMaxWaveforms();

                List<PlotSpikeWaveform> wfms = pd.read();
                for (int i = 0; i < wfms.Count; ++i)
                {
                    int channel = wfms[i].channel;
                    if (Properties.Settings.Default.ChannelMapping == "invitro")
                        channel = (MEAChannelMappings.ch2rc[channel, 0]-1) * 8 + MEAChannelMappings.ch2rc[channel, 1] - 1;
                    spkWfmGraph.plotY(wfms[i].waveform, pd.horizontalOffset(channel), 1, Microsoft.Xna.Framework.Graphics.Color.Lime,
                        numSpkWfms[channel]++ + channel * maxWaveforms);
                    numSpkWfms[channel] %= maxWaveforms;
                }
                spkWfmGraph.Invalidate();
            }
            else { pd.skipRead(); }
        }
        #endregion //End spike acquisition

        #region LFP_Acquisition
        /**************************************************
         * LFP Data acquisition, plotting                 *
         **************************************************/
        private void AnalogInCallback_LFPs(IAsyncResult ar)
        {
            try
            {
                if (taskRunning)
                {
                    //Read the available data from the channels
                    lfpData = lfpReader.EndReadInt16(ar);

                    //Write to file in format [numChannels numSamples]
                    #region WriteLFPFile
                    if (switch_record.Value)
                    {
                        lfpFile.read(lfpData, numChannels, 0, lfpBufferLength);
                        //for (int j = 0; j < lfpBufferLength; ++j)
                        //    for (int i = 0; i < numChannels; ++i)
                        //        fsLFPs.Write(BitConverter.GetBytes(lfpData[i, j]), 0, 2);
                    }
                    #endregion

                    //Convert to scaled double array
                    for (int i = 0; i < numChannels; ++i)
                    {
                        //filtLFPData[i] = new double[lfpBufferLength];
                        for (int j = 0; j < lfpBufferLength; ++j)
                        {
                            filtLFPData[i][j] = (rawType)lfpData[i, j] * (rawType)lfpData[i, j] * (rawType)lfpData[i, j] * (rawType)scalingCoeffsLFPs[3] +
                                (rawType)lfpData[i, j] * (rawType)lfpData[i, j] * (rawType)scalingCoeffsLFPs[2] +
                                (rawType)lfpData[i, j] * (rawType)scalingCoeffsLFPs[1] + (rawType)scalingCoeffsLFPs[0];
                        }
                    }

                    //Filter
                    if (checkBox_LFPsFilter.Checked)
                        for (int i = 0; i < numChannels; ++i)
                            lfpFilter[i].filterData(filtLFPData[i]);

                    //Post to PlotData buffer
                    lfpPlotData.write(filtLFPData, 0, numChannels);

                    //Setup next callback
                    lfpReader.BeginReadInt16(lfpBufferLength, lfpCallback, lfpReader);
                }
            }
            catch (DaqException exception)
            {
                //Display Errors
                MessageBox.Show(exception.Message);
                reset();
            }
        }

        //******************************
        //lfpPlotData_dataAcquired
        //******************************
        void lfpPlotData_dataAcquired(object sender)
        {
            //if (lfpGraph.InvokeRequired)
            //{
            //    lfpGraph.BeginInvoke(new plotData_dataAcquiredDelegate(lfpPlotData_dataAcquired), sender);
            //}
            //else
            //{
                PlotData pd = (PlotData)sender;
                if (lfpGraph.Visible && !checkBox_freeze.Checked)
                {
                    float[][] data = pd.read();
                    for (int i = 0; i < data.Length; ++i)
                        //lfpGraph.Plots.Item(i + 1).PlotY(data[i], (double)pd.downsample / (double)lfpSamplingRate,
                        //    (double)pd.downsample / (double)lfpSamplingRate);
                        lfpGraph.plotY(data[i], 0F, 1F, Microsoft.Xna.Framework.Graphics.Color.Lime, i);
                    lfpGraph.Invalidate();
                }
                else { pd.skipRead(); }
            //}
        }

        //******************************
        //muaPlotData_dataAcquired
        //******************************
        void muaPlotData_dataAcquired(object sender)
        {
            PlotData pd = (PlotData)sender;
            if (muaGraph.Visible && !checkBox_freeze.Checked)
            {
                float[][] data = pd.read();
                for (int i = 0; i < data.Length; ++i)
                    //lfpGraph.Plots.Item(i + 1).PlotY(data[i], (double)pd.downsample / (double)lfpSamplingRate,
                    //    (double)pd.downsample / (double)lfpSamplingRate);
                    muaGraph.plotY(data[i], 0F, 1F, Microsoft.Xna.Framework.Graphics.Color.Lime, i);
                muaGraph.Invalidate();
            }
            else { pd.skipRead(); }
        }

        #endregion  //End LFP acquisition

        #region EEG_Acquisition
        /**************************************************
         * EEG Data acquisition, plotting                 *
         **************************************************/
        private void AnalogInCallback_EEG(IAsyncResult ar)
        {
            try
            {
                if (taskRunning)
                {
                    //Read the available data from the channels
                    eegData = eegReader.EndReadInt16(ar);

                    double temp;
                    double rangeHigh = eegTask.AIChannels.All.RangeHigh;
                    double rangeLow = eegTask.AIChannels.All.RangeLow;

                    //Write to file in format [numChannels numSamples]
                    #region WriteEEGFile
                    if (switch_record.Value)
                    {
                        for (int j = 0; j < eegBufferLength; ++j)
                            for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                                fsEEG.Write(BitConverter.GetBytes(eegData[i, j]), 0, 2);
                    }
                    #endregion

                    //Convert to scaled double array
                    for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                    {
                        //filtLFPData[i] = new double[lfpBufferLength];
                        for (int j = 0; j < eegBufferLength; ++j)
                        {
                            filtEEGData[i][j] = (double)eegData[i, j] * (double)eegData[i, j] * (double)eegData[i, j] * scalingCoeffsEEG[3] +
                                (double)eegData[i, j] * (double)eegData[i, j] * scalingCoeffsEEG[2] +
                                (double)eegData[i, j] * scalingCoeffsEEG[1] + scalingCoeffsEEG[0];
                        }
                    }

                    //Filter
                    if (checkBox_eegFilter.Checked)
                        for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                            filtEEGData[i] = eegFilter[i].FilterData(filtEEGData[i]);

                    //Stacked plot (if the LFP tab is selected)
                    int jMax = eegPlotData.GetLength(1) - eegBufferLength / eegDownsample;
                    for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)  //for each channel
                    {
                        //first, move old data down in array
                        for (int j = 0; j < jMax; ++j)
                            eegPlotData[i, j] = eegPlotData[i, j + eegBufferLength / eegDownsample];

                        //now, scale new data by stacking offset; add to end of array
                        //double offset = 2 * i * lfpTask.AIChannels.All.RangeHigh;
                        for (int j = jMax; j < eegPlotData.GetLength(1); ++j)
                        {
                            //lfpPlotData[i, j] = finalLFPData[i][(j - jMax) * lfpDownsample] * lfpDisplayGain - lfpOffset[i];
                            temp = filtEEGData[i][(j - jMax) * eegDownsample] * eegDisplayGain;
                            if (temp > rangeHigh)
                                temp = rangeHigh;
                            else if (temp < rangeLow)
                                temp = rangeLow;
                            eegPlotData[i, j] = temp - eegOffset[i];
                        }
                    }
                    if (tabControl.SelectedTab.Text == "EEG" && !checkBox_freeze.Checked)
                    {
                        //if (plotLFP)
                        //{
                            //lfpGraph.PlotYMultiple(lfpPlotData, DataOrientation.DataInRows, (double)lfpDownsample / (double)lfpSamplingRate, (double)lfpDownsample / (double)lfpSamplingRate);
                            eegGraph.PlotY(eegPlotData, (double)eegDownsample / (double)eegSamplingRate, (double)eegDownsample / (double)eegSamplingRate, true);
                        //    plotLFP = false;
                        //}
                        //else
                        //    plotLFP = true;
                    }

                    //Setup next callback
                    eegReader.BeginReadInt16(eegBufferLength, eegCallback, eegReader);
                }
            }
            catch (DaqException exception)
            {
                //Display Errors
                MessageBox.Show(exception.Message);
                reset();
            }
        }

        private void setupEEGOffset()
        {
            eegOffset = new double[Convert.ToInt32(comboBox_eegNumChannels.SelectedItem)];
            double rangeHigh;
            rangeHigh = eegTask.AIChannels.All.RangeHigh;
            for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                eegOffset[i] = 2 * i * rangeHigh;
        }
        #endregion

        #region Caretaking_Routines
        /************************************
         * Caretaking Routines
         ************************************/

        private void buttonStop_Click(object sender, EventArgs e) 
        { 
            if (taskRunning) reset();
#if (USE_LOG_FILE)
        logFile.Flush();
    logFile.Close();
    logFile.Dispose();
#endif
        }

        private void NeuroControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (taskRunning) { reset(); }

            if (Properties.Settings.Default.UseStimulator)
            {
                //Deal with IvsV
                stimIvsVTask = new Task("stimIvsV");
                //stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port0/line8:15", "",
                //    ChannelLineGrouping.OneChannelForAllLines);
                stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                    ChannelLineGrouping.OneChannelForAllLines);
                stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                //stimIvsVTask.Timing.ConfigureSampleClock("100kHztimebase", 100000,
                //    SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimIvsVTask.Control(TaskAction.Verify);
                //byte[] b_array = new byte[5] { 0, 0, 0, 0, 0 };
                //DigitalWaveform wfm = new DigitalWaveform(5, 8, DigitalState.ForceDown);
                //wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                //stimIvsVWriter.WriteWaveform(true, wfm);
                stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
                stimIvsVTask.WaitUntilDone();
                stimIvsVTask.Stop();
                stimIvsVTask.Dispose();

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
            }

            if (spikeGraph != null) { spikeGraph.Dispose(); spikeGraph = null; }
            if (lfpGraph != null) { lfpGraph.Dispose(); lfpGraph = null; }
            if (spkWfmGraph != null) { spkWfmGraph.Dispose(); spkWfmGraph = null; }

            //Save gain settings
            Properties.Settings.Default.Gain = comboBox_SpikeGain.SelectedIndex;
            
            //Save referencing scheme settings
            if (radioButton_spikesReferencingCommonAverage.Checked)
                Properties.Settings.Default.SpikesReferencingScheme = 0;
            else if (radioButton_spikesReferencingCommonMedian.Checked)
                Properties.Settings.Default.SpikesReferencingScheme = 1;
            else if (radioButton_spikesReferencingCommonMedianLocal.Checked)
                Properties.Settings.Default.SpikesReferencingScheme = 2;
            else if (radioButton_spikeReferencingNone.Checked)
                Properties.Settings.Default.SpikesReferencingScheme = 3;

            //Save filter cut-offs and poles
            Properties.Settings.Default.SpikesLowCut = Convert.ToDouble(SpikeLowCut.Value);
            Properties.Settings.Default.SpikesHighCut = Convert.ToDouble(SpikeHighCut.Value);
            Properties.Settings.Default.SpikesNumPoles = Convert.ToUInt16(SpikeFiltOrder.Value);
            Properties.Settings.Default.LFPLowCut = Convert.ToDouble(LFPLowCut.Value);
            Properties.Settings.Default.LFPHighCut = Convert.ToDouble(LFPHighCut.Value);
            Properties.Settings.Default.LFPNumPoles = Convert.ToUInt16(LFPFiltOrder.Value);

            //Save EEG settings
            if (Properties.Settings.Default.UseEEG)
            {
                Properties.Settings.Default.EEGGain = comboBox_eegGain.SelectedIndex;
                Properties.Settings.Default.EEGNumChannels = Convert.ToInt32(comboBox_eegNumChannels.SelectedItem);
                Properties.Settings.Default.EEGSamplingRate = Convert.ToInt32(textBox_eegSamplingRate.Text);
            }
            

            //Save 
            Properties.Settings.Default.Save();
        }

        //Set gain for channels
        private void setGain(Task myTask, ComboBox cb)
        {
            for (int i = 0; i < myTask.AIChannels.Count; ++i)
            {
                myTask.AIChannels[i].RangeHigh = 10.0 / Convert.ToDouble(cb.SelectedItem);
                myTask.AIChannels[i].RangeLow = -10.0 / Convert.ToDouble(cb.SelectedItem);

                myTask.AIChannels[i].Maximum = 10.0 / Convert.ToDouble(cb.SelectedItem);
                myTask.AIChannels[i].Minimum = -10.0 / Convert.ToDouble(cb.SelectedItem);
            }
        }

        //Called after data acq. is complete, resets buttons and stops tasks.
        private void reset()
        {
            //Grab display gains for later use
            Properties.Settings.Default.SpikeDisplayGain = spikePlotData.getGain();
            if (Properties.Settings.Default.UseLFPs)
                Properties.Settings.Default.LFPDisplayGain = lfpPlotData.getGain();
            Properties.Settings.Default.SpkWfmDisplayGain = waveformPlotData.getGain();

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
            if (spikeTask != null)
            {
                if (bwSpikes != null)
                {
                    for (int i = 0; i < bwSpikes.Count; ++i)
                        while (bwSpikes[i].IsBusy) { Application.DoEvents(); }//block while bw finishes

                    //All the bw workers are done, so we'll kill them
                    bwSpikes.Clear();
                    bwSpikes = null;
                }
                for (int i = 0; i < spikeTask.Count; ++i)
                    spikeTask[i].Dispose();
                spikeTask.Clear();
                spikeTask = null;
            }
            if (waveformPlotData != null) waveformPlotData.stop();
            if (Properties.Settings.Default.SeparateLFPBoard && lfpTask != null) lfpTask.Dispose();
            if (Properties.Settings.Default.UseEEG && eegTask != null) eegTask.Dispose();
            //if (spikeOutTask != null) spikeOutTask.Dispose();
            if (BNCOutput != null) { BNCOutput.Dispose(); BNCOutput = null; }
            if (stimTimeTask != null) stimTimeTask.Dispose();
            if (triggerTask != null) triggerTask.Dispose();


            buttonStop.Enabled = false;
            buttonStart.Enabled = true;
            comboBox_numChannels.Enabled = true;
            comboBox_SpikeGain.Enabled = true;
            numPreSamples.Enabled = true;
            numPostSamples.Enabled = true;
            settingsToolStripMenuItem.Enabled = true;
            comboBox_SpikeGain.Enabled = true;
            button_Train.Enabled = true;
            checkBox_SaveRawSpikes.Enabled = true;
            switch_record.Enabled = true;
            processingSettingsToolStripMenuItem.Enabled = true;
            textBox_spikeSamplingRate.Enabled = true;
            textBox_lfpSamplingRate.Enabled = true;
            textBox_MUASamplingRate.Enabled = true;

            if (Properties.Settings.Default.UseEEG)
            {
                comboBox_eegNumChannels.Enabled = true;
                comboBox_eegGain.Enabled = true;
                textBox_eegSamplingRate.Enabled = true;
            }
            if (Properties.Settings.Default.SeparateLFPBoard)  comboBox_LFPGain.Enabled = true;
            if (rawFile != null) { rawFile.flush(); rawFile = null; }
            if (lfpFile != null) { lfpFile.flush(); lfpFile = null; }
            if (fsSpks != null) fsSpks.Close();
            if (fsStim != null) fsStim.Close();
            if (fsEEG != null) fsEEG.Close();
            if (triggerWriter != null) triggerWriter = null;
            channelOut.Enabled = Properties.Settings.Default.UseSingleChannelPlayback;

            led_recording.OnColor = Color.Lime;

            timer_timeElapsed.Enabled = false;
        }

        #region FilterControls
        private void SpikeLowCut_ValueChanged(object sender, EventArgs e)
        {
            resetSpikeFilter();
        }
        private void checkBox_spikesFilter_CheckedChanged(object sender, EventArgs e)
        {
            resetSpikeFilter();
        }
        private void SpikeHighCut_ValueChanged(object sender, EventArgs e)
        {
            resetSpikeFilter();
        }
        private void checkBox_LFPsFilter_CheckedChanged(object sender, EventArgs e)
        {
            resetLFPFilter();
        }
        private void LFPLowCut_ValueChanged(object sender, EventArgs e)
        {
            resetLFPFilter();
        }
        private void LFPHighCut_ValueChanged(object sender, EventArgs e)
        {
            resetLFPFilter();
        }
        private void SpikeFiltOrder_ValueChanged(object sender, EventArgs e)
        {
            resetSpikeFilter();
        }
        private void LFPFiltOrder_ValueChanged(object sender, EventArgs e)
        {
            resetLFPFilter();
        }
        private void EEGLowCut_ValueChanged(object sender, EventArgs e)
        {
            resetEEGFilter();
        }
        private void EEGHighCut_ValueChanged(object sender, EventArgs e)
        {
            resetEEGFilter();
        }
        private void EEGFiltOrder_ValueChanged(object sender, EventArgs e)
        {
            resetEEGFilter();
        }
        private void checkBox_eegFilter_CheckedChanged(object sender, EventArgs e)
        {
            resetEEGFilter();
        }

        //Setup spike filters
        private void resetSpikeFilter()
        {
            if (spikeFilter != null)
            {
                lock (spikeFilter)
                {
                    for (int i = 0; i < spikeFilter.Length; ++i)
                        spikeFilter[i].Reset((int)SpikeFiltOrder.Value, Convert.ToDouble(textBox_spikeSamplingRate.Text),
                            Convert.ToDouble(SpikeLowCut.Value), Convert.ToDouble(SpikeHighCut.Value), spikeBufferLength);
                }
            }
            else //spikeFilter is uninitialized
            {
                //spikeFilter = new BesselBandpassFilter[numChannels];
                spikeFilter = new ButterworthFilter[numChannels];
                for (int i = 0; i < numChannels; ++i)
                    //spikeFilter[i] = new ButterworthBandpassFilter((int)SpikeFiltOrder.Value, Convert.ToDouble(textBox_spikeSamplingRate.Text),
                    //    Convert.ToDouble(SpikeLowCut.Value), Convert.ToDouble(SpikeHighCut.Value));
                    spikeFilter[i] = new ButterworthFilter((int)SpikeFiltOrder.Value, Convert.ToDouble(textBox_spikeSamplingRate.Text),
                        Convert.ToDouble(SpikeLowCut.Value), Convert.ToDouble(SpikeHighCut.Value), spikeBufferLength);
            }
        }

        //Setup LFP filters
        private void resetLFPFilter()
        {
            rawType Fs;
            if (Properties.Settings.Default.SeparateLFPBoard)
                Fs = Convert.ToDouble(textBox_lfpSamplingRate.Text);
            else
                Fs = Convert.ToDouble(textBox_spikeSamplingRate.Text);

            if (lfpFilter != null)
            {
                lock (lfpFilter)
                {
                    for (int i = 0; i < lfpFilter.Length; ++i)
                        //lfpFilter[i].Reset((int)LFPFiltOrder.Value, Fs,
                        //    Convert.ToDouble(LFPLowCut.Value), Convert.ToDouble(LFPHighCut.Value));
                        if (Properties.Settings.Default.SeparateLFPBoard)
                            lfpFilter[i].Reset((int)LFPFiltOrder.Value, Fs,
                                Convert.ToDouble(LFPLowCut.Value), Convert.ToDouble(LFPHighCut.Value), lfpBufferLength);
                        else
                            lfpFilter[i].Reset((int)LFPFiltOrder.Value, Fs,
                                Convert.ToDouble(LFPLowCut.Value), Convert.ToDouble(LFPHighCut.Value), spikeBufferLength);
                }
            }
            else //lfpFilter is uninitialized
            {
                //lfpFilter = new BesselBandpassFilter[numChannels];
                lfpFilter = new ButterworthFilter[numChannels];
                for (int i = 0; i < numChannels; ++i)
                    //lfpFilter[i] = new BesselBandpassFilter((int)LFPFiltOrder.Value, Convert.ToDouble(textBox_lfpSamplingRate.Text),
                    //    Convert.ToDouble(LFPLowCut.Value), Convert.ToDouble(LFPHighCut.Value));
                if (Properties.Settings.Default.SeparateLFPBoard)
                    lfpFilter[i] = new ButterworthFilter((int)LFPFiltOrder.Value, Convert.ToDouble(textBox_lfpSamplingRate.Text),
                        Convert.ToDouble(LFPLowCut.Value), Convert.ToDouble(LFPHighCut.Value), lfpBufferLength);
                else
                    lfpFilter[i] = new ButterworthFilter((int)LFPFiltOrder.Value, Convert.ToDouble(textBox_lfpSamplingRate.Text),
                        Convert.ToDouble(LFPLowCut.Value), Convert.ToDouble(LFPHighCut.Value), spikeBufferLength);
            }
        }
        private void resetEEGFilter()
        {
            if (eegFilter != null)
            {
                lock (eegFilter)
                {
                    for (int i = 0; i < eegFilter.Length; ++i)
                        eegFilter[i].Reset((int)EEGFiltOrder.Value, Convert.ToDouble(textBox_eegSamplingRate.Text),
                            Convert.ToDouble(EEGLowCut.Value), Convert.ToDouble(EEGHighCut.Value));
                }
            }
            else //eegFilter is uninitialized
            {
                //eegFilter = new ButterworthBandpassFilter[16];
                eegFilter = new BesselBandpassFilter[Convert.ToInt32(comboBox_eegNumChannels.SelectedItem)];
                for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                    //eegFilter[i] = new ButterworthBandpassFilter((int)EEGFiltOrder.Value, Convert.ToDouble(textBox_eegSamplingRate.Text),
                    //    Convert.ToDouble(EEGLowCut.Value), Convert.ToDouble(EEGHighCut.Value));
                    eegFilter[i] = new BesselBandpassFilter((int)EEGFiltOrder.Value, Convert.ToDouble(textBox_eegSamplingRate.Text),
                        Convert.ToDouble(EEGLowCut.Value), Convert.ToDouble(EEGHighCut.Value));
            }
        }
        #endregion

        private void comboBox_numChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            numChannels = Convert.ToInt32(comboBox_numChannels.SelectedItem);
            Properties.Settings.Default.DefaultNumChannels = Convert.ToString(numChannels);
            Properties.Settings.Default.Save();
            numChannelsPerDev = (numChannels < 32 ? numChannels : 32);
            spikeFilter = null;
            lfpFilter = null;
            resetSpikeFilter();
            resetLFPFilter();
            thrSALPA = null;
            label_noise.Text = "Noise levels have not been trained.";
            label_noise.ForeColor = Color.Red;
            checkBox_SALPA.Enabled = false;
            checkBox_SALPA.Checked = false;

            //Add more available stim channels
            stimChannel.Maximum = Convert.ToInt32(comboBox_numChannels.SelectedItem);
            numericUpDown_impChannel.Maximum = Convert.ToInt32(comboBox_numChannels.SelectedItem);
            listBox_stimChannels.Items.Clear();
            listBox_exptStimChannels.Items.Clear();
            listBox_closedLoopLearningProbeElectrodes.Items.Clear();
            listBox_closedLoopLearningCPSElectrodes.Items.Clear();
            listBox_closedLoopLearningPTSElectrodes.Items.Clear();
            for (int i = 0; i < Convert.ToInt32(comboBox_numChannels.SelectedItem); ++i)
            {
                listBox_stimChannels.Items.Add(i + 1);
                listBox_closedLoopLearningCPSElectrodes.Items.Add(i + 1);
                listBox_exptStimChannels.Items.Add(i + 1);
                listBox_closedLoopLearningProbeElectrodes.Items.Add(i + 1);
                listBox_closedLoopLearningPTSElectrodes.Items.Add(i + 1);
            }

            //Ensure that sampling rates are okay
            button_lfpSamplingRate_Click(null, null);
            button_spikeSamplingRate_Click(null, null);
            setSpikeDetector();

            //if (radioButton_spikeReferencingNone.Checked)
            //    referncer = null;
            //else if (radioButton_spikesReferencingCommonAverage.Checked)
            //    referncer = new Filters.CommonAverageReferencer(spikeBufferLength);
            //else if (radioButton_spikesReferencingCommonMedian.Checked)
            //    referncer = new Filters.CommonMedianReferencer(spikeBufferLength, numChannels);
            //else if (radioButton_spikesReferencingCommonMedianLocal.Checked)
            //{
            //    int channelsPerGroup = Convert.ToInt32(numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value);
            //    referncer = new Filters.CommonMedianLocalReferencer(spikeBufferLength, channelsPerGroup, numChannels / channelsPerGroup);
            //}
            resetReferencers();
        }

        //******************************
        //Reset spike waveform graphs
        //******************************
        private void resetSpkWfm()
        {
            numSpkWfms = new int[numChannels];
            for (int i = 0; i < numSpkWfms.Length; ++i)
                numSpkWfms[i] = 0; //Set to 1

            int numCols, numRows;
            switch (Convert.ToInt32(comboBox_numChannels.SelectedItem))
            {
                case 16:
                    numRows = numCols = 4; break;
                case 32:
                    numRows = numCols = 6; break;
                case 64:
                    numRows = numCols = 8; break;
                default:
                    numRows = numCols = 4; break;
            }
            if (spkWfmGraph != null) { spkWfmGraph.Dispose(); spkWfmGraph = null; }
            spkWfmGraph = new GridGraph();
            if (spikeTask != null && spikeTask[0] != null)
                spkWfmGraph.setup(numRows, numCols, numPre + numPost + 1, true, (double)(numPre + numPost + 1) / spikeSamplingRate, spikeTask[0].AIChannels.All.RangeHigh * 2.0);
            else
            {
                double gain = 20.0 / Convert.ToInt32(comboBox_SpikeGain.SelectedItem);
                spkWfmGraph.setup(numRows, numCols, numPre + numPost + 1, true, (double)(numPre + numPost + 1) / spikeSamplingRate, gain);
            }
            //spkWfmGraph.Resize += new EventHandler(spkWfmGraph.resize);
            //spkWfmGraph.SizeChanged += new EventHandler(spkWfmGraph.resize);
            //spkWfmGraph.VisibleChanged += new EventHandler(spkWfmGraph.resize);
            spkWfmGraph.Parent = tabPage_waveforms;
            spkWfmGraph.BringToFront();
            spkWfmGraph.Dock = DockStyle.Fill;

            //Adjust ranges
            if (spikeTask != null && spikeTask[0] != null)
            {
                double wfmLength = numPre + numPost + 1;
                spkWfmGraph.setMinMax(1, (float)(numCols * wfmLength), (float)(spikeTask[0].AIChannels.All.RangeLow * (numRows * 2 - 1)), (float)(spikeTask[0].AIChannels.All.RangeHigh));
            }
            //spkWfmGraph.clear();
        }
        private void resetReferencers()
{
           if (radioButton_spikeReferencingNone.Checked)
                referncer = null;
           else if (radioButton_spikesReferencingCommonAverage.Checked)
           {
                referncer = new
                Filters.CommonAverageReferencer(spikeBufferLength);
           }
           else if (radioButton_spikesReferencingCommonMedian.Checked)
           {
                referncer = new
                Filters.CommonMedianReferencer(spikeBufferLength, numChannels);
           }
           else if (radioButton_spikesReferencingCommonMedianLocal.Checked)
           {
                int channelsPerGroup =
                Convert.ToInt32(numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value);
                referncer = new
                Filters.CommonMedianLocalReferencer(spikeBufferLength, channelsPerGroup, numChannels / channelsPerGroup);
           }
       }


        #region The region resets the SALPA parameters in real time when the user changes them


        private void numericUpDown_salpa_halfwidth_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_postpeg_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_postpegzeros_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_delta_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void checkBox_SALPA_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SALPA.Checked)
            {
                numericUpDown_salpa_halfwidth.Enabled = false;
                numericUpDown_salpa_postpeg.Enabled = false;
                numericUpDown_salpa_postpegzeros.Enabled = false;
                numericUpDown_salpa_delta.Enabled = false;
                resetSALPA();
            }
            else
            {
                numericUpDown_salpa_halfwidth.Enabled = true;
                numericUpDown_salpa_postpeg.Enabled = true;
                numericUpDown_salpa_postpegzeros.Enabled = true;
                numericUpDown_salpa_delta.Enabled = true;
            }
        }

        private void resetSALPA()
        {
            // Set SALPA parameters
            double prepegSeconds = 0.0002;
            double postpegSeconds = Convert.ToDouble(numericUpDown_salpa_postpeg.Value);
            double postpegZeroSeconds = Convert.ToDouble(numericUpDown_salpa_postpegzeros.Value); //0.0002 = 5 samples @ 25 kHz
            double delta = Convert.ToDouble(numericUpDown_salpa_delta.Value);
            SALPA_WIDTH = Convert.ToInt32(numericUpDown_salpa_halfwidth.Value);

            if (4 * SALPA_WIDTH + 1 > spikeBufferLength) // Make sure that the number of samples needed for polynomial fit is not more than the current buffersize
            {
                double max_halfwidth =  Convert.ToDouble((spikeBufferLength - 1) / 4);
                SALPA_WIDTH = (int)Math.Floor(max_halfwidth);
            }

            int prepeg = (int)Math.Round(prepegSeconds * spikeSamplingRate);
            int postpeg = (int)Math.Round(postpegSeconds * spikeSamplingRate);
            int postpegzero = (int)Math.Round(postpegZeroSeconds * spikeSamplingRate);
            
            SALPAFilter = new SALPA2(SALPA_WIDTH, prepeg, postpeg, postpegzero, (rawType)(-10 / Convert.ToDouble(comboBox_SpikeGain.SelectedItem) + 0.01),
                (rawType)(10 / Convert.ToDouble(comboBox_SpikeGain.SelectedItem) - 0.01), numChannels, delta, spikeBufferLength);
        }

        #endregion

        private void comboBox_SpikeGain_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBox_SALPA.Checked = checkBox_SALPA.Enabled = false;
            label_noise.Text = "Noise levels have not been trained.";
            label_noise.ForeColor = Color.Red;
        }

        private void comboBox_spikeDetAlg_SelectedIndexChanged(object sender, EventArgs e) { setSpikeDetector(); }

        /***************************************************************************
        * Check sampling rates for hardware capabilities                          *
        * *************************************************************************/
        private void button_lfpSamplingRate_Click(object sender, EventArgs e)
        {
            try
            {
                int maxFs;

                numChannels = Convert.ToInt32(comboBox_numChannels.SelectedItem);
                maxFs = 1000000 / numChannels; //Valid for PCI-6259, not sure about other cards

                int fs = Convert.ToInt32(textBox_lfpSamplingRate.Text);
                if (fs < 1)
                    textBox_lfpSamplingRate.Text = "4";
                if (fs > 1000000 / numChannels)
                    textBox_lfpSamplingRate.Text = maxFs.ToString();

            }
            catch  //This should happen if the user enters something inane
            {
                textBox_lfpSamplingRate.Text = "1000"; //Set to default of 1kHz
            }
        }

        private void button_spikeSamplingRate_Click(object sender, EventArgs e)
        {
            try
            {
                int numChannelsPerDevice = (numChannels > 32 ? 32 : numChannels);
                int maxFs = 1000000 / numChannelsPerDevice; //Valid for PCI-6259, not sure about other cards

                int fs = Convert.ToInt32(textBox_spikeSamplingRate.Text);
                if (fs * DEVICE_REFRESH <= numPost + numPre + 1)
                {
                    int fsmin = (int)Math.Ceiling((numPost + numPre + 1) / DEVICE_REFRESH);
                    textBox_spikeSamplingRate.Text = Convert.ToString(fsmin);
                    fs = fsmin;
                }
                if (fs > 1000000 / numChannelsPerDevice)
                {
                    textBox_spikeSamplingRate.Text = maxFs.ToString();
                    fs = maxFs;
                }

                spikeSamplingRate = fs;

            }
            catch  //This should happen if the user enters something inane
            {
                textBox_spikeSamplingRate.Text = "25000"; //Set to default of 1kHz
            }

            spikeBufferLength = Convert.ToInt32(DEVICE_REFRESH * Convert.ToDouble(textBox_spikeSamplingRate.Text));
            resetReferencers();
            setSpikeDetector();
        }

        private void button_scaleDown_Click(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            {
                case "Spikes":
                    spikePlotData.setGain(spikePlotData.getGain() * 0.5F);
                    spikeGraph.setDisplayGain(spikePlotData.getGain());
                    break;
                case "Spk Wfms":
                    waveformPlotData.setGain(waveformPlotData.getGain() / 2);
                    spkWfmGraph.setDisplayGain(waveformPlotData.getGain());
                    break;
                case "LFPs":
                    lfpPlotData.setGain(lfpPlotData.getGain() * 0.5F);
                    lfpGraph.setDisplayGain(lfpPlotData.getGain());
                    break;
                case "MUA":
                    muaPlotData.setGain(muaPlotData.getGain() * 0.5F);
                    muaGraph.setDisplayGain(muaPlotData.getGain());
                    break;
                case "EEG":
                    eegDisplayGain /= 2;
                    break;
                default:
                    break;
                //do nothing
            }
        }
        private void button_scaleUp_Click(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            {
                case "Spikes":
                    spikePlotData.setGain(spikePlotData.getGain() * 2F);
                    spikeGraph.setDisplayGain(spikePlotData.getGain());
                    break;
                case "Spk Wfms":
                    waveformPlotData.setGain(waveformPlotData.getGain() * 2F);
                    spkWfmGraph.setDisplayGain(waveformPlotData.getGain());
                    break;
                case "LFPs":
                    lfpPlotData.setGain(lfpPlotData.getGain() * 2F);
                    lfpGraph.setDisplayGain(lfpPlotData.getGain());
                    break;
                case "MUA":
                    muaPlotData.setGain(muaPlotData.getGain() * 2F);
                    muaGraph.setDisplayGain(muaPlotData.getGain());
                    break;
                case "EEG":
                    eegDisplayGain *= 2;
                    break;
                default:
                    break;
                //do nothing
            }
        }

        private void button_scaleReset_Click(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            //switch (tabControl.SelectedIndex)
            {
                //case 0:
                case "Spikes":
                    spikePlotData.setGain(1F);
                    spikeGraph.setDisplayGain(spikePlotData.getGain());
                    break;
                case "Spk Wfms":
                    waveformPlotData.setGain(1F);
                    spkWfmGraph.setDisplayGain(waveformPlotData.getGain());
                    break;
                case "LFPs":
                    lfpPlotData.setGain(1F);
                    lfpGraph.setDisplayGain(lfpPlotData.getGain());
                    break;
                case "MUA":
                    muaPlotData.setGain(1F);
                    muaGraph.setDisplayGain(muaPlotData.getGain());
                    break;
                case "EEG":
                    eegDisplayGain = 1;
                    break;
                default:
                    break;
                //do nothing
            }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HardwareSettings nc_s = new HardwareSettings();
            nc_s.ShowDialog();
            updateSettings();
        }

        //******************************
        //updateSettings
        //******************************
        private void updateSettings()
        {
            try {
                if (spikeTask != null)
                {
                    for (int i = 0; i < spikeTask.Count; ++i)
                        spikeTask[i].Dispose();
                    spikeTask.Clear();  spikeTask = null;
                }
                if (stimTimeTask != null) { stimTimeTask.Dispose();  stimTimeTask = null; }
                if (stimPulseTask != null) { stimPulseTask.Dispose(); stimPulseTask = null; }
                if (stimDigitalTask != null) { stimDigitalTask.Dispose(); stimDigitalTask = null; }
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
                            //stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port0/line8:15", "",
                            //    ChannelLineGrouping.OneChannelForAllLines);
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
                else if (!Properties.Settings.Default.ProcessMUA && tabControl.TabPages.Contains(tabPage_MUA)) tabControl.TabPages.Remove(tabPage_MUA);
            }
            catch (DaqException exception)
            {
                MessageBox.Show(exception.Message); //Display Errors
                reset();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reset();
            if (Properties.Settings.Default.UseStimulator)
            {
                stimIvsVTask = new Task("stimIvsV");
                //stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port0/line8:15", "",
                //    ChannelLineGrouping.OneChannelForAllLines);
                stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                    ChannelLineGrouping.OneChannelForAllLines);
                stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                //stimIvsVTask.Timing.ConfigureSampleClock("100kHztimebase", 100000,
                //    SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                stimIvsVTask.Control(TaskAction.Verify);
                //byte[] b_array = new byte[5] { 0, 0, 0, 0, 0 };
                //DigitalWaveform wfm = new DigitalWaveform(5, 8, DigitalState.ForceDown);
                //wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                //stimIvsVWriter.WriteWaveform(true, wfm);
                stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
                stimIvsVTask.WaitUntilDone();
                stimIvsVTask.Stop();
                stimIvsVTask.Dispose();
            }

            if (spikeGraph != null) { spikeGraph.Dispose(); spikeGraph = null; }
            if (lfpGraph != null) { lfpGraph.Dispose(); lfpGraph = null; }
            if (spkWfmGraph != null) { spkWfmGraph.Dispose(); spkWfmGraph = null; }

            this.Close();
        }

        private void setSpikeDetector()
        {
            switch (comboBox_spikeDetAlg.SelectedIndex)
            {
                case 0:  //RMS Adaptive
                    spikeDetector = new RMSThreshold(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost, 
                        numPre, Convert.ToDouble(thresholdMultiplier.Value), DEVICE_REFRESH);
                    break;
                case 1:  //Improved RMS Adaptive
                    spikeDetector = new SpikeDetection.StimSafeAdaptiveRMS(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost, numPre,
                        Convert.ToDouble(thresholdMultiplier.Value), DEVICE_REFRESH);
                    break;
                case 2:  //RMS Fixed
                    spikeDetector = new RMSThresholdFixed(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost, numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value));
                    break;
                case 3:  //Median method
                    spikeDetector = new MedianThreshold(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost, 
                        numPre, Convert.ToDouble(thresholdMultiplier.Value), DEVICE_REFRESH, spikeSamplingRate);
                    break;
                case 4:  //Improved Median
                    spikeDetector = new SpikeDetection.StimSafeMedian(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost, numPre,
                        (double)thresholdMultiplier.Value, DEVICE_REFRESH, spikeSamplingRate);
                    break;
                case 5:  //LimAda
                    spikeDetector = new LimAda(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost, numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), Convert.ToInt32(textBox_spikeSamplingRate.Text));
                    break;
                default:
                    break;
            }
        }

        private void thresholdMultiplier_ValueChanged(object sender, EventArgs e)
        {
            spikeDetector.thresholdMultiplier = (rawType)Convert.ToDouble(thresholdMultiplier.Value);
        }

        private void numPostSamples_ValueChanged(object sender, EventArgs e)
        {
            numPost = Convert.ToInt32(numPostSamples.Value);
        }

        private void button_clearSpkWfms_Click(object sender, EventArgs e)
        {
            spkWfmGraph.clear();
        }

        private void switch_record_StateChanged(object sender, ActionEventArgs e)
        {
            if (switch_record.Value)
                led_recording.Value = true;
            else
                led_recording.Value = false;
        }

        //Compute the RMS of an array.  Use this rather than a stock method, since it has no error checking and is faster.  Error checking is for pansies!
        internal static double rootMeanSquared(double[] data)
        {
            double rms = 0;
            for (int i = 0; i < data.Length; ++i)
                rms += data[i] * data[i];
            rms /= data.Length;
            return Math.Sqrt(rms);
        }

        private void toolStripMenuItem_DisplaySettings_Click(object sender, EventArgs e)
        {
            DisplaySettings ds = new DisplaySettings();
            ds.ShowDialog();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            //switch (tabControl.SelectedIndex)
            {
                //case 0: //Spike graph
                case "Spikes":
                    spikeGraph.Visible = true;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                //case 1: //Waveform graph
                case "Spk Wfms":
                    spkWfmGraph.Visible = true;
                    spikeGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                //case 2: //LFP Graph
                case "LFPs":
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                //case 3: //EEG Graph
                case "EEG":
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    eegGraph.Visible = true;
                    break;
                case "MUA":
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    muaGraph.Visible = true;
                    break;
                default:
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    break;
            }
        }

        /*******************************************************
         * End caretaking routines                             *
         *******************************************************/
        #endregion 
        private void button_Train_Click(object sender, EventArgs e)
        {
            thrSALPA = new rawType[Convert.ToInt32(comboBox_numChannels.SelectedItem)];
            spikeSamplingRate = Convert.ToInt32(textBox_spikeSamplingRate.Text);

            this.Cursor = Cursors.WaitCursor;

            label_noise.Text = "Noise levels have not been trained.";
            label_noise.ForeColor = Color.Red;
            label_noise.Update();
            buttonStart.Enabled = false;  //So users can't try to get data from the same card
            int numChannelsPerDevice = (numChannels > 32 ? 32 : numChannels);
            int numDevices = (numChannels > 32 ? Properties.Settings.Default.AnalogInDevice.Count : 1);
            spikeTask = new List<Task>(numDevices);
            for (int i = 0; i < numDevices; ++i)
            {
                spikeTask.Add(new Task("SALPATrainingTask_" + i));
                for (int j = 0; j < numChannelsPerDevice; ++j)
                {
                    spikeTask[i].AIChannels.CreateVoltageChannel(Properties.Settings.Default.AnalogInDevice[i] + "/ai" + j.ToString(), "",
                        AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                }
            }

            //Change gain based on comboBox values (1-100)
            for (int i = 0; i < spikeTask.Count; ++i)
                setGain(spikeTask[i], comboBox_SpikeGain);

            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Timing.ReferenceClockSource = "OnboardClock";

            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Timing.ConfigureSampleClock("", spikeSamplingRate, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples, Convert.ToInt32(spikeSamplingRate / 2));

            //Verify the Task
            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Timing.ReferenceClockSource = "OnboardClock"; 

            //spikeTask[0].Timing.ReferenceClockSource = "OnboardClock";
            //for (int i = 1; i < spikeTask.Count; ++i)
            //{
            //    spikeTask[i].Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
            //    spikeTask[i].Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
            //}

            //Verify the Task
            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Control(TaskAction.Verify);
            //for (int i = 1; i < spikeTask.Count; ++i)
            //{
            //    spikeTask[i].Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
            //    spikeTask[i].Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
            //}
            

            List<AnalogMultiChannelReader> readers = new List<AnalogMultiChannelReader>(spikeTask.Count);
            for (int i = 0; i < spikeTask.Count; ++i)
                readers.Add(new AnalogMultiChannelReader(spikeTask[i].Stream));          
            double[][] data = new double[numChannels][];
            int c = 0; //Last channel of 'data' written to
            for (int i = 0; i < readers.Count; ++i)
            {
                double[,] tempData = readers[i].ReadMultiSample(NUM_SECONDS_TRAINING * spikeSamplingRate); //Get a few seconds of "noise"
                for (int j = 0; j < tempData.GetLength(0); ++j)
                    data[c++] = ArrayOperation.CopyRow(tempData, j);
            }
            for (int i = 0; i < numChannels; ++i)
                thrSALPA[i] = 9 * 5 * (rawType)Statistics.Variance(data[i]);


            //Now, destroy the objects we made
            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Dispose();
            spikeTask.Clear();
            spikeTask = null;
            buttonStart.Enabled = true;
            label_noise.Text = "Noise levels trained.";
            label_noise.ForeColor = Color.Green;
            checkBox_SALPA.Enabled = true;

            this.Cursor = Cursors.Default;
        }

        #region Stimulation
        /* ************************************************************************
         *  STIMULATION
         * ************************************************************************/
        #region On Demand Stimulation
        private void button_stim_Click(object sender, EventArgs e)
        {
            /* There are two timing schemes.  If the total duration is >500 ms, stopping of the tasks is 
             * software timed (with a Timer object).  Otherwise, the entire waveform is started/stopped in 
             * a single block of code.  This means that long trains might have an extra pulse or two.
             * */
            button_stim.Enabled = false;
            button_stimExpt.Enabled = false;
            listBox_exptStimChannels.Enabled = false;
            listBox_stimChannels.Enabled = false;
            radioButton_OnDemandBiphasic.Enabled = false;
            radioButton_OnDemandUniphasic.Enabled = false;
            button_stim.Refresh();

            int ch = Convert.ToInt32(stimChannel.Value); //Channel to stimulate
            double v = Convert.ToDouble(stimVoltage.Value);
            int width = Convert.ToInt32(stimWidth.Value);
            int numPulses = Convert.ToInt32(stimPulses.Value);
            int rate = Convert.ToInt32(stimRate.Value);
            double inOffsetVoltage = Convert.ToDouble(offsetVoltage.Value);
            int interphaseLength = Convert.ToInt32(stimInterphaseLength.Value);

            //Setup voltage waveform, pos. then neg.
            int size = Convert.ToInt32((width / 1000000.0) * STIM_SAMPLING_FREQ * 2 + 2 * STIM_PADDING); //Num. pts. in pulse
            //What was that doing? Convert width to seconds, divide by sample duration, mult. by
            //two since the pulse is biphasic, add padding to both sides
            int offset = STIM_SAMPLING_FREQ / rate; //The num pts. b/w stim pulses

            if (numPulses > 1)
            {
                if ((double)numPulses * 1000.0 / (double)rate < 500)
                {
                    stimPulseTask.Timing.SamplesPerChannel = offset * numPulses; //Set buffer to exact length of pulse
                    stimDigitalTask.Timing.SamplesPerChannel = offset * numPulses;
                    stimPulseTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
                    stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
                }
                else
                {
                    stimPulseTask.Timing.SamplesPerChannel = offset;
                    stimDigitalTask.Timing.SamplesPerChannel = offset;
                    stimPulseTask.Timing.SampleQuantityMode = SampleQuantityMode.ContinuousSamples;
                    stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.ContinuousSamples;

                }
            }
            else //numPulses == 1
            { //This needs to be done, in case another routine changes the samplequantity mode
                stimPulseTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
                stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
            }
            double[] stim_params = new double[9];
            stim_params[0] = width;
            stim_params[1] = v;
            stim_params[2] = ch;
            stim_params[3] = numPulses;
            stim_params[4] = rate;
            stim_params[5] = inOffsetVoltage;
            stim_params[6] = interphaseLength;
            bw_stim.RunWorkerAsync(stim_params);
        }

        private void bw_stim_DoWork(object sender, DoWorkEventArgs e)
        {
            //Get pulse arguments
            double[] stim_params = (double[])e.Argument;

            //Select between uni- and biphasic pulses
            int phase2Width = (int)stim_params[0];
            if (radioButton_OnDemandUniphasic.Checked)
                phase2Width = 0;

            if (stim_params[3] * 1000 / stim_params[4] < 500)
            {
                //Create pulse
                StimPulse sp = new StimPulse((int)stim_params[0], phase2Width, stim_params[1], -stim_params[1], 
                    (int)stim_params[2], (int)stim_params[3], (int)stim_params[4], (double)stim_params[5], (int)stim_params[6], 10, 10, true);
                if (stim_params[3] == 1)
                {
                    stimPulseTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1);
                    stimDigitalTask.Timing.SamplesPerChannel = sp.digitalData.Length;
                }

                //Write
                stimPulseWriter.WriteMultiSample(true, sp.analogPulse);
                if (Properties.Settings.Default.StimPortBandwidth == 32)
                    stimDigitalWriter.WriteMultiSamplePort(true, sp.digitalData);
                else if (Properties.Settings.Default.StimPortBandwidth == 8)
                    stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(sp.digitalData));

                DateTime stimStartTime = DateTime.Now;
                stimStopTime = stimStartTime.AddMilliseconds(((double)stim_params[3] * 1000.0) / (double)stim_params[4]);
                //timer.Enabled = true;
                stimPulseTask.WaitUntilDone();
                stimDigitalTask.WaitUntilDone();
                stimPulseTask.Stop();
                stimDigitalTask.Stop();
            }
            else
            {
                //Make a single stim pulse, but with the correct number of zeros to insure proper rate
                //when task is regenerative
                StimPulse sp = new StimPulse((int)stim_params[0], phase2Width, stim_params[1], -stim_params[1], (int)stim_params[2], 1, (int)stim_params[4], (double)stim_params[5], (int)stim_params[6], 10, 10, true);
                if (stim_params[3] == 1)
                {
                    stimPulseTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1);
                    stimDigitalTask.Timing.SamplesPerChannel = sp.digitalData.Length;
                }
                stimPulseWriter.WriteMultiSample(true, sp.analogPulse);
                //stimDigitalWriter.WriteWaveform(true, sp.digitalPulse);
                if (Properties.Settings.Default.StimPortBandwidth == 32)
                    stimDigitalWriter.WriteMultiSamplePort(true, sp.digitalData);
                else if (Properties.Settings.Default.StimPortBandwidth == 8)
                    stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(sp.digitalData));
                DateTime stimStartTime = DateTime.Now;
                stimStopTime = stimStartTime.AddMilliseconds(((double)stim_params[3] * 1000.0) / (double)stim_params[4]);
                stimTimer = new System.Threading.Timer(stim_timer_tick, null, 100, 100);
            }
        }

        private void stim_timer_tick(object data)
        {
            DateTime dt = DateTime.Now;
            if (dt > stimStopTime)
            {
                stimPulseTask.Stop();
                stimDigitalTask.Stop();
                stimTimer.Dispose();

                //De-select channel on mux
                stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
                stimDigitalTask.Timing.SamplesPerChannel = 3;
                if (Properties.Settings.Default.StimPortBandwidth == 32)
                    stimDigitalWriter.WriteMultiSamplePort(true, new UInt32[] { 0, 0, 0 });
                else if (Properties.Settings.Default.StimPortBandwidth == 8)
                    stimDigitalWriter.WriteMultiSamplePort(true, new byte[] { 0, 0, 0 });
                stimDigitalTask.WaitUntilDone();
                stimDigitalTask.Stop();
            }
            
        }
        private void bw_stim_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Now that the tasks are running, check periodically to see if they're done using the timer
            //Timer will detect finish and re-enable button
            timer.Interval = 100;
            timer.Enabled = true;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            //Check to see when stim train is over, then reset buttons
            if (stimDigitalTask.IsDone)
            {
                button_stim.Enabled = true;
                button_stimExpt.Enabled = true;
                listBox_exptStimChannels.Enabled = true;
                listBox_stimChannels.Enabled = true;
                radioButton_OnDemandUniphasic.Enabled = true;
                radioButton_OnDemandBiphasic.Enabled = true;
                timer.Enabled = false;
            }
        }
        #endregion //End OneClickStimulation

        #region StimulationExperiment
        /**************************************************************
         * Run a stimulation experiment                               *
         **************************************************************/
        private void button_stimExpt_Click(object sender, EventArgs e)
        {
            if (listBox_exptStimChannels.SelectedIndices.Count < 1)
            {
                MessageBox.Show("At least one channel must be selected.", "Invalid experimental design", MessageBoxButtons.OK);
            }
            else
            {

                //Take care of buttons
                button_stim.Enabled = false;
                button_stimExpt.Enabled = false;
                button_stopExpt.Enabled = true;
                listBox_exptStimChannels.Enabled = false;
                listBox_stimChannels.Enabled = false;
                button_stim.Refresh();
                button_stimExpt.Refresh();

                stimPulseTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
                stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;

                int[] channels = new int[listBox_exptStimChannels.SelectedIndices.Count]; //Only use these channels
                for (int i = 0; i < listBox_exptStimChannels.SelectedIndices.Count; ++i)
                    channels[i] = Convert.ToInt32(listBox_exptStimChannels.SelectedItems[i]);

                int numTrials = Convert.ToInt32(numericUpDown_exptNumRepeats.Value);
                char[] delimiterChars = { ' ', ',', ':', '\t', '\n' };
                string[] s = textBox_exptVoltages.Text.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                double[] voltages = new double[s.Length];
                for (int i = 0; i < s.Length; ++i) voltages[i] = Convert.ToDouble(s[i]);
                s = textBox_exptPPT.Text.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                int[] pulsesPerTrain = new int[s.Length];
                for (int i = 0; i < s.Length; ++i) pulsesPerTrain[i] = Convert.ToInt32(s[i]);
                s = textBox_exptPulseWidths.Text.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
                int[] pulseWidths = new int[s.Length];
                for (int i = 0; i < s.Length; ++i) pulseWidths[i] = Convert.ToInt32(Convert.ToDouble(s[i]));

                const int trainRate = 200; //200Hz = 5ms b/w pulses

                List<Object> exptParams = new List<object>(6);
                exptParams.Add(channels); exptParams.Add(numTrials); exptParams.Add(voltages); exptParams.Add(pulsesPerTrain);
                exptParams.Add(pulseWidths); exptParams.Add(trainRate);

                //Run background thread for expt, set progress bar to 0
                timer_expt.Interval = 1000; //In ms: this is the time between pulses or pulse trains
                bw_genExpt.RunWorkerAsync(exptParams);
                progressBar_stimExpt.Minimum = 0;
            }
        }

        private void bw_genExpt_DoWork(object sender, DoWorkEventArgs e)
        {
            //NB: Within new threads, you shouldn't reference any of the main forms controls
            //expt_Params ep = (expt_Params)e.Argument;
            List<Object> exptParams = (List<Object>)e.Argument;  //exptParams.Add(channels, numTrials, voltages, pulsesPerTrain, pulseWidths, trainRate);

            int[] channels = (int[])exptParams[0];
            int numTrials = (int)exptParams[1];
            double[] voltages = (double[])exptParams[2];
            int[] pulsesPerTrain = (int[])exptParams[3];
            int[] pulseWidths = (int[])exptParams[4];
            int trainRate = (int)exptParams[5];


            stimList = new ArrayList(channels.Length * voltages.Length * numTrials * pulseWidths.Length * pulsesPerTrain.Length);
            for (int i = 0; i < channels.Length; ++i)
            {
                for (int v = 0; v < voltages.Length; ++v)
                {
                    for (int ppt = 0; ppt < pulsesPerTrain.Length; ++ppt)
                    {
                        for (int pw = 0; pw < pulseWidths.Length; ++pw)
                        {
                            for (int j = 0; j < numTrials; ++j) //Repeat each test 5x
                            {
                                StimPulse sp = new StimPulse(pulseWidths[pw], pulseWidths[pw], voltages[v], -voltages[v], channels[i], pulsesPerTrain[ppt], trainRate, 0.0, 0, 100, 100, false);
                                stimList.Add(sp);
                            }
                        }
                    }
                }
            }

        }

        Random randExpt;
        private void bw_genExpt_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar_stimExpt.Maximum = stimList.Count;
            
            randExpt = new Random();
            int idx = randExpt.Next(0, stimList.Count);

            StimPulse sp = (StimPulse)stimList[idx];
            sp.populate();

            if (sp.numPulses > 1)
            {
                stimPulseTask.Timing.SamplesPerChannel = (int)(sp.numPulses * STIM_SAMPLING_FREQ / sp.rate); //Set buffer to exact length of pulse train
                stimDigitalTask.Timing.SamplesPerChannel = (int)(sp.numPulses * STIM_SAMPLING_FREQ / sp.rate);
            }
            else
            {
                stimPulseTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1); //Set buffer to exact length of pulse
                stimDigitalTask.Timing.SamplesPerChannel = sp.digitalData.Length;
            }

            stimPulseWriter.WriteMultiSample(true, sp.analogPulse);
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalWriter.WriteMultiSamplePort(true, sp.digitalData);
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(sp.digitalData));
            timer_expt.Enabled = true;
            stimDigitalTask.WaitUntilDone();
            stimPulseTask.WaitUntilDone();
            stimPulseTask.Stop();
            stimDigitalTask.Stop();

            stimList.RemoveAt(idx);
            progressBar_stimExpt.Increment(1);
            progressBar_stimExpt.Refresh();
        }

        private void timer_expt_Tick(object sender, EventArgs e)
        {
            int idx = randExpt.Next(0, stimList.Count);
            StimPulse sp = (StimPulse)stimList[idx];
            sp.populate();

            if (sp.numPulses > 1)
            {
                stimPulseTask.Timing.SamplesPerChannel = (int)(sp.numPulses * STIM_SAMPLING_FREQ / sp.rate); //Set buffer to exact length of pulse
                stimDigitalTask.Timing.SamplesPerChannel = (int)(sp.numPulses * STIM_SAMPLING_FREQ / sp.rate);
            }
            else
            {
                stimPulseTask.Timing.SamplesPerChannel = sp.analogPulse.GetLength(1); //Set buffer to exact length of pulse
                stimDigitalTask.Timing.SamplesPerChannel = sp.digitalData.Length;
            } 

            stimPulseWriter.WriteMultiSample(true, sp.analogPulse);
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalWriter.WriteMultiSamplePort(true, sp.digitalData);
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(sp.digitalData));
            stimDigitalTask.WaitUntilDone();
            stimPulseTask.WaitUntilDone();
            stimPulseTask.Stop();
            stimDigitalTask.Stop();

            stimList.RemoveAt(idx);
            progressBar_stimExpt.Increment(1);
            progressBar_stimExpt.Refresh();
            if (stimList.Count < 1)
            {
                timer_expt.Enabled = false;
                button_stimExpt.Enabled = true;
                button_stim.Enabled = true;
                button_stopExpt.Enabled = false;
                listBox_exptStimChannels.Enabled = true;
                listBox_stimChannels.Enabled = true;
                progressBar_stimExpt.Value = 0;
                randExpt = null;
            }
        }

        private void button_stopExpt_Click(object sender, EventArgs e)
        {
            timer_expt.Enabled = false;
            stimDigitalTask.WaitUntilDone();
            stimPulseTask.WaitUntilDone();
            stimPulseTask.Stop();
            stimDigitalTask.Stop();
            stimList.Clear();
            button_stimExpt.Enabled = true;
            button_stim.Enabled = true;
            button_stopExpt.Enabled = false;
            listBox_exptStimChannels.Enabled = true;
            listBox_stimChannels.Enabled = true;
            progressBar_stimExpt.Value = 0;
            randExpt = null;

            //De-select channel on mux
            stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
            stimDigitalTask.Timing.SamplesPerChannel = 3;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalWriter.WriteMultiSamplePort(true, new UInt32[] { 0, 0, 0 });
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalWriter.WriteMultiSamplePort(true, new byte[] { 0, 0, 0 });
            stimDigitalTask.WaitUntilDone();
            stimDigitalTask.Stop();
        }
        #endregion //End StimulationExperiment

        #region OpenLoopStimulaton
        /***********************************************************
         * Run Open Loop Stimulation                               *
         ***********************************************************/
        public struct stim_params
        {
            public int width1;
            public int width2;
            public double v1;
            public double v2;
            public double rate;
            public int[] stimChannelList;
            public double offsetVoltage;
            public int interphaseLength;
            public int prephaseLength;
            public int postphaseLength;
        }
        
        private void openLoopStart_Click(object sender, EventArgs e)
        {
            //Check that at least one channel is selected
            if (listBox_stimChannels.SelectedIndices.Count > 0)
            {

                button_stim.Enabled = false;
                button_stimExpt.Enabled = false;
                openLoopStart.Enabled = false;
                openLoopStop.Enabled = true;
                button_stim.Refresh();
                button_stimExpt.Refresh();
                listBox_exptStimChannels.Enabled = false;
                listBox_stimChannels.Enabled = false;

                stim_params sp = new stim_params();

                sp.v1 = Convert.ToDouble(openLoopVoltage1.Value);
                sp.v2 = Convert.ToDouble(openLoopVoltage2.Value);
                sp.width1 = Convert.ToInt32(openLoopWidth1.Value);
                sp.width2 = Convert.ToInt32(openLoopWidth2.Value);
                sp.rate = Convert.ToDouble(openLoopRate.Value);
                sp.offsetVoltage = Convert.ToDouble(offsetVoltage.Value);
                sp.interphaseLength = Convert.ToInt32(openLoopInterphaseLength.Value);
                sp.prephaseLength = Convert.ToInt32(openLoopPrephaseLength.Value);
                sp.postphaseLength = Convert.ToInt32(openLoopPostphaseLength.Value);

                //Get list of channels to stimulate
                sp.stimChannelList = new int[listBox_stimChannels.SelectedIndices.Count];
                for (int i = 0; i < listBox_stimChannels.SelectedIndices.Count; ++i)
                    sp.stimChannelList[i] = listBox_stimChannels.SelectedIndices[i] + 1; //+1 since the stimulator is 1-based

                int sizeSeq = (int)(sp.stimChannelList.GetLength(0) * STIM_SAMPLING_FREQ / sp.rate); //The num pts. for a random seq. of all channels

                stimPulseTask.Timing.SamplesPerChannel = sizeSeq;
                stimDigitalTask.Timing.SamplesPerChannel = sizeSeq;
                stimPulseTask.Timing.SampleQuantityMode = SampleQuantityMode.ContinuousSamples; //When these are set to continuous, the sampling is regenerative
                stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.ContinuousSamples;

                bw_openLoop.RunWorkerAsync(sp);
            }
            else //Display error that no channels are selected
                MessageBox.Show("Stimulation not started. No channels selected for stimulation. Please select at least one channel.", "NeuroRighter Stimulation Error",
                    MessageBoxButtons.OK);
        }

        private void bw_openLoop_DoWork(object sender, DoWorkEventArgs e)
        {
            //DEBUGGING
            stimPulseTask.Control(TaskAction.Verify);
            stimDigitalTask.Control(TaskAction.Verify);

            stim_params sp = (stim_params)e.Argument;
            //Create randomized list of channels
            int numStimChannels = sp.stimChannelList.GetLength(0);
            ArrayList chListSorted = new ArrayList(numStimChannels);
            int[] chListRand = new int[numStimChannels];
            for (int i = 0; i < numStimChannels; ++i)
                chListSorted.Add(sp.stimChannelList[i]);
            Random r = new Random();
            for (int i = 0; i < numStimChannels; ++i)
            {
                int j = r.Next(chListSorted.Count);
                chListRand[i] = (int)chListSorted[j];
                chListSorted.RemoveAt(j);
            }
            StimPulse spulse = new StimPulse(sp.width1, sp.width2, sp.v1, sp.v2, chListRand, sp.rate, sp.offsetVoltage, sp.interphaseLength, sp.prephaseLength, sp.postphaseLength);
            stimPulseWriter.WriteMultiSample(true, spulse.analogPulse);
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalWriter.WriteMultiSamplePort(true, spulse.digitalData);
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(spulse.digitalData));
        }

        private void bw_openLoop_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Now that the tasks are running, check periodically to see if they're done using the timer
            //Timer will detect finish and re-enable button
        }

        private void openLoopStop_Click(object sender, EventArgs e)
        {
            stimDigitalTask.Stop();
            stimPulseTask.Stop();

            //De-select channel on mux
            stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
            stimDigitalTask.Timing.SamplesPerChannel = 3;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalWriter.WriteMultiSamplePort(true, new UInt32[] { 0, 0, 0 });
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalWriter.WriteMultiSamplePort(true, new byte[] { 0, 0, 0 });
            stimDigitalTask.WaitUntilDone();
            stimDigitalTask.Stop();

            button_stim.Enabled = true;
            button_stimExpt.Enabled = true;
            openLoopStart.Enabled = true;
            openLoopStop.Enabled = false;
            listBox_exptStimChannels.Enabled = true;
            listBox_stimChannels.Enabled = true;
        }

        private void button_openLoopSelectNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < numChannels; ++i)
                listBox_stimChannels.SelectedIndices.Remove(i);
        }

        private void button_openLoopSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < numChannels; ++i)
                listBox_stimChannels.SelectedIndices.Add(i);
        }
        #endregion //End OpenLoopStimulaton

        #region Current_vs_Voltage_Control
        private void radioButton_stimCurrentControlled_Click(object sender, EventArgs e)
        {
            if (radioButton_stimCurrentControlled.Checked)
            {
                if (Properties.Settings.Default.UseStimulator)
                {
                    stimIvsVTask = new Task("stimIvsV");
                    //stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port0/line8:15", "",
                    //    ChannelLineGrouping.OneChannelForAllLines);
                    stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                        ChannelLineGrouping.OneChannelForAllLines);
                    stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                    //stimIvsVTask.Timing.ConfigureSampleClock("100kHztimebase", 100000,
                    //    SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                    stimIvsVTask.Control(TaskAction.Verify);
                    //byte[] b_array = new byte[5] { 255, 255, 255, 255, 255 };
                    //DigitalWaveform wfm = new DigitalWaveform(5, 8, DigitalState.ForceDown);
                    //wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                    //stimIvsVWriter.WriteWaveform(true, wfm);
                    stimIvsVWriter.WriteSingleSampleSingleLine(true, true);
                    stimIvsVTask.WaitUntilDone();
                    stimIvsVTask.Stop();
                    stimIvsVTask.Dispose();
                }

                radioButton_impCurrent.Checked = true;
            }
        }

        private void radioButton_stimVoltageControlled_Click(object sender, EventArgs e)
        {
            if (radioButton_stimVoltageControlled.Checked)
            {
                if (Properties.Settings.Default.UseStimulator)
                {
                    //this line goes high (TTL-wise) when we're doing current-controlled stim, low for voltage-controlled
                    stimIvsVTask = new Task("stimIvsV");
                    //stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port0/line8:15", "",
                    //    ChannelLineGrouping.OneChannelForAllLines);
                    stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                        ChannelLineGrouping.OneChannelForAllLines);
                    stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                    //stimIvsVTask.Timing.ConfigureSampleClock("100kHztimebase", 100000,
                    //    SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
                    stimIvsVTask.Control(TaskAction.Verify);
                    //byte[] b_array = new byte[5] { 0, 0, 0, 0, 0 };
                    //DigitalWaveform wfm = new DigitalWaveform(5, 8, DigitalState.ForceDown);
                    //wfm = NationalInstruments.DigitalWaveform.FromPort(b_array);
                    //stimIvsVWriter.WriteWaveform(true, wfm);
                    stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
                    stimIvsVTask.WaitUntilDone();
                    stimIvsVTask.Stop();
                    stimIvsVTask.Dispose();
                }

                radioButton_impVoltage.Checked = true;
            }
        }
        #endregion

        #region DrawStimPulse
        private void drawOpenLoopStimPulse()
        {
            double v1 = Convert.ToDouble(openLoopVoltage1.Value);
            double v2 = Convert.ToDouble(openLoopVoltage2.Value);
            int width1 = Convert.ToInt32(openLoopWidth1.Value);
            int width2 = Convert.ToInt32(openLoopWidth2.Value);
            int interWidth = Convert.ToInt32(openLoopInterphaseLength.Value);
            int pre = Convert.ToInt32(openLoopPrephaseLength.Value);
            int post = Convert.ToInt32(openLoopPostphaseLength.Value);
            double offsetV = Convert.ToInt32(offsetVoltage.Value);

            double[] pulse = new double[STIM_SAMPLING_FREQ * (pre + width1 + interWidth + width2 + post) / 1000000];
            for (int i = 0; i < STIM_SAMPLING_FREQ * pre / 1000000; ++i)
                pulse[i] = offsetV;
            for (int i = STIM_SAMPLING_FREQ * pre / 1000000; i < STIM_SAMPLING_FREQ * (pre + width1) / 1000000; ++i)
                pulse[i] = offsetV + v1;
            for (int i = STIM_SAMPLING_FREQ * (pre + width1) / 1000000; i < STIM_SAMPLING_FREQ * (pre + width1 + interWidth) / 1000000; ++i)
                pulse[i] = offsetV;
            for (int i = STIM_SAMPLING_FREQ * (pre + width1 + interWidth) / 1000000; i < STIM_SAMPLING_FREQ * (pre + width1 + interWidth + width2) / 1000000; ++i)
                pulse[i] = offsetV + v2;
            for (int i = STIM_SAMPLING_FREQ * (pre + width1 + interWidth + width2) / 1000000; i < STIM_SAMPLING_FREQ * (pre + width1 + interWidth + width2 + post) / 1000000; ++i)
                pulse[i] = offsetV;

            waveformGraph_openLoopStimPulse.PlotY(pulse, 0, 1000000 / STIM_SAMPLING_FREQ);


            //Give a little breathing room on plot
            double min = Math.Min(Math.Min(offsetV + v1, offsetV + v2), offsetV);
            double max = Math.Max(Math.Max(offsetV + v1, offsetV + v2), offsetV);
            double diff = max - min;
            if (diff == 0.0) diff = 0.1;
            min -= diff * 0.05;
            max += diff * 0.05;


            waveformGraph_openLoopStimPulse.YAxes[0].Range = new Range(min, max);
        }

        private void openLoopVoltage1_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void openLoopVoltage2_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void openLoopWidth1_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void openLoopWidth2_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void openLoopInterphaseLength_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void openLoopPrephaseLength_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void openLoopPostphaseLength_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }

        private void offsetVoltage_ValueChanged(object sender, EventArgs e)
        {
            drawOpenLoopStimPulse();
        }
        #endregion //End DrawStimPulse region
        #endregion //End stimulation section

        #region Impedance Testing
        /*************************************
         * IMPEDANCE TEST
         * ***********************************/
        private void button_impedanceTest_Click(object sender, EventArgs e)
        {
            stimDigitalTask.Dispose();
            stimPulseTask.Dispose();

            double startFreq = Convert.ToDouble(numericUpDown_impStartFreq.Value);
            double stopFreq = Convert.ToDouble(numericUpDown_impStopFreq.Value);
            double numPeriods = Convert.ToDouble(numericUpDown_impNumPeriods.Value);
            double commandVoltage = Convert.ToDouble(numericUpDown_impCommandVoltage.Value);
            double RCurr = Convert.ToDouble(numericUpDown_RCurr.Value);
            double RMeas = Convert.ToDouble(numericUpDown_RMeas.Value);
            double RGain = Convert.ToDouble(numericUpDown_RGain.Value);

            buttonStart.Enabled = false;  //So users can't try to get data from the same card
            button_impedanceTest.Enabled = false;
            button_computeGain.Enabled = false;
            button_impedanceCancel.Enabled = true;

            //Toggle between voltage/current to discharge any weird build-ups
            if (radioButton_impCurrent.Checked)
            {
                radioButton_impVoltage_Click(this, null);
                System.Threading.Thread.Sleep(500);
                radioButton_impCurrent_Click(this, null);
            }
            else
            {
                radioButton_impCurrent_Click(this, null);
                System.Threading.Thread.Sleep(500);
                radioButton_impVoltage_Click(this, null);
            }

            //Clear plots
            scatterGraph_impedance.Plots.Clear();

            impMeasurer = new Impedance.ImpedanceMeasurer();
            impMeasurer.alertChannelFinished += new Impedance.ImpedanceMeasurer.ChannelFinishedHandler(impedanceChannelFinishedHandler);
            impMeasurer.alertAllFinished += new Impedance.ImpedanceMeasurer.AllFinishedHandler(impedanceFinished);
            impMeasurer.alertProgressChanged += new Impedance.ImpedanceMeasurer.ProgressChangedHandler(impedanceProgressChangedHandler);
            if (checkBox_impedanceAllChannels.Checked)
                impMeasurer.getImpedance(startFreq, stopFreq, numPeriods, radioButton_impCurrent.Checked,
                    1, numChannels, RCurr, RMeas, RGain, commandVoltage, checkBox_impBandpassFilter.Checked, checkBox_impUseMatchedFilter.Checked);
            else
                impMeasurer.getImpedance(startFreq, stopFreq, numPeriods, radioButton_impCurrent.Checked,
                    Convert.ToInt32(numericUpDown_impChannel.Value), 1, RCurr, RMeas, RGain, commandVoltage, checkBox_impBandpassFilter.Checked, checkBox_impUseMatchedFilter.Checked);

        }

        private Impedance.ImpedanceMeasurer impMeasurer;

        private void impedanceChannelFinishedHandler(object sender, int channelIndex, int channel, double[][] impedance, double[] freqs)
        {
            if (scatterGraph_impedance.InvokeRequired)
            {
                scatterGraph_impedance.Invoke(new Impedance.ImpedanceMeasurer.ChannelFinishedHandler(impedanceChannelFinishedHandler), 
                    new object[] { sender, channelIndex, channel, impedance, freqs });
            }
            else
            {
                scatterGraph_impedance.Plots.Add(new ScatterPlot());
                scatterGraph_impedance.Plots[channelIndex].PlotXY(freqs, impedance[channelIndex]);
                scatterGraph_impedance.Refresh();

                textBox_impedanceResults.Text += "Channel " + channel.ToString() + "\r\n\tFrequency (Hz)\tImpedance (Ohms)\r\n";
                for (int f = 0; f < freqs.GetLength(0); ++f)
                    textBox_impedanceResults.Text += "\t" + freqs[f].ToString() + "\t" + string.Format("{0:0.000}", impedance[channelIndex][f]) + "\r\n";
                textBox_impedanceResults.Text += "\r\n";
            }
        }

        private void impedanceProgressChangedHandler(object sender, int percentage, int channel, double frequency)
        {
            if (progressBar_impedance.InvokeRequired)
            {
                progressBar_impedance.Invoke(new Impedance.ImpedanceMeasurer.ProgressChangedHandler(impedanceProgressChangedHandler),
                    new object[] { sender, percentage, channel, frequency });
            }
            else
            {
                progressBar_impedance.Value = percentage;
                label_impedanceProgress.Text = "Ch " + channel + " Freq " + string.Format("{0:0.0}", frequency) + " Hz";
            }
        }

        private void impedanceFinished(object sender)
        {
            progressBar_impedance.Value = 100;
            label_impedanceProgress.Text = "Impedance Progress";

            buttonStart.Enabled = true;
            button_impedanceTest.Enabled = true;
            button_computeGain.Enabled = true;
            button_impedanceCancel.Enabled = false;

            textBox_impedanceResults.SelectAll();

            //Now, destroy the objects we made
            updateSettings();
        }

        private void button_impedanceCancel_Click(object sender, EventArgs e)
        {
            impMeasurer.cancel();
        }

        private void button_impedanceSaveAsMAT_Click(object sender, EventArgs e)
        {
            if (impMeasurer != null) impMeasurer.saveAsMAT();
        }

        private void button_impedanceCopyDataToClipboard_Click(object sender, EventArgs e)
        {
            textBox_impedanceResults.SelectAll();
            textBox_impedanceResults.Copy();
        }

        private void checkBox_impedanceAllChannels_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_impedanceAllChannels.Checked)
                numericUpDown_impChannel.Enabled = false;
            else
                numericUpDown_impChannel.Enabled = true;
        }

        #region Impedance Radio Buttons
        private void radioButton_impCurrent_Click(object sender, EventArgs e)
        {
            if (radioButton_impCurrent.Checked)
            {
                radioButton_stimCurrentControlled.Checked = true;
                radioButton_stimCurrentControlled_Click(null, null);
            }
        }

        private void radioButton_impVoltage_Click(object sender, EventArgs e)
        {
            if (radioButton_impVoltage.Checked)
            {
                radioButton_stimVoltageControlled.Checked = true;
                radioButton_stimVoltageControlled_Click(null, null);
            }
        }
        #endregion

        #endregion //Impedance

        #region Electrolesioning
        /**************************************************************************
         * ELECTROLESIONING
         **************************************************************************/
        private void button_electrolesioningStart_Click(object sender, EventArgs e)
        {
            //Change mouse cursor to waiting cursor
            this.Cursor = Cursors.WaitCursor;

            //Grab values from UI
            double voltage = Convert.ToDouble(numericUpDown_electrolesioningVoltage.Value);
            double duration = Convert.ToDouble(numericUpDown_electrolesioningDuration.Value);
            List<Int32> chList = new List<int>(listBox_electrolesioningChannels.SelectedIndices.Count);
            for (int i = 0; i < listBox_electrolesioningChannels.SelectedIndices.Count; ++i)
                chList.Add(listBox_electrolesioningChannels.SelectedIndices[i] + 1); //+1 since indices are 0-based but channels are 1-base


            //Disable buttons, so users don't try running two experiments at once
            button_electrolesioningStart.Enabled = false;
            button_electrolesioningSelectAll.Enabled = false;
            button_electrolesioningSelectNone.Enabled = false;
            button_electrolesioningStart.Refresh();

            //Refresh stim task
            stimDigitalTask.Dispose();
            stimDigitalTask = new Task("stimDigitalTask_Electrolesioning");
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);

            //Refresh pulse task
            stimPulseTask.Dispose();
            stimPulseTask = new Task("stimPulseTask");
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
            stimPulseWriter = new AnalogMultiChannelWriter(stimPulseTask.Stream);

            stimPulseTask.Timing.ConfigureSampleClock("",
                StimPulse.STIM_SAMPLING_FREQ, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
            stimPulseTask.Timing.SamplesPerChannel = 2;


            stimDigitalTask.Control(TaskAction.Verify);
            stimPulseTask.Control(TaskAction.Verify);

            //For each channel, deliver lesioning pulse
            for (int i = 0; i < chList.Count; ++i)
            {
                int channel = chList[i];
                UInt32 data = StimPulse.channel2MUX((double)channel);

                //Setup digital waveform, open MUX channel
                stimDigitalWriter.WriteSingleSamplePort(true, data);
                stimDigitalTask.WaitUntilDone();
                stimDigitalTask.Stop();

                //Write voltage to channel, wait duration, stop
                stimPulseWriter.WriteMultiSample(true, new double[,] { { 0, 0 }, { 0, 0 }, { voltage, voltage }, { 0, 0 } });
                stimPulseTask.WaitUntilDone();
                stimPulseTask.Stop();
                Thread.Sleep((int)(Math.Round(duration * 1000))); //Convert to ms
                stimPulseWriter.WriteMultiSample(true, new double[,] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } });
                stimPulseTask.WaitUntilDone();
                stimPulseTask.Stop();

                //Close MUX
                stimDigitalWriter.WriteSingleSamplePort(true, 0);
                stimDigitalTask.WaitUntilDone();
                stimDigitalTask.Stop();
            }

            bool[] fData = new bool[Properties.Settings.Default.StimPortBandwidth];
            stimDigitalWriter.WriteSingleSampleMultiLine(true, fData);
            stimDigitalTask.WaitUntilDone();
            stimDigitalTask.Stop();

            button_electrolesioningSelectAll.Enabled = true;
            button_electrolesioningSelectNone.Enabled = true;
            button_electrolesioningStart.Enabled = true;

            //Now, destroy the objects we made
            updateSettings();
            this.Cursor = Cursors.Default;
        }

        private void button_electrolesioningSelectAll_Click(object sender, EventArgs e)
        {
            listBox_electrolesioningChannels.SelectedIndices.Clear();
            for (int i = 0; i < listBox_electrolesioningChannels.Items.Count; ++i)
                listBox_electrolesioningChannels.SelectedIndices.Add(i);
        }

        private void button_electrolesioningSelectNone_Click(object sender, EventArgs e)
        {
            listBox_electrolesioningChannels.SelectedIndices.Clear();
        }

        #endregion

        #region diagnostics
        /*********************************************************************
         * Diagnostics Section
         *********************************************************************/
        private List<AnalogMultiChannelReader> diagnosticsReaders;
        private double[][] gains;
        private void button_computeGain_Click(object sender, EventArgs e)
        {
            double startFreq = Convert.ToDouble(numericUpDown_startFreq.Value);
            double stopFreq = Convert.ToDouble(numericUpDown_stopFreq.Value);
            double numPeriods = Convert.ToDouble(numericUpDown_numPeriods.Value);
            double[] freqs = new double[1 + Convert.ToInt32(Math.Floor(Math.Log(stopFreq / startFreq) / Math.Log(Convert.ToDouble(textBox_diagnosticsMult.Text))))]; //This determines the number of frequencies counting by doublings

            radioButton_stimVoltageControlled.Checked = true;
            radioButton_stimVoltageControlled_Click(null, null);
            
            //Populate freqs vector
            freqs[0] = startFreq;
            for (int i = 1; i < freqs.GetLength(0); ++i)
                freqs[i] = freqs[i - 1] * Convert.ToDouble(textBox_diagnosticsMult.Text);
            
            spikeSamplingRate = Convert.ToInt32(textBox_spikeSamplingRate.Text);
            buttonStart.Enabled = false;  //So users can't try to get data from the same card
            button_computeGain.Enabled = false;
            button_computeGain.Refresh();
            buttonStart.Refresh();
            spikeTask = new List<Task>(Properties.Settings.Default.AnalogInDevice.Count);
            diagnosticsReaders = new List<AnalogMultiChannelReader>(Properties.Settings.Default.AnalogInDevice.Count);
            for (int i = 0; i < Properties.Settings.Default.AnalogInDevice.Count; ++i)
            {
                spikeTask.Add(new Task("spikeTask_Diagnostics"));
                int numChannelsPerDevice = (numChannels < 32 ? numChannels : 32);
                for (int j = 0; j < numChannelsPerDevice; ++j)
                    spikeTask[i].AIChannels.CreateVoltageChannel(Properties.Settings.Default.AnalogInDevice[0] + "/ai" + j.ToString(), "",
                        AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);

                //Change gain based on comboBox values (1-100)
                setGain(spikeTask[i], comboBox_SpikeGain);

                //Verify the Task
                spikeTask[i].Control(TaskAction.Verify);

                spikeTask[i].Timing.ConfigureSampleClock("", spikeSamplingRate, SampleClockActiveEdge.Rising,
                    SampleQuantityMode.FiniteSamples);
                diagnosticsReaders.Add(new AnalogMultiChannelReader(spikeTask[i].Stream));
            }

            spikeTask[0].Timing.ReferenceClockSource = "OnboardClock";
            for (int i = 1; i < spikeTask.Count; ++i)
            {
                spikeTask[i].Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
                spikeTask[i].Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;
            }
            stimPulseTask.Timing.ReferenceClockSource = spikeTask[0].Timing.ReferenceClockSource;
            stimPulseTask.Timing.ReferenceClockRate = spikeTask[0].Timing.ReferenceClockRate;

            stimDigitalTask.Dispose();
            stimDigitalTask = new Task("stimDigitalTask");
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);
            stimPulseTask.Timing.ConfigureSampleClock("/" + Properties.Settings.Default.AnalogInDevice[0] + "/ai/SampleClock",
                spikeSamplingRate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples);
            stimPulseTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("/" +
                                Properties.Settings.Default.AnalogInDevice[0] + "/ai/StartTrigger",
                                DigitalEdgeStartTriggerEdge.Rising);

            stimDigitalTask.Control(TaskAction.Verify);
            stimPulseTask.Control(TaskAction.Verify);

            switch (comboBox_numChannels.SelectedIndex)
            {
                case 0:
                    numChannels = 16;
                    break;
                case 1:
                    numChannels = 32;
                    break;
                case 2:
                    numChannels = 48;
                    break;
                case 3:
                    numChannels = 64;
                    break;
            }
            //gains = new double[numChannels, freqs.GetLength(0)];
            //numChannels = 1;

            gains = new double[numChannels][];
            for (int i = 0; i < numChannels; ++i)
                gains[i] = new double[freqs.GetLength(0)];
            scatterGraph_diagnostics.ClearData();
            scatterGraph_diagnostics.Plots.Clear();

            textBox_diagnosticsResults.Clear();

            if (!checkBox_diagnosticsBulk.Checked)
            {
                //for (int c = 1; c <= numChannels; ++c)
                for (int c = 13; c < 14; ++c)
                {
                    textBox_diagnosticsResults.Text += "Channel " + c.ToString() + "\r\n\tFrequency (Hz)\tGain (dB)\r\n";

                    scatterGraph_diagnostics.Plots.Add(new ScatterPlot());

                    UInt32 data = StimPulse.channel2MUX((double)c); //Get data bits lined up to control MUXes

                    //Setup digital waveform
                    stimDigitalWriter.WriteSingleSamplePort(true, data);
                    stimDigitalTask.WaitUntilDone();
                    stimDigitalTask.Stop();

                    for (int f = 0; f < freqs.GetLength(0); ++f)
                    {
                        double numSeconds = 1 / freqs[f];
                        if (numSeconds * numPeriods < 0.1)
                        {
                            numPeriods = Math.Ceiling(0.1 * freqs[f]);
                        }

                        int size = Convert.ToInt32(numSeconds * spikeSamplingRate);
                        SineSignal testWave = new SineSignal(freqs[f], Convert.ToDouble(numericUpDown_diagnosticsVoltage.Value));  //Generate a 100 mV sine wave at 1000 Hz
                        double[] testWaveValues = testWave.Generate(spikeSamplingRate, size);
 
                        double[,] analogPulse = new double[2, size];

                        for (int i = 0; i < size; ++i)
                            analogPulse[0, i] = testWaveValues[i];

                        for (int i = 0; i < spikeTask.Count; ++i)
                            spikeTask[i].Timing.SamplesPerChannel = (long)(numPeriods * size);

                        stimPulseTask.Timing.SamplesPerChannel = (long)(numPeriods * size); //Do numperiods cycles of sine wave
                        stimPulseWriter.WriteMultiSample(true, analogPulse);

                        double[] stateData = new double[4];
                        stateData[0] = (double)c;
                        stateData[1] = freqs[f];
                        stateData[2] = (double)f;
                        for (int i = diagnosticsReaders.Count - 1; i >= 0; --i)
                        {
                            stateData[3] = (double)i;
                            diagnosticsReaders[i].BeginReadMultiSample((int)(numPeriods * size), analogInCallback_computeGain, (Object)stateData); //Get 5 seconds of "noise"
                        }

                        stimPulseTask.WaitUntilDone();
                        for (int i = 0; i < spikeTask.Count; ++i)
                        {
                            spikeTask[i].WaitUntilDone();
                            spikeTask[i].Stop();
                        }
                        stimPulseTask.Stop();
                    }
                    stimDigitalWriter.WriteSingleSamplePort(true, 0);
                    stimDigitalTask.WaitUntilDone();
                    stimDigitalTask.Stop();
//DEBUGGING
                    c = 1;
                    scatterGraph_diagnostics.Plots[c - 1].PlotXY(freqs, gains[c - 1]);
                    for (int f = 0; f < freqs.GetLength(0); ++f)
                    {
                        textBox_diagnosticsResults.Text += "\t" + freqs[f].ToString() + "\t" + gains[c - 1][f] + "\r\n";
                    }
                    textBox_diagnosticsResults.Text += "\r\n";
                    scatterGraph_diagnostics.Refresh();

//DEBUGGING
                    c = 100;
                }
            }
            else
            {
                for (int f = 0; f < freqs.GetLength(0); ++f)
                {
                    double numSeconds = 1 / freqs[f];
                    if (numSeconds * numPeriods < 0.1)
                    {
                        numPeriods = Math.Ceiling(0.1 * freqs[f]);
                    }

                    int size = Convert.ToInt32(numSeconds * spikeSamplingRate);
                    SineSignal testWave = new SineSignal(freqs[f], Convert.ToDouble(numericUpDown_diagnosticsVoltage.Value));  //Generate a 100 mV sine wave at 1000 Hz
                    double[] testWaveValues = testWave.Generate(spikeSamplingRate, size);

                    
                    double[,] analogPulse = new double[2, size];

                    for (int i = 0; i < size; ++i)
                        analogPulse[0, i] = testWaveValues[i];

                    for (int i = 0; i < spikeTask.Count; ++i)
                        spikeTask[i].Timing.SamplesPerChannel = (long)(numPeriods * size);

                    stimPulseTask.Timing.SamplesPerChannel = (long)(numPeriods * size); //Do numperiods cycles of sine wave
                    stimPulseWriter.WriteMultiSample(true, analogPulse);

                    double[] stateData = new double[4];
                    stateData[0] = -1.0;
                    stateData[1] = freqs[f]; 
                    stateData[2] = (double)f; //Frequency of interest

                    for (int i = diagnosticsReaders.Count - 1; i >= 0; --i)
                    {
                        stateData[3] = (double)i; //Keeps track of which device called the reader
                        diagnosticsReaders[i].BeginReadMultiSample((int)(numPeriods * size), analogInCallback_computeGain, (Object)stateData); //Get 5 seconds of "noise"
                    }

                    stimPulseTask.WaitUntilDone();
                    for (int i = 0; i < spikeTask.Count; ++i)
                    {
                        spikeTask[i].WaitUntilDone();
                        spikeTask[i].Stop();
                    }
                    stimPulseTask.Stop();
                }
                for (int c = 0; c < numChannels; ++c)
                {
                    scatterGraph_diagnostics.Plots.Add(new ScatterPlot());
                    scatterGraph_diagnostics.Plots[c].PlotXY(freqs, gains[c]);
                    textBox_diagnosticsResults.Text += "Channel " + (c+1).ToString() + "\r\n\tFrequency (Hz)\tGain (dB)\r\n";
                    for (int f = 0; f < freqs.GetLength(0); ++f)
                    {
                        textBox_diagnosticsResults.Text += "\t" + freqs[f].ToString() + "\t" + gains[c][f].ToString() + "\r\n";
                    }
                    textBox_diagnosticsResults.Text += "\r\n";
                }
                scatterGraph_diagnostics.Refresh();
            }
            buttonStart.Enabled = true;
            button_computeGain.Enabled = true;

            //Now, destroy the objects we made
            updateSettings();
            gains = null;
            diagnosticsReaders = null;
        }

        private void analogInCallback_computeGain(IAsyncResult ar)
        {
            double[] state = (double[])ar.AsyncState;
            int ch = (int)state[0];
            double f = state[1];
            int reader = (int)state[3];
            ButterworthBandpassFilter bwfilt = null;
            if (checkBox_diagnosticsDigitalFilter.Checked)
                bwfilt = new ButterworthBandpassFilter(1, spikeSamplingRate, f - f / 8, f + f / 8);

            double[,] data = diagnosticsReaders[reader].EndReadMultiSample(ar);

            double[] oneChannelData = new double[data.GetLength(1)];
            double RMSinput = 0.707106704695506 * Convert.ToDouble(numericUpDown_diagnosticsVoltage.Value);
            if (checkBox_diagnosticsVotlageDivider.Checked)
                RMSinput /= Convert.ToDouble(textBox_voltageDivider.Text);
            if (ch != -1 && ch < (reader + 1) * 32 && ch >= reader * 32) //If the channel is not "all channels" it should be in the particular device's range
            //if (ch > 0)
            {
                for (int i = 0; i < data.GetLength(1); ++i)
                    oneChannelData[i] = data[ch - 1, i];
                //Filter data to bring out pure tone
                if (checkBox_diagnosticsDigitalFilter.Checked && bwfilt != null)
                    oneChannelData = bwfilt.FilterData(oneChannelData);

                double rms = rootMeanSquared(oneChannelData);
//DEBUGGING
ch = 1;
                gains[ch - 1][(int)state[2]] = rms / RMSinput;
                gains[ch - 1][(int)state[2]] = 20 * Math.Log10(gains[ch - 1][(int)state[2]]);
            }
            else if (ch == -1) //Do all channels at once, but this requires special hardware (like Plexon headstage tester)
            {
                for (int i = 0; i < numChannels; ++i)
                {
                    if (checkBox_diagnosticsDigitalFilter.Checked) 
                        oneChannelData = bwfilt.FilterData(ArrayOperation.CopyRow(data, i));
                    oneChannelData = ArrayOperation.CopyRow(data, i);
                    double rms = rootMeanSquared(oneChannelData);
                    gains[i][(int)state[2]] = rms / RMSinput;
                    gains[i][(int)state[2]] = 20 * Math.Log10(gains[i][(int)state[2]]);
                }
            }
        }
        #endregion

        #region Programmable_Referencing
        /***************************************************
        /* Deal with programmable referencing
        /***************************************************/
        private void changeReference(int type)
        {
            //'type' is 0 for spikes, 1 for LFPs
            int ch = 0;
            if (type == 0)
                ch = Convert.ToInt32(numericUpDown_analogRefSpikes.Value);
            else if (type == 1)
                ch = Convert.ToInt32(numericUpDown_analogRefLFPs.Value) + 32; //32 comes from the 32 channel preamp
            else if (type == 2)   //Reset spike refs
            {
                serialOut.Write("#0140/3," + currentRef[0].ToString() + "\r");
                for (int i = 1; i <= 32; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",8\r");
                }
                return;
            }
            else if (type == 3)  //Reset LFP refs
            {
                serialOut.Write("#0140/3," + currentRef[1].ToString() + "\r");
                for (int i = 33; i <= 64; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",8\r");
                }
                return;
            }

            if (currentRef[type] > 0)
            {
                serialOut.Write("#0140/3," + currentRef[type].ToString() + "\r"); //Disconnect old ch from ref1
                //serialOut.Write("#0140/3," + (currentRef+32).ToString() + "\r"); //Disconnect old ch from ref1
            }
            currentRef[type] = ch;
            serialOut.Write("#0140/3," + currentRef[type].ToString() + "\r"); //Disconnect new ch from any ref

            if (type == 0)
                serialOut.Write("#0140/2," + currentRef[type].ToString() + ",1\r"); //Set 'ch' to ref1
            else if (type == 1)
                serialOut.Write("#0140/2," + currentRef[type].ToString() + ",2\r"); //Set 'ch' to ref1

            //Set ref channel's ref to default
            serialOut.Write("#0140/5," + currentRef[type].ToString() + "\r");
            serialOut.Write("#0140/4," + currentRef[type].ToString() + ",8\r");
            //Likewise for LFPs
            //serialOut.Write("#0140/5," + (currentRef + 32).ToString() + "\r");
            //serialOut.Write("#0140/4," + (currentRef + 32).ToString() + ",8\r");


            //Now, set all other channel's reference's to ref1
            if (type == 0)
            {
                for (int i = 1; i < currentRef[type]; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",1\r");
                }
                for (int i = currentRef[type] + 1; i <= 32; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",1\r");
                }
            }
            else if (type == 1)
            {
                //Now, do the same things for LFP channels
                for (int i = 33; i < currentRef[type]; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",2\r");
                }
                for (int i = currentRef[type] + 1; i <= 64; ++i)
                {
                    serialOut.Write("#0140/5," + i.ToString() + "\r");
                    serialOut.Write("#0140/4," + i.ToString() + ",2\r");
                }
            }

        }

        private void numericUpDown_analogRefSpikes_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox_analogRefSpikes.Checked)
                changeReference(0); //Send 0 for spikes, 1 for LFPs
        }

        private void numericUpDown_analogRefLFPs_ValueChanged(object sender, EventArgs e)
        {
            if (checkBox_analogRefSpikes.Checked)
                changeReference(1); //Send 0 for spikes, 1 for LFPs
        }

        private void checkBox_analogRefSpikes_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox_analogRefSpikes.Checked)
                changeReference(0);
            else
            {
                //serialOut.Write("#0140/0\r"); //Reset everything to power-up state
                changeReference(2);
                currentRef[0] = 0;
            }
        }

        private void checkBox_analogRefLFPs_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox_analogRefSpikes.Checked)
                changeReference(1);
            else
            {
                //serialOut.Write("#0140/0\r"); //Reset everything to power-up state
                changeReference(3);
                currentRef[1] = 0;
            }
        }

        private void button_analogResetRefs_Click(object sender, EventArgs e)
        {
            serialOut.Write("#0140/0\r"); //Reset everything to power-up state
        }
        #endregion

        BakkumExpt expt;
        private void button_closedLoopLearningStart_Click(object sender, EventArgs e)
        {
            button_closedLoopLearningStart.Enabled = false;
            button_closedLoopLearningStop.Enabled = true;

            List<int> probeChannels = new List<int>();
            List<int> CPSChannels = new List<int>();
            List<int> PTSChannels = new List<int>();

            for (int i = 0; i < listBox_closedLoopLearningProbeElectrodes.SelectedItems.Count; ++i)
                probeChannels.Add(Convert.ToInt32(listBox_closedLoopLearningProbeElectrodes.SelectedItems[i]));
            for (int i = 0; i < listBox_closedLoopLearningCPSElectrodes.SelectedItems.Count; ++i)
                CPSChannels.Add(Convert.ToInt32(listBox_closedLoopLearningCPSElectrodes.SelectedItems[i]));
            for (int i = 0; i < listBox_closedLoopLearningPTSElectrodes.SelectedItems.Count; ++i)
                PTSChannels.Add(Convert.ToInt32(listBox_closedLoopLearningPTSElectrodes.SelectedItems[i]));

            //Get user's desired votlage
            double voltage = Convert.ToDouble(numericUpDown_bakkumVoltage.Value);

            expt = new BakkumExpt(CPSChannels, probeChannels, PTSChannels, voltage, stimDigitalTask, stimPulseTask,
                stimDigitalWriter, stimPulseWriter);
            expt.linkToSpikes(this);
            expt.start();
        }

        private void button_closedLoopLearningStop_Click(object sender, EventArgs e)
        {
            expt.stop();
            button_closedLoopLearningStart.Enabled = true;
            button_closedLoopLearningStop.Enabled = false;
        }

        #region IISZapper (Experimental)

        internal delegate void IISDetectedHandler(object sender, double[][] lfpData, int numReads);
        internal event IISDetectedHandler IISDetected;
        private IISZapper iisZap;

        private void button_IISZapper_start_Click(object sender, EventArgs e)
        {
            button_IISZapper_start.Enabled = false;
            iisZap = new IISZapper((int)numericUpDown_IISZapper_phaseWidth.Value,
                (double)numericUpDown_IISZapper_voltage.Value, (int)numericUpDown_IISZapper_channel.Value,
                (int)numericUpDown_IISZapper_pulsePerTrain.Value, (double)numericUpDown_IISZapper_rate.Value,
                stimDigitalTask, stimPulseTask, stimDigitalWriter, stimPulseWriter, DEVICE_REFRESH,
                this);

            iisZap.start(this);
            button_IISZapper_stop.Enabled = true;
        }

        private void button_IISZapper_stop_Click(object sender, EventArgs e)
        {
            button_IISZapper_stop.Enabled = false;
            iisZap.stop(this);
            button_IISZapper_start.Enabled = true;
        }
        #endregion //IISZapper

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        private void processingSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessingSettings ps = new ProcessingSettings();
            ps.ShowDialog();
            updateSettings();
        }

        private void radioButton_spikesReferencingCommonAverage_CheckedChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                if (radioButton_spikesReferencingCommonAverage.Checked)
                    referncer = new Filters.CommonAverageReferencer(spikeBufferLength);
            }
        }

        private void radioButton_spikesReferencingCommonMedian_CheckedChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                if (radioButton_spikesReferencingCommonMedian.Checked)
                    referncer = new Filters.CommonMedianReferencer(spikeBufferLength, numChannels);
            }
        }

        private void radioButton_spikesReferencingCommonMedianLocal_CheckedChanged(object sender, EventArgs e)
        {
            int channelsPerGroup = Convert.ToInt32(numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value);
            lock (this)
            {
                if (radioButton_spikesReferencingCommonMedianLocal.Checked)
                    referncer = new Filters.CommonMedianLocalReferencer(spikeBufferLength, channelsPerGroup, numChannels / channelsPerGroup);
            }
        }

        private void radioButton_spikeReferencingNone_CheckedChanged(object sender, EventArgs e)
        {
            lock (this)
            {
                if (radioButton_spikeReferencingNone.Checked)
                    referncer = null;
            }
        }

        private void button_showcase_Click(object sender, EventArgs e)
        {
            if (videoTask != null)
            {
                videoTask.Dispose();
                videoTask = null;
            }

            Stimulation.StimulatorShowcaser ss = new Stimulation.StimulatorShowcaser(stimDigitalTask, stimPulseTask, stimDigitalWriter, stimPulseWriter);
            //ss.makeSampleWaveforms();

            radioButton_impCurrent.Checked = true;
            radioButton_stimCurrentControlled.Checked = true;
            radioButton_impVoltage.Checked = false;
            radioButton_stimVoltageControlled.Checked = false;
            radioButton_stimCurrentControlled_Click(null, null);
            ss.makeDualVIWaveforms(true);

            System.Threading.Thread.Sleep(1000); //Rest a little to let things discharge

            radioButton_impCurrent.Checked = false;
            radioButton_stimCurrentControlled.Checked = false;
            radioButton_impVoltage.Checked = true;
            radioButton_stimVoltageControlled.Checked = true;
            radioButton_stimVoltageControlled_Click(null, null);
            ss.makeDualVIWaveforms(false);


            updateSettings();
        }

        private void timer_timeElapsed_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now - experimentStartTime;
            label_timeElapsed.Text = "Time elapsed: \r\n" + String.Format("{0:00}:{1:00}:{2:00}",(int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        private void checkBox_artiFilt_CheckedChanged(object sender, EventArgs e)
        {
            //artiFilt = new Filters.ArtiFilt(0.001, 0.002, spikeSamplingRate, numChannels);
            artiFilt = new Filters.ArtiFilt_Interpolation(0.001, 0.002, spikeSamplingRate, numChannels);
        }

        private void button_scaleUp_MouseEnter(object sender, EventArgs e) { mouseOver(sender, 1); }
        private void button_scaleUp_MouseLeave(object sender, EventArgs e) { mouseOver(sender, 0); }
        private void button_scaleDown_MouseEnter(object sender, EventArgs e) { mouseOver(sender, 3); }
        private void button_scaleDown_MouseLeave(object sender, EventArgs e) { mouseOver(sender, 2); }
        private void button_scaleReset_MouseEnter(object sender, EventArgs e) { mouseOver(sender, 5); }
        private void button_scaleReset_MouseLeave(object sender, EventArgs e) { mouseOver(sender, 4); }
        private void mouseOver(object sender, int imageNumber)
        {
           Button b = (Button)sender;
           b.Image = imageList_zoomButtons.Images[imageNumber];
        }

        private void checkBox_enableTimedRecording_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown_timedRecordingDuration.Enabled = checkBox_enableTimedRecording.Checked;
            numericUpDown_timedRecordingDurationSeconds.Enabled = checkBox_enableTimedRecording.Checked;
        }

        private Stimulation.OpenLoopFollowerTest olfTest;
        private void button_OpenLoopFollowerStart_Click(object sender, EventArgs e)
        {
            //Take care of buttons
            button_stim.Enabled = false;
            button_stimExpt.Enabled = false;
            button_OpenLoopFollowerStart.Enabled = false;
            button_OpenLoopFollowerStop.Enabled = true;
            button_ClosedLoopFollowerStart.Enabled = false;
            button_stim.Refresh();
            button_stimExpt.Refresh();

            List<int> channels = new List<int>(listBox_OpenLoopFollowerChannels.SelectedIndices.Count); //Only use these channels
            for (int i = 0; i < listBox_OpenLoopFollowerChannels.SelectedIndices.Count; ++i)
                channels.Add(Convert.ToInt32(listBox_OpenLoopFollowerChannels.SelectedItems[i]));

            char[] delimiterChars = { ' ', ',', ':', '\t', '\n' };
            string[] s = textBox_OpenLoopFollowerFrequencies.Text.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            List<double> frequencies = new List<double>(s.Length);
            for (int i = 0; i < s.Length; ++i) frequencies.Add(Convert.ToDouble(s[i]));
            double voltage = Convert.ToDouble(numericUpDown_OpenLoopFollowerVoltage.Value);

            //Get durations
            double Duration = Convert.ToDouble(numericUpDown_OpenLoopFollowerDuration.Value);
            double WaitDuration = Convert.ToDouble(numericUpDown_OpenLoopFollowerWaitDuration.Value);

            olfTest = new Stimulation.OpenLoopFollowerTest(channels, frequencies, voltage, Duration, WaitDuration,
                stimPulseTask, stimDigitalTask, stimPulseWriter, stimDigitalWriter);
            olfTest.alertAllFinished += new Stimulation.OpenLoopFollowerTest.AllFinishedHandler(OpenLoopFollowerFinished);
            olfTest.alertProgressChanged += new Stimulation.OpenLoopFollowerTest.ProgressChangedHandler(OpenLoopFollowerProgressChanged);
            progressBar__OpenLoopFollowerProgressBar.Minimum = 0;
            progressBar__OpenLoopFollowerProgressBar.Maximum = 100;
            progressBar__OpenLoopFollowerProgressBar.Value = 0;
            olfTest.Start();
        }

        private void OpenLoopFollowerProgressChanged(object sender, int percent, int channel, double frequency)
        {
            progressBar__OpenLoopFollowerProgressBar.Value = percent;
            label_OpenLoopFollowerStatus.Text = "Channel: " + channel.ToString() + "\r\nFreq. (Hz): " + frequency.ToString("F1");
        }

        private void OpenLoopFollowerFinished(object sender)
        {
            olfTest = null;

            button_stim.Enabled = true;
            button_stimExpt.Enabled = true;
            button_OpenLoopFollowerStart.Enabled = true;
            button_OpenLoopFollowerStop.Enabled = false;
            button_ClosedLoopFollowerStart.Enabled = true;
        }

        private void button_OpenLoopFollowerStop_Click(object sender, EventArgs e)
        {
            olfTest.Stop();
            olfTest = null;

            button_stim.Enabled = true;
            button_stimExpt.Enabled = true;
            button_OpenLoopFollowerStart.Enabled = true;
            button_OpenLoopFollowerStop.Enabled = false;
            button_ClosedLoopFollowerStart.Enabled = true;
        }

        private Stimulation.ClosedLoopFollowerTest clfTest;
        private void button_ClosedLoopFollowerStart_Click(object sender, EventArgs e)
        {
            if (checkBox_SALPA.Checked)
            {
                //Take care of buttons
                button_stim.Enabled = false;
                button_stimExpt.Enabled = false;
                button_OpenLoopFollowerStart.Enabled = false;
                button_ClosedLoopFollowerStop.Enabled = true;
                button_ClosedLoopFollowerStart.Enabled = false;
                button_stim.Refresh();
                button_stimExpt.Refresh();

                List<int> channels = new List<int>(listBox_ClosedLoopFollowerChannels.SelectedIndices.Count); //Only use these channels
                for (int i = 0; i < listBox_ClosedLoopFollowerChannels.SelectedIndices.Count; ++i)
                    channels.Add(Convert.ToInt32(listBox_ClosedLoopFollowerChannels.SelectedItems[i]));
                double voltage = Convert.ToDouble(numericUpDown_ClosedLoopFollowerVoltage.Value);

                //calculate blanking time
                double blankingTime = 0.0;
                blankingTime += (double)(SALPAFilter.postPeg + SALPAFilter.postPegZero + SALPAFilter.prePeg) / spikeSamplingRate;

                clfTest = new Stimulation.ClosedLoopFollowerTest(channels, voltage, stimPulseTask, stimDigitalTask, stimPulseWriter,
                    stimDigitalWriter, blankingTime);
                clfTest.alertProgressChanged += new Stimulation.ClosedLoopFollowerTest.ProgressChangedHandler(ClosedLoopFollowerProgressChanged);
                clfTest.alertAllFinished += new Stimulation.ClosedLoopFollowerTest.AllFinishedHandler(ClosedLoopFollowerFinished);
                clfTest.linkToSpikes(this);
                clfTest.Start();

            }

            else
                MessageBox.Show("SALPA must be running.", "Closed-loop Follower Error", MessageBoxButtons.OK);

        }

        private void ClosedLoopFollowerProgressChanged(object sender, int channel, double frequency, double metric, double metricDifference)
        {
            label_ClosedLoopFollowerData.Text = "Ch #" + channel.ToString() + ", " + frequency.ToString("F1") + " Hz\r\n" +
                "Value = " + metric.ToString("F2") + "\r\nDifference = " + metricDifference.ToString("F2") + "%";
        }

        private void ClosedLoopFollowerFinished(object sender)
        {
            clfTest = null;

            button_stim.Enabled = true;
            button_stimExpt.Enabled = true;
            button_OpenLoopFollowerStart.Enabled = true;
            button_ClosedLoopFollowerStop.Enabled = false;
            button_ClosedLoopFollowerStart.Enabled = true;
        }

        private void button_ClosedLoopFollowerStop_Click(object sender, EventArgs e)
        {
            if (clfTest != null)
            {
                clfTest.Stop(this);
                clfTest = null;

                button_stim.Enabled = true;
                button_stimExpt.Enabled = true;
                button_OpenLoopFollowerStart.Enabled = true;
                button_ClosedLoopFollowerStop.Enabled = false;
                button_ClosedLoopFollowerStart.Enabled = true;
            }
        }

        private void button_ElectrodeScreeningSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox_exptStimChannels.Items.Count; ++i)
                listBox_exptStimChannels.SetSelected(i, true);
        }

        private void button_ElectrodeScreeningSelectNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox_exptStimChannels.Items.Count; ++i)
                listBox_exptStimChannels.SetSelected(i, false);
        }

        private void numericUpDown_CommonMedianLocalReferencingChannelsPerGroup_ValueChanged(object sender, EventArgs e)
        {
            int channelsPerGroup = Convert.ToInt32(numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value);
            if (numChannels % channelsPerGroup != 0)
            {
                channelsPerGroup = 8;
                numericUpDown_CommonMedianLocalReferencingChannelsPerGroup.Value = 8;
                MessageBox.Show("Value must evenly divide total number of channels.");
            }
            if (radioButton_spikesReferencingCommonMedianLocal.Checked)
                referncer = new Filters.CommonMedianLocalReferencer(spikeBufferLength, channelsPerGroup, numChannels / channelsPerGroup);
        }

        #region arbitrary stimulation from file
        File2Stim3 custprot;

        private void button_startStimFromFile_Click(object sender, EventArgs e)
        {
            if (textBox_protocolFileLocations.Text.Length < 1)
            {
                MessageBox.Show("Please enter the directory and file base of your stimulation protcol");
            }
            else
            {
                button_startStimFromFile.Enabled = false;
                button_stopStimFromFile.Enabled = true;

                string stimfile = textBox_protocolFileLocations.Text;

                // Make sure that the user has input a valid file path
                if (!checkFilePath(stimfile))
                {
                    MessageBox.Show("The *.olstim file provided does not exist");
                    button_startStimFromFile.Enabled = true;
                    button_stopStimFromFile.Enabled = false;
                    return;
                }

                // Refresh DAQ tasks as they are needed for file2stim
                if (stimPulseTask != null) { stimPulseTask.Dispose(); stimPulseTask = null; }
                if (stimDigitalTask != null) { stimDigitalTask.Dispose(); stimDigitalTask = null; }

                // Create new DAQ tasks and corresponding writers
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

                stimPulseTask.Timing.ReferenceClockSource = "OnboardClock";

                // Setup the AO and DO tasks for continuous stimulaiton
                stimPulseTask.Timing.ConfigureSampleClock("100kHzTimebase",Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, STIMBUFFSIZE);
                stimDigitalTask.Timing.ConfigureSampleClock("100kHzTimebase", Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, STIMBUFFSIZE);
                stimDigitalTask.SynchronizeCallbacks = false;
                stimPulseTask.SynchronizeCallbacks = false;

                //Create output writers
                stimFromFileAnalogWriter = new AnalogMultiChannelWriter(stimPulseTask.Stream);
                stimFromFileDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);

                //Verify the tasks
                stimPulseTask.Control(TaskAction.Verify);
                stimDigitalTask.Control(TaskAction.Verify);

                // Create a File2Stim object and start to run the protocol via its methods
                custprot = new File2Stim3(stimfile, STIM_SAMPLING_FREQ, STIMBUFFSIZE, stimDigitalTask, stimPulseTask, stimFromFileDigitalWriter, stimFromFileAnalogWriter);
                buttonStart.PerformClick();
                custprot.start();
                buttonStop.Enabled = false;
                custprot.AlertProgChanged += new File2Stim3.ProgressChangedHandler(protProgressChangedHandler);
                custprot.AlertAllFinished += new File2Stim3.AllFinishedHandler(protFinisheddHandler);

                progressBar_protocolFromFile.Minimum = 0;
                progressBar_protocolFromFile.Maximum = 100;
                progressBar_protocolFromFile.Value = 0;

            }
        }

        private void button_stopStimFromFile_Click(object sender, EventArgs e)
        {
            button_startStimFromFile.Enabled = true;
            button_stopStimFromFile.Enabled = false;
            custprot.stop();
            buttonStop.Enabled = true;
            buttonStop.PerformClick();

            if (stimDigitalTask != null) { stimDigitalTask.Dispose(); stimDigitalTask = null; }
            if (stimPulseTask != null) { stimPulseTask.Dispose(); stimPulseTask = null; }

            stimDigitalTask = new Task("stimDigitalTask");

            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalTask.DOChannels.CreateChannel(Properties.Settings.Default.StimulatorDevice + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes

            stimDigitalTask.Timing.ConfigureSampleClock("100kHzTimebase", Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, 3);
            stimFromFileDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);

            //De-select channel on mux
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimFromFileDigitalWriter.WriteMultiSamplePort(true, new UInt32[] { 0, 0, 0 });
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimFromFileDigitalWriter.WriteMultiSamplePort(true, new byte[] { 0, 0, 0 });
            //stimDigitalTask.Start();
            stimDigitalTask.WaitUntilDone();
            stimDigitalTask.Stop();
            updateSettings();
        }

        private void protProgressChangedHandler(object sender, int percentage)
        {
            progressBar_protocolFromFile.Value = percentage;
        }

        // Return buttons to default configuration when finished
        private void protFinisheddHandler(object sender)
        {
            buttonStop.Enabled = true;
            progressBar_protocolFromFile.Value = 0;
            button_startStimFromFile.Enabled = true;
            button_stopStimFromFile.Enabled = false;
            MessageBox.Show("Stimulation protocol " + textBox_protocolFileLocations.Text + " is complete. Click Stop to end recording.");
        }

        private bool checkFilePath(string filePath)
        {
            string sourcefile = @filePath;
            bool check = File.Exists(sourcefile);
            return (check);
        }


        private void button_BrowseOLStimFile_Click(object sender, EventArgs e)
        {
            // Set dialog's default properties
            OpenFileDialog OLStimFileDialog = new OpenFileDialog();
            OLStimFileDialog.DefaultExt = "*.olstim";         //default extension is for olstim files
            OLStimFileDialog.Filter = "Open Loop Stim Files|*.olstim|All Files|*.*";

            // Display Save File Dialog (Windows forms control)
            DialogResult result = OLStimFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                filenameOutput = OLStimFileDialog.FileName;
                textBox_protocolFileLocations.Text = filenameOutput;
            }
        }
        #endregion


    }
}