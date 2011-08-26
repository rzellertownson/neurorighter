// FORMX.CS
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
using NeuroRighter.SpikeDetection;

namespace NeuroRighter
{

    ///<summary>Methods for dealing with menus and tabs in NR mainform. Also deals with closing the mainform.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HardwareSettings nc_s = new HardwareSettings();
            nc_s.ShowDialog();
            updateSettings();

            //NRAcquisitionSetup();

            spikeDet = new SpikeDetSettings(spikeBufferLength, numChannels, spikeSamplingRate);
            spikeDet.SettingsHaveChanged += new SpikeDetSettings.resetSpkDetSettingsHandler(spikeDet_SettingsHaveChanged);
            spikeDet.SetSpikeDetector();

            

        }

        private void toolStripMenuItem_DisplaySettings_Click(object sender, EventArgs e)
        {
            DisplaySettings ds = new DisplaySettings();
            ds.ShowDialog();
        }

        //private void processingSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    ProcessingSettings ps = new ProcessingSettings();
        //    ps.ShowDialog();
        //    updateSettings();
        //}

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        // Called when the user exits any manu where aspects of the hardware or GUI interface could have been changed
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reset();
            if (Properties.Settings.Default.UseStimulator)
            {
                updateSettings();
            }

            if (spikeGraph != null) { spikeGraph.Dispose(); spikeGraph = null; }
            if (lfpGraph != null) { lfpGraph.Dispose(); lfpGraph = null; }
            if (spkWfmGraph != null) { spkWfmGraph.Dispose(); spkWfmGraph = null; }

            this.Close();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            {
                //case 0: //Spike graph
                case "Spikes":
                    updateAuxGraph = false;
                    spikeGraph.Visible = true;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                //case 1: //Waveform graph
                case "Spk Wfms":
                    updateAuxGraph = false;
                    spkWfmGraph.Visible = true;
                    spikeGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                //case 2: //LFP Graph
                case "LFPs":
                    updateAuxGraph = false;
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                //case 3: //EEG Graph
                case "EEG":
                    updateAuxGraph = false;
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    eegGraph.Visible = true;
                    break;
                case "MUA":
                    updateAuxGraph = false;
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    muaGraph.Visible = true;
                    break;
                case "Aux Input":
                    updateAuxGraph = true;
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    if (eegGraph != null) eegGraph.Visible = false;
                    if (muaGraph != null) muaGraph.Visible = false;
                    break;
                default:
                    spikeGraph.Visible = false;
                    spkWfmGraph.Visible = false;
                    break;
            }
        }

        // Toggle file writing
        private void switch_record_StateChanged(object sender, ActionEventArgs e)
        {
            if (switch_record.Value)
                led_recording.Value = true;
            else
                led_recording.Value = false;
        }

        private void checkBox_RepeatOpenLoopProtocol_CheckedChanged(object sender, EventArgs e)
        {
            // Take away/giveback control of main repeat record checkbox
            if (checkBox_RepeatOpenLoopProtocol.Checked)
            {
                checkbox_repeatRecord.Checked = true;
                checkBox_enableTimedRecording.Checked = false;
                checkBox_enableTimedRecording.Enabled = false;
            }
            else
            {
                checkBox_enableTimedRecording.Enabled = true;
                checkbox_repeatRecord.Checked = false;
            }
            // Set the repeatOpenLoopProtocol bool and numOpenLoopRepeats int
            repeatOpenLoopProtocol = checkBox_RepeatOpenLoopProtocol.Checked;
            numOpenLoopRepeats = (double)numericUpDown_NumberOfOpenLoopRepeats.Value;
        }

        private void NeuroControl_FormClosing(object sender, FormClosingEventArgs e)
        {

            // Reset recording tasks
            if (taskRunning) { reset(); }

            // Reset stimulation tasks
            if (Properties.Settings.Default.UseStimulator) { resetStim(); }

            if (spikeGraph != null) { spikeGraph.Dispose(); spikeGraph = null; }
            if (lfpGraph != null) { lfpGraph.Dispose(); lfpGraph = null; }
            if (spkWfmGraph != null) { spkWfmGraph.Dispose(); spkWfmGraph = null; }
            if (openLoopSynchronizedOutput != null)
            {
                openLoopSynchronizedOutput.KillAllAODOTasks();
            }

            // Close the recording and spike detection forms
            spikeDet.Close();
            recordingSettings.Close();

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

            //Save Settings
            Properties.Settings.Default.Save();
        }

    }
}
