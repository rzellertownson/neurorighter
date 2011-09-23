using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NationalInstruments;
using NationalInstruments.DAQmx;

namespace NeuroRighter.FileWriting
{
    sealed internal partial class RecordingSetup : Form
    {

        // The base file name + parameters
        private string fid;
        private int numElectrodes;

        // These bits determine the streams to be recorded
        internal bool recordSpike;
        internal bool recordRaw;
        internal bool recordSALPA;
        internal bool recordSpikeFilt;
        internal bool recordLFP;
        internal bool recordEEG;
        internal bool recordMUA;
        internal bool recordStim;
        internal bool recordAuxDig;
        internal bool recordAuxAnalog;

        // List of electrodes to write to file
        internal List<int> electrodesToRecord;

        // The file writers
        internal SpikeFileOutput spkOut;
        internal FileOutput rawOut;
        internal FileOutput salpaOut;
        internal FileOutput spkFiltOut;
        internal FileOutput lfpOut;
        internal FileOutput eegOut;
        internal StimFileOutput stimOut;
        internal FileOutput auxAnalogOut;
        internal DigFileOutput auxDigitalOut;

        //

        // Delegates for informing mainform of settings change
        internal delegate void resetRecordingSettingsHandler(object sender, EventArgs e);
        internal event resetRecordingSettingsHandler SettingsHaveChanged;

        public RecordingSetup()
        {
            InitializeComponent();
            Refresh();

            // Set SALPA access to false since the user has not trained yet
            SetSalpaAccess(false);

            // Set recording parameters
            ResetStreams2Record();

            // Reset the electrodes to match number of channels
            ResetElectrodeCheckBox();
        }

        internal void Refresh()
        {
            // Set up access to streams
            checkBox_RecordLFP.Enabled = Properties.Settings.Default.UseLFPs;
            if (!checkBox_RecordLFP.Enabled)
                checkBox_RecordLFP.Checked = false;
            checkBox_RecordEEG.Enabled = Properties.Settings.Default.UseEEG;
            if (!checkBox_RecordEEG.Enabled)
                checkBox_RecordEEG.Checked = false;
            checkBox_RecordStim.Enabled = Properties.Settings.Default.RecordStimTimes;
            if (!checkBox_RecordStim.Enabled)
                checkBox_RecordStim.Checked = false;
            checkBox_RecordMUA.Enabled = false; // TODO: CREATE SUPPORT FOR MUA
            checkBox_RecordAuxAnalog.Enabled = Properties.Settings.Default.useAuxAnalogInput;
            if (!checkBox_RecordAuxAnalog.Enabled)
                checkBox_RecordAuxAnalog.Checked = false;
            checkBox_RecordAuxDig.Enabled = Properties.Settings.Default.useAuxDigitalInput;
            if (!checkBox_RecordAuxDig.Enabled)
                checkBox_RecordAuxDig.Checked = false;

            // Set recording parameters
            ResetStreams2Record();
        }

        internal void SetFID(string fid)
        {
            this.fid = fid;
        }

        internal void SetNumElectrodes(int numElectrodes)
        {
            this.numElectrodes = numElectrodes;
            ResetElectrodeCheckBox();
        }

