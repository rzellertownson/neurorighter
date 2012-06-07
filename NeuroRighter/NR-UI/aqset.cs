// Copyright (c) 2008-2012 Potter Lab
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
using NeuroRighter.Filters;

namespace NeuroRighter
{
    ///<summary>Methods for setting up filtering, gain settings, digital referencing etc.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter
    {
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

        // Deal with change in gain setting
        private void comboBox_SpikeGain_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkBox_SALPA.Checked = checkBox_SALPA.Enabled = false;
            label_noise.Text = "Noise levels not trained.";
            label_noise.ForeColor = Color.Red;
        }

        // Deal with changes to the number of selected input channels
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
            for (int i = 0; i < Convert.ToInt32(comboBox_numChannels.SelectedItem); ++i)
            {
                listBox_stimChannels.Items.Add(i + 1);
                listBox_exptStimChannels.Items.Add(i + 1);
            }

            //Ensure that sampling rates are okay
            button_lfpSamplingRate_Click(null, null);
            button_spikeSamplingRate_Click(null, null);

            // Reset the spike detector if it exists
            if (spikeDet != null)
            {
                spikeDet.Close();
                spikeDet = new SpikeDetSettings(spikeBufferLength, numChannels);
                spikeDet.SetSpikeDetector(spikeBufferLength);
            }

            resetReferencers();
        }

        // Set LFP sampling rate
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

        // Set spike sampling rate
        private void button_spikeSamplingRate_Click(object sender, EventArgs e)
        {
            try
            {
                //Properties.Settings.Default.ADCPollingPeriodSec = Properties.Settings.Default.ADCPollingPeriodSec;
                int numChannelsPerDevice = (numChannels > 32 ? 32 : numChannels);
                int maxFs = 1000000 / numChannelsPerDevice; //Valid for PCI-6259, not sure about other cards

                int fs = Convert.ToInt32(textBox_spikeSamplingRate.Text);

                // This constraint is worked out in the GUI min/max settings

                //if (fs * Properties.Settings.Default.ADCPollingPeriodSec <= spikeDet.NumPre + spikeDet.NumPost + 1)
                //{
                //    int fsmin = (int)Math.Ceiling((spikeDet.NumPost + spikeDet.NumPre + 1) / Properties.Settings.Default.ADCPollingPeriodSec);
                //    textBox_spikeSamplingRate.Text = Convert.ToString(fsmin);
                //    fs = fsmin;
                //}

                if (fs > 1000000 / numChannelsPerDevice)
                {
                    textBox_spikeSamplingRate.Text = maxFs.ToString();
                    fs = maxFs;
                }

                spikeSamplingRate = fs;

            }
            catch  //This should happen if the user enters something inane
            {
                textBox_spikeSamplingRate.Text = "25000"; //Set to default
            }

            spikeBufferLength = Convert.ToInt32(Properties.Settings.Default.ADCPollingPeriodSec * Convert.ToDouble(textBox_spikeSamplingRate.Text));
            resetReferencers();

            // Reset the spike detector if it exists
            if (spikeDet != null)
                spikeDet.SetSpikeDetector(spikeBufferLength);
        }

        //// Set number of samples to collect after each spike detection
        //private void numPostSamples_ValueChanged(object sender, EventArgs e)
        //{
        //    numPost = Convert.ToInt32(spikeDet.numPostSamples.Value);
        //}

        // Train the SALPA filter
        private void button_Train_Click(object sender, EventArgs e)
        {
            thrSALPA = new rawType[Convert.ToInt32(comboBox_numChannels.SelectedItem)];
            spikeSamplingRate = Convert.ToInt32(textBox_spikeSamplingRate.Text);

            this.Cursor = Cursors.WaitCursor;

            label_noise.Text = "Noise levels not trained.";
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

            // Set reference clock source
            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Timing.ReferenceClockSource = "OnboardClock";

            //Verify the Task
            for (int i = 0; i < spikeTask.Count; ++i)
                spikeTask[i].Control(TaskAction.Verify);

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
            {
                thrSALPA[i] = 9 * (rawType)Statistics.Variance(data[i]) / Math.Pow(Properties.Settings.Default.PreAmpGain, 2);
                Console.Out.WriteLine("channel " + i + ": thr = " + thrSALPA[i]);
            }


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
        // Set up Artifilt
        private void checkBox_artiFilt_CheckedChanged(object sender, EventArgs e)
        {
            //artiFilt = new Filters.ArtiFilt(0.001, 0.002, spikeSamplingRate, numChannels);
            artiFilt = new Filters.ArtiFilt_Interpolation(0.001, 0.002, spikeSamplingRate, numChannels);
        }

        //// Deal with changes to filter parameters
        //private void button_EnableFilterChanges_Click(object sender, EventArgs e)
        //{
        //    MessageBox.Show("The Potter Lab standard settings for spike detection are: \n" +
        //        "\tLow-Cut = 200 Hz\n" +
        //        "\tHigh-Cut = 5000 Hz\n" +
        //        "\tFilter = 2nd order Butterworth\n" +
        //        "Be careful with these settings because they will affect spike\n" +
        //        "detection and detected spike shape.");

        //    checkBox_spikesFilter.Enabled = true;
        //    SpikeLowCut.Enabled = true;
        //    SpikeHighCut.Enabled = true;
        //    SpikeFiltOrder.Enabled = true;
        //    checkBox_LFPsFilter.Enabled = true;
        //    LFPFiltOrder.Enabled = true;
        //    LFPHighCut.Enabled = true;
        //    LFPLowCut.Enabled = true;
        //    checkBox_artiFilt.Enabled = true;
        //    button_DisableFilterChanges.Enabled = true;
        //    button_EnableFilterChanges.Enabled = false;
        //}

        //private void button_DisableFilterChanges_Click(object sender, EventArgs e)
        //{
        //    checkBox_spikesFilter.Enabled = false;
        //    SpikeLowCut.Enabled = false;
        //    SpikeHighCut.Enabled = false;
        //    SpikeFiltOrder.Enabled = false;
        //    checkBox_LFPsFilter.Enabled = false;
        //    LFPFiltOrder.Enabled = false;
        //    LFPHighCut.Enabled = false;
        //    LFPLowCut.Enabled = false;
        //    checkBox_artiFilt.Enabled = false;
        //    button_DisableFilterChanges.Enabled = false;
        //    button_EnableFilterChanges.Enabled = true;
        //}

        private void SpikeLowCut_ValueChanged(object sender, EventArgs e)
        {
            resetSpikeFilter();
        }
        private void checkBox_spikesFilter_CheckedChanged(object sender, EventArgs e)
        {
            resetSpikeFilter();
            recordingSettings.SetSpikeFiltAccess(checkBox_spikesFilter.Checked);
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
        private void numericUpDown_salpa_halfwidth_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_postpeg_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_forcepeg_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_ahead_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }
        private void numericUpDown_salpa_asym_ValueChanged(object sender, EventArgs e)
        {
            resetSALPA();
        }

        private void numericUpDown_MUAHighCut_ValueChanged(object sender, EventArgs e)
        {
            resetMUAFilter();
        }

        private void numericUpDown_MUAFilterOrder_ValueChanged(object sender, EventArgs e)
        {
            resetMUAFilter();
        }


        private void checkBox_SALPA_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SALPA.Checked)
            {
                numericUpDown_salpa_halfwidth.Enabled = false;
                numericUpDown_salpa_postpeg.Enabled = false;
                numericUpDown_salpa_forcepeg.Enabled = false;
                numericUpDown_salpa_ahead.Enabled = false;
                numericUpDown_salpa_asym.Enabled = false;
                resetSALPA();
            }
            else
            {
                numericUpDown_salpa_halfwidth.Enabled = true;
                numericUpDown_salpa_postpeg.Enabled = true;
                numericUpDown_salpa_forcepeg.Enabled = true;
                numericUpDown_salpa_ahead.Enabled = true;
                numericUpDown_salpa_asym.Enabled = true;
            }

            recordingSettings.SetSalpaAccess(checkBox_SALPA.Checked);
        }

        //Reset spike filter
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
                spikeFilter = new ButterworthFilter[numChannels];
                for (int i = 0; i < numChannels; ++i)
                    spikeFilter[i] = new ButterworthFilter((int)SpikeFiltOrder.Value, Convert.ToDouble(textBox_spikeSamplingRate.Text),
                        Convert.ToDouble(SpikeLowCut.Value), Convert.ToDouble(SpikeHighCut.Value), spikeBufferLength);
            }
        }

        //Reset SALPA filter
        private void resetSALPA()
        {
            // Set SALPA parameters

            int asym_sams = Convert.ToInt32(numericUpDown_salpa_asym.Value);
            int blank_sams = Convert.ToInt32(numericUpDown_salpa_postpeg.Value);
            int ahead_sams = Convert.ToInt32(numericUpDown_salpa_ahead.Value); //0.0002 = 5 samples @ 25 kHz
            int forcepeg_sams = Convert.ToInt32(numericUpDown_salpa_forcepeg.Value);
            SALPA_WIDTH = Convert.ToInt32(numericUpDown_salpa_halfwidth.Value);

            //if (4 * SALPA_WIDTH + 1 > spikeBufferLength) // Make sure that the number of samples needed for polynomial fit is not more than the current buffersize
            //{
            //    double max_halfwidth = Convert.ToDouble((spikeBufferLength - 1) / 4);
            //    SALPA_WIDTH = (int)Math.Floor(max_halfwidth);
            //}


            //public SALPA3(int length_sams,int asym_sams,int blank_sams,int ahead_sams, int forcepeg_sams, rawType railLow, rawType railHigh, int numElectrodes, int bufferLength, rawType[] thresh)
            if (thrSALPA == null)
                MessageBox.Show("train salpa before editing parameters");
            else
                SALPAFilter = new global::NeuroRighter.Filters.SALPA3(SALPA_WIDTH, asym_sams, blank_sams, ahead_sams, forcepeg_sams, (rawType)(-4 * Math.Pow(10, -3)),
                    (rawType)(4 * Math.Pow(10, -3)), numChannels, spikeBufferLength, thrSALPA);
            //SALPAFilter = new SALPA3(SALPA_WIDTH, prepeg, postpeg, postpegzero, (rawType)(-10 / Convert.ToDouble(comboBox_SpikeGain.SelectedItem) + 0.01),
            //    (rawType)(10 / Convert.ToDouble(comboBox_SpikeGain.SelectedItem) - 0.01), numChannels, delta, spikeBufferLength);
        }

        //Reset LFP filter
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

        //Reset EEG filter
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

        //Reset MUA filter
        private void resetMUAFilter()
        {
            Properties.Settings.Default.MUAHighCutHz = Convert.ToDouble(numericUpDown_MUAHighCut.Value);
            Properties.Settings.Default.MUAFilterOrder = (int)(numericUpDown_MUAFilterOrder.Value);

            if (muaFilter != null)
            {
                lock (muaFilter)
                {
                    muaFilter.Reset(Properties.Settings.Default.MUAHighCutHz, Properties.Settings.Default.MUAFilterOrder);
                }
            }
            else //lfpFilter is uninitialized
            {
                //lfpFilter = new BesselBandpassFilter[numChannels];
                muaFilter = new Filters.MUAFilter(
                            numChannels, spikeSamplingRate, spikeBufferLength, 
                            Properties.Settings.Default.MUAHighCutHz, 
                            Properties.Settings.Default.MUAFilterOrder, 
                            MUA_DOWNSAMPLE_FACTOR, 
                            Properties.Settings.Default.ADCPollingPeriodSec);
            }
        }

        //Reset dig referencer
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
    }
}