        // For spike-type streams
        internal void Setup(string dataType, Task dataTask, int numPreSamp, int numPostSamp)
        {
            //Create the nessesary file writers
            switch (dataType)
            {
                case "spk":
                    // Check if we need to create this stream
                    if (recordSpike)
                    {
                        spkOut = new SpikeFileOutput(fid, numElectrodes,
                            (int)dataTask.Timing.SampleClockRate,
                            Convert.ToInt32(numPreSamp + numPostSamp) + 1,
                            dataTask, "." + dataType);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown data type specified during RecordingSetup.Setup()");
                    break;

            }
        }

        // For down-sampled, raw-type streams or streams that are potentially sub-tasks of other tasks
        internal void Setup(string dataType, Task dataTask, int extraInt)
        {
            //Create the nessesary file writers
            switch (dataType)
            {
                case "lfp":
                    // Check if we need to create this stream
                    if (recordLFP)
                    {
                        if (Properties.Settings.Default.SeparateLFPBoard)
                            lfpOut = new FileOutput(fid, numElectrodes, extraInt, 1, dataTask,
                                         "." + dataType, Properties.Settings.Default.PreAmpGain);
                        else
                        {
                            if (numElectrodes == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                                lfpOut = new FileOutputRemapped(fid, numElectrodes, extraInt, 1, dataTask,
                                    "." + dataType, Properties.Settings.Default.PreAmpGain);
                            else
                                lfpOut = new FileOutput(fid, numElectrodes, extraInt, 1, dataTask,
                                    "." + dataType, Properties.Settings.Default.PreAmpGain);
                        }
                    }
                    break;
                case "eeg":
                    // Check if we need to create this stream
                    if (recordEEG)
                    {
                        if (numElectrodes == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                            eegOut = new FileOutputRemapped(fid, numElectrodes, extraInt, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                        else
                            eegOut = new FileOutput(fid, numElectrodes, extraInt, 1, dataTask,
                                    "." + dataType, Properties.Settings.Default.PreAmpGain);
                    }
                    break;
                case "aux":
                    // Check if we need to create this stream
                    if (recordAuxAnalog)
                    {
                        auxAnalogOut = new FileOutput(fid, extraInt,
                            (int)dataTask.Timing.SampleClockRate, 0, dataTask,
                            "." + dataType, 1);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown data type specified during RecordingSetup.Setup()");
                    break;


            }
        }

        // For full sampled streams
        internal void Setup(string dataType, Task dataTask)
        {
            //Create the nessesary file writers
            switch (dataType)
            {
                case "raw":
                    // Check if we need to create this stream
                    if (recordRaw)
                    {
                        if (numElectrodes == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                            rawOut = new FileOutputRemapped(fid, numElectrodes,
                                (int)dataTask.Timing.SampleClockRate, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                        else
                            rawOut = new FileOutput(fid, numElectrodes,
                                (int)dataTask.Timing.SampleClockRate, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                    }
                    break;
                case "salpa":
                    // Check if we need to create this stream
                    if (recordSALPA)
                    {
                        if (numElectrodes == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                            salpaOut = new FileOutputRemapped(fid, numElectrodes,
                                (int)dataTask.Timing.SampleClockRate, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                        else
                            salpaOut = new FileOutput(fid, numElectrodes,
                                (int)dataTask.Timing.SampleClockRate, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                    }
                    break;
                case "spkflt":
                    // Check if we need to create this stream
                    if (recordSpikeFilt)
                    {
                        if (numElectrodes == 64 && Properties.Settings.Default.ChannelMapping == "invitro")
                            spkFiltOut = new FileOutputRemapped(fid, numElectrodes,
                                (int)dataTask.Timing.SampleClockRate, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                        else
                            spkFiltOut = new FileOutput(fid, numElectrodes,
                                (int)dataTask.Timing.SampleClockRate, 1, dataTask,
                                "." + dataType, Properties.Settings.Default.PreAmpGain);
                    }
                    break;
                case "stim":
                    // Check if we need to create this stream
                    if (recordStim)
                    {
                        stimOut = new StimFileOutput(fid, (int)dataTask.Timing.SampleClockRate,
                            "." + dataType);
                    }
                    break;

                case "aux":
                    // Check if we need to create this stream
                    if (recordAuxAnalog)
                    {
                        auxAnalogOut = new FileOutput(fid, dataTask.AIChannels.Count,
                            (int)dataTask.Timing.SampleClockRate, 0, dataTask,
                            "." + dataType, 1);
                    }
                    break;

                case "dig":
                    // Check if we need to create this stream
                    if (recordAuxDig)
                    {
                        auxDigitalOut = new DigFileOutput(fid, (int)dataTask.Timing.SampleClockRate,
                            "." + dataType);
                    }
                    break;

                default:
                    Console.WriteLine("Unknown data type specified during RecordingSetup.Setup()");
                    break;
            }
        }

        // Cleanup
        internal void Flush()
        {
            if (spkOut != null) { spkOut.flush(); spkOut = null; }
            if (rawOut != null) { rawOut.flush(); rawOut = null; }
            if (salpaOut != null) { salpaOut.flush(); salpaOut = null; }
            if (spkFiltOut != null) { spkFiltOut.flush(); spkFiltOut = null; }
            if (lfpOut != null) { lfpOut.flush(); lfpOut = null; };
            if (stimOut != null) { stimOut.flush(); stimOut = null; }
            if (auxAnalogOut != null) { auxAnalogOut.flush(); auxAnalogOut = null; };
            if (auxDigitalOut != null) { auxDigitalOut.flush(); auxDigitalOut = null; }

        }

        internal void SetSalpaAccess(bool recSalpaEnable)
        {
            if (!recSalpaEnable)
                checkBox_RecordSALPA.Checked = false;
            checkBox_RecordSALPA.Enabled = recSalpaEnable;
        }

        internal void SetSpikeFiltAccess(bool recSpikeEnable)
        {
            if (!recSpikeEnable)
                checkBox_RecordSpikeFilt.Checked = false;
            checkBox_RecordSpikeFilt.Enabled = recSpikeEnable;
        }

        internal void RecallDefaultSettings()
        {

            // Recall Form location
            this.Location = Properties.Settings.Default.recSetFormLoc;

            // Load defaults
            if (checkBox_RecordSpikes.Enabled)
                checkBox_RecordSpikes.Checked = Properties.Settings.Default.recordSpikes;
            if (checkBox_RecordSALPA.Enabled)
                checkBox_RecordSALPA.Checked = Properties.Settings.Default.recordSalpa;
            if (checkBox_RecordSpikeFilt.Enabled)
                checkBox_RecordSpikeFilt.Checked = Properties.Settings.Default.recordSpikeFilt;
            if (checkBox_RecordLFP.Enabled)
                checkBox_RecordLFP.Checked = Properties.Settings.Default.recordLFP;
            if (checkBox_RecordEEG.Enabled)
                checkBox_RecordEEG.Checked = Properties.Settings.Default.recordEEG;
            if (checkBox_RecordMUA.Enabled)
                checkBox_RecordMUA.Checked = Properties.Settings.Default.recordMUA;
            if (checkBox_RecordStim.Enabled)
                checkBox_RecordStim.Checked = Properties.Settings.Default.recordStim;
            if (checkBox_RecordAuxAnalog.Enabled)
                checkBox_RecordAuxAnalog.Checked = Properties.Settings.Default.recordAuxAnalog;
            if (checkBox_RecordAuxDig.Enabled)
                checkBox_RecordAuxDig.Checked = Properties.Settings.Default.recordAuxDigital;

            //Recall default electrode settings
            //ResetElectrodeCheckBox();
            //string[] e2RString = Properties.Settings.Default.electrodesToRecord.Split(',');
            //int[] e2R = new int[e2RString.Length];
            //for (int i = 0; i < e2RString.Length; ++i)
            //{
            //    e2R[i] = Convert.ToInt32(e2RString);
            //}
            ////int[] e2R = Properties.Settings.Default.electrodesToRecord.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
            //electrodesToRecord = e2R.ToList();
            //if (electrodesToRecord.Max() <= Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels))
            //{
            //    foreach (int e in electrodesToRecord)
            //    {
            //        checkedListBox_Electrodes.SetItemChecked(e - 1, true);
            //    }
            //}

        }

        internal void MoveToDefaultLocation()
        {
            this.Location = Properties.Settings.Default.recSetFormLoc;
        }

        private void checkBox_RecordRaw_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordSALPA_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordSpikeFilt_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordLFP_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordEEG_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordMUA_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordStim_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordAuxAnalog_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordAuxDig_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void checkBox_RecordSpikes_CheckedChanged(object sender, EventArgs e)
        {
            // Set recording parameters
            ResetStreams2Record();
            SettingsHaveChanged(this, e);
        }

        private void ResetStreams2Record()
        {
            // Set recording parameters
            recordSpike = checkBox_RecordSpikes.Checked;
            recordRaw = checkBox_RecordRaw.Checked;
            recordSALPA = checkBox_RecordSALPA.Checked;
            recordSpikeFilt = checkBox_RecordSpikeFilt.Checked;
            recordLFP = checkBox_RecordLFP.Checked;
            recordEEG = checkBox_RecordEEG.Checked;
            recordMUA = checkBox_RecordMUA.Checked;
            recordStim = checkBox_RecordStim.Checked;
            recordAuxDig = checkBox_RecordAuxDig.Checked;
            recordAuxAnalog = checkBox_RecordAuxAnalog.Checked;
        }

        private void ResetElectrodeCheckBox()
        {
            // On electrodes tab, enable the checkboxes that correspond to the given
            // number of electrodes
            checkedListBox_Electrodes.Items.Clear();
            for (int i = 0; i < Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels); ++i)
            {
                checkedListBox_Electrodes.Items.Add(i + 1, false);
            }
        }

        private void SelectAllElectrodes()
        {
            // On electrodes tab, enable the checkboxes that correspond to the given
            // number of electrodes
            checkedListBox_Electrodes.Items.Clear();
            for (int i = 0; i < Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels); ++i)
            {
                checkedListBox_Electrodes.Items.Add(i + 1, true);
            }
        }

        private void SetElectrodes()
        {
            // Recall default electrode settings
            electrodesToRecord.Clear();
            foreach (int ce in checkedListBox_Electrodes.CheckedIndices)
            {
                electrodesToRecord.Add(ce+1);
            }
        }

        private void button_MakeRawSelections_Click(object sender, EventArgs e)
        {
            // Streams
            Properties.Settings.Default.recordSpikes = checkBox_RecordSpikes.Checked;
            Properties.Settings.Default.recordSalpa = checkBox_RecordSALPA.Checked;
            Properties.Settings.Default.recordSpikeFilt = checkBox_RecordSpikeFilt.Checked;
            Properties.Settings.Default.recordLFP = checkBox_RecordLFP.Checked;
            Properties.Settings.Default.recordEEG = checkBox_RecordEEG.Checked;
            Properties.Settings.Default.recordMUA = checkBox_RecordMUA.Checked;
            Properties.Settings.Default.recordStim = checkBox_RecordStim.Checked;
            Properties.Settings.Default.recordAuxAnalog = checkBox_RecordAuxAnalog.Checked;
            Properties.Settings.Default.recordAuxDigital = checkBox_RecordAuxDig.Checked;

            // Electrodes
            SetElectrodes();
            int[] e2R = electrodesToRecord.ToArray();
            string e2RString = String.Join(",", e2R.Select(i => i.ToString()).ToArray());
            Properties.Settings.Default.electrodesToRecord = e2RString;
            Properties.Settings.Default.Save();

            // Save form location
            Properties.Settings.Default.recSetFormLoc = this.Location;

            this.Hide();
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            // Load defaults
            checkBox_RecordSpikes.Checked = Properties.Settings.Default.recordSpikes;
            checkBox_RecordSALPA.Checked = Properties.Settings.Default.recordSalpa;
            checkBox_RecordSpikeFilt.Checked = Properties.Settings.Default.recordSpikeFilt;
            checkBox_RecordLFP.Checked = Properties.Settings.Default.recordLFP;
            checkBox_RecordEEG.Checked = Properties.Settings.Default.recordEEG;
            checkBox_RecordMUA.Checked = Properties.Settings.Default.recordMUA;
            checkBox_RecordStim.Checked = Properties.Settings.Default.recordStim;
            checkBox_RecordAuxAnalog.Checked = Properties.Settings.Default.recordAuxAnalog;
            checkBox_RecordAuxDig.Checked = Properties.Settings.Default.recordAuxDigital;

            //Recall default electrode settings
            ResetElectrodeCheckBox();
            int[] e2R = Properties.Settings.Default.electrodesToRecord.Split(',').Select(s => Int32.Parse(s)).ToArray();
            electrodesToRecord = e2R.ToList();
            if (electrodesToRecord.Max() <= Convert.ToInt32(Properties.Settings.Default.DefaultNumChannels))
            {
                foreach (int elec in electrodesToRecord)
                {
                    checkedListBox_Electrodes.SetItemChecked(elec - 1, true);
                }
            }

            // Save form location
            Properties.Settings.Default.recSetFormLoc = this.Location;

            this.Hide();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ResetElectrodeCheckBox();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SelectAllElectrodes();
        }

    }
}
