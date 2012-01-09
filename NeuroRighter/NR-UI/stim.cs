// STIM.CS
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
using System.Reflection;
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
using NeuroRighter.Output;



namespace NeuroRighter
{

    ///<summary>Methods controlling stimulation through NeuroRighter.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter
    {
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
                    stimIvsVTask.DOChannels.CreateChannel(Properties.Settings.Default.StimIvsVDevice + "/Port1/line0", "",
                        ChannelLineGrouping.OneChannelForAllLines);
                    stimIvsVWriter = new DigitalSingleChannelWriter(stimIvsVTask.Stream);
                    stimIvsVTask.Control(TaskAction.Verify);
                    stimIvsVWriter.WriteSingleSampleSingleLine(true, false);
                    stimIvsVTask.WaitUntilDone();
                    stimIvsVTask.Stop();
                    stimIvsVTask.Dispose();
                }

                radioButton_impVoltage.Checked = true;
            }
        }
        #endregion

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

        #region Manual Open Loop Stimulation

        internal struct stim_params
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
            // Update stim hardware settings
            UpdateStimulationSettings();

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

            UpdateStimulationSettings();

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

        private double[] ReturnOpenLoopStimPulse()
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

            return pulse;
        }
        #endregion //End DrawStimPulse region

        #region Open Loop Output
        private void button_BrowseOLStimFile_Click(object sender, EventArgs e)
        {
            // Set dialog's default properties
            OpenFileDialog OLFileDialog = new OpenFileDialog();
            OLFileDialog.DefaultExt = "*.olstim";         //default extension is for olstim files
            OLFileDialog.Filter = "Open Loop Stimulation Files|*.olstim|All Files|*.*";
            OLFileDialog.InitialDirectory = Properties.Settings.Default.OLstimdirectory;

            // Display Save File Dialog (Windows forms control)
            DialogResult result = OLFileDialog.ShowDialog();



            if (result == DialogResult.OK)
            {
                string tmp = new FileInfo(OLFileDialog.FileName).DirectoryName;
                Properties.Settings.Default.OLstimdirectory = tmp;
                Properties.Settings.Default.Save();
                filenameOutput = OLFileDialog.FileName;
                textBox_protocolFileLocations.Text = filenameOutput;
            }
        }

        private void button_BrowseOLDFile_Click(object sender, EventArgs e)
        {
            // Set dialog's default properties
            OpenFileDialog OLFileDialog = new OpenFileDialog();
            OLFileDialog.DefaultExt = "*.oldig";         //default extension is for olstim files
            OLFileDialog.Filter = "Open Loop Digital Files|*.oldig|All Files|*.*";
            OLFileDialog.InitialDirectory = Properties.Settings.Default.OLstimdirectory;
            // Display Save File Dialog (Windows forms control)
            DialogResult result = OLFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string tmp = new FileInfo(OLFileDialog.FileName).DirectoryName;
                Properties.Settings.Default.OLstimdirectory = tmp;
                Properties.Settings.Default.Save();
                filenameOutput = OLFileDialog.FileName;
                textBox_digitalProtocolFileLocation.Text = filenameOutput;
            }
        }

        private void button_browseAuxFile_Click(object sender, EventArgs e)
        {
            // Set dialog's default properties
            OpenFileDialog OLFileDialog = new OpenFileDialog();
            OLFileDialog.DefaultExt = "*.olaux";         //default extension is for olstim files
            OLFileDialog.Filter = "Open Loop Auxiliary Files|*.olaux|All Files|*.*";
            OLFileDialog.InitialDirectory = Properties.Settings.Default.OLstimdirectory;
            // Display Save File Dialog (Windows forms control)
            DialogResult result = OLFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string tmp = new FileInfo(OLFileDialog.FileName).DirectoryName;
                Properties.Settings.Default.OLstimdirectory = tmp;
                Properties.Settings.Default.Save();
                filenameOutput = OLFileDialog.FileName;
                textBox_AuxFile.Text = filenameOutput;
            }
        }

        private void button_startStimFromFile_Click(object sender, EventArgs e)
        {
            // Update all the recording/stim settings
            updateSettings();


            // Start the protocol
            numOpenLoopsPerformed = 0;
            numOpenLoopRepeats = (double)numericUpDown_NumberOfOpenLoopRepeats.Value;
            StartOpenLoopCallback();

        }

        private void StartOpenLoopCallback()
        {
            bool openLoopSyncFail;

            // Set up recording so we can access the task info from spikeTask[0] to sync
            // clock and start
            // Set the recording type to non-normal since this is a OL protocol
            isNormalRecording = false;
            NRAcquisitionSetup();

            // Get OL filenames
            string stimFile = textBox_protocolFileLocations.Text;
            string digFile = textBox_digitalProtocolFileLocation.Text;
            string auxFile = textBox_AuxFile.Text;

            // create a syncronized OL Output object
            if (checkBox_useManStimWaveform.Checked)
            {
                double[] stimWaveform = ReturnOpenLoopStimPulse();
                openLoopSynchronizedOutput = new OpenLoopOut(stimFile, digFile, auxFile, STIM_SAMPLING_FREQ, spikeTask[0], Debugger, stimWaveform);
            }
            else
            {
                openLoopSynchronizedOutput = new OpenLoopOut(stimFile, digFile, auxFile, STIM_SAMPLING_FREQ, spikeTask[0], Debugger);
            }

            // Subscribe the stopStimFromFile Click to the event raised when
            // OpenLoopOut is finsihed
            openLoopSynchronizedOutput.OpenLoopOutIsFinished +=
                new OpenLoopOut.OpenLoopOutFinishedEventHandler(FinishOutputFromFile);

            // Start the OL Output exp
            openLoopSyncFail = openLoopSynchronizedOutput.Start();

            if (openLoopSyncFail)
            {
                ResetUIAfterOpenLoopOut(openLoopSyncFail);
                return;
            }

            // Start recording and the master tasks along with it
            NRStartRecording();

            // UI care-taking
            button_startStimFromFile.Enabled = false;
            button_stopStimFromFile.Enabled = true;
            buttonStop.Enabled = false;
        }

        internal void FinishOutputFromFile(object sender, EventArgs e)
        {
            //ResetUIAfterOpenLoopOut(false);

            if (repeatOpenLoopProtocol)
            {
                // Take care of the continuous repeat case
                if (numOpenLoopRepeats == 0)
                {
                    numOpenLoopRepeats = double.PositiveInfinity;
                }

                // increment numOpenLoopsPerformed
                numOpenLoopsPerformed += 1;

                // Stop the recording
                // Invoke an anonymous method on the thread of the form.
                this.Invoke((MethodInvoker)delegate
                {
                    this.buttonStop.Enabled = true;
                    this.button_stopStimFromFile.Enabled = false;
                    this.buttonStop.PerformClick();
                });

                // Decided whether to repeat
                if (numOpenLoopsPerformed < numOpenLoopRepeats)
                {
                    openLoopSynchronizedOutput.OpenLoopOutIsFinished -= FinishOutputFromFile;
                    this.Invoke((MethodInvoker)delegate
                    {
                        StartOpenLoopCallback();
                    });
                }
                else
                {
                    MessageBox.Show("The repeated open loop protocol has finished.");
                    // Invoke an anonymous method on the thread of the form.
                    this.Invoke((MethodInvoker)delegate
                    {
                        this.buttonStop.Enabled = true;
                        this.button_stopStimFromFile.Enabled = false;
                    });
                }
            }
            else
            {
                MessageBox.Show("The open loop protocol finished peacefully. Press Stop to end the recording");
                // Invoke an anonymous method on the thread of the form.
                this.Invoke((MethodInvoker)delegate
                {
                    this.buttonStop.Enabled = true;
                    this.button_stopStimFromFile.Enabled = false;
                });
            }
        }

        internal void ResetUIAfterOpenLoopOut(bool comingFromFail)
        {
            try
            {
                if (openLoopSynchronizedOutput != null)
                {
                    openLoopSynchronizedOutput.StopAllBuffers();
                    System.Threading.Thread.Sleep((int)(Properties.Settings.Default.DACPollingPeriodSec * 2000));
                    openLoopSynchronizedOutput.KillTasks();
                    //openLoopSynchronizedOutput.KillAllAODOTasks();

                }

                //ZeroOutput zeroOpenLoopOutput = new ZeroOutput();

                //int[] analogChannelsToZero = { 0, 1, 2, 3 };
                //zeroOpenLoopOutput.ZeroAOChanOnDev(
                //    Properties.Settings.Default.SigOutDev, analogChannelsToZero);
                //zeroOpenLoopOutput.ZeroAOChanOnDev(
                //    Properties.Settings.Default.StimulatorDevice, analogChannelsToZero);
                //zeroOpenLoopOutput.ZeroPortOnDev(
                //    Properties.Settings.Default.SigOutDev, 0);
                //zeroOpenLoopOutput.ZeroPortOnDev(
                //    Properties.Settings.Default.StimulatorDevice, 0);

                if (comingFromFail)
                {
                    button_startStimFromFile.Enabled = true;
                    reset();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        internal void button_stopStimFromFile_Click(object sender, EventArgs e)
        {
            if (repeatOpenLoopProtocol)
            {
                numOpenLoopRepeats = -1;
            }

            lock (this)
            {
                this.Invoke((MethodInvoker)delegate//this code is executed on the main thread
                {
                    // NROutputShutdown();
                    ResetUIAfterOpenLoopOut(false);
                    this.buttonStop.Enabled = true;
                    buttonStop.PerformClick();
                    Console.WriteLine("Stim, Aux and Digital Outputs were killed mid-process");
                });
            }
        }

        #endregion

        #region Closed Loop Output
        Assembly ClosedLoopLibrary;
        System.AppDomain NewAppDomain;
        //private List<ClosedLoopExperiment> experimentList;
        private void button_BrowseCLStimFile_Click(object sender, EventArgs e)
        {
            try
            {
                // Set dialog's default properties
                OpenFileDialog CLFileDialog = new OpenFileDialog();
                CLFileDialog.DefaultExt = "*.dll";         //default extension is for olstim files
                CLFileDialog.Filter = "library files|*.dll|All Files|*.*";
                CLFileDialog.InitialDirectory = Properties.Settings.Default.CLstimdirectory;

                // Display Save File Dialog (Windows forms control)
                DialogResult result = CLFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    

                    // copy the cl dll to local directory
                    File.Delete("LOCAL_CL_EXP1.dll");
                    File.Copy(CLFileDialog.FileName, "LOCAL_CL_EXP1.dll");

                    string tmp = new FileInfo(CLFileDialog.FileName).DirectoryName;
                    Properties.Settings.Default.CLstimdirectory = tmp;
                    Properties.Settings.Default.Save();
                    textBox_ClosedLoopProtocolFile.Text = CLFileDialog.FileName;

                    FileInfo tmpFi = new FileInfo("LOCAL_CL_EXP1.dll");
                    filenameOutput = tmpFi.FullName;
                    tmpFi = null;

                    // Create a new application domain for the assembly
                    //System.AppDomain NewAppDomain = System.AppDomain.CreateDomain("CLDllDomain",null);
                    //NewAppDomain.Load(filenameOutput);

                    //List<Type> types = new List<Type>();
                    //Type ti = typeof(ClosedLoopExperiment);
                    //foreach (Assembly asm in NewAppDomain.GetAssemblies())
                    //{
                    //    foreach (Type t in asm.GetTypes())
                    //    {
                    //        if (ti.IsAssignableFrom(t))
                    //        {
                    //            types.Add(t);
                    //        }
                    //    }
                    //}


                    //grab the plugins
                    ClosedLoopLibrary = Assembly.LoadFile(filenameOutput);//load the dll as an assembly
                   
                    // Refresh the CL combobox
                    RefreshCLComboBox();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Invalid closed loop DLL selection. Please try again: " + ex.Message);

            }
        }

        private void RefreshCLComboBox()
        {

            Type[] types = ClosedLoopLibrary.GetTypes();//find the invidual classes in the assembly

            // Repopulate the combobox
            comboBox_closedLoopProtocol.Items.Clear();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].BaseType.Equals(typeof(ClosedLoopExperiment)))//find the classes that are implimenting the abstract ClosedLoopExperiment class
                {
                    ClosedLoopExperiment tmpcl = Activator.CreateInstance(types[i]) as ClosedLoopExperiment;
                    //experimentList.Add(tmp);//and activated them as such.
                    Console.WriteLine(tmpcl.ToString());
                    comboBox_closedLoopProtocol.Enabled = true;
                    comboBox_closedLoopProtocol.Items.Add(tmpcl);
                }

                // Clear the text
                comboBox_closedLoopProtocol.Text = "";
            }

        }

        private void button_startClosedLoopStim_Click(object sender, EventArgs e)
        {
            // Update all the recording/stim settings
            updateSettings();

            // Set the recording type to non-normal since this is a OL protocol
            isNormalRecording = false;

            // Start the protocol
            startClosedLoopStim();

        }

        private void startClosedLoopStim()
        {
            if (comboBox_closedLoopProtocol.SelectedItem == null)
            {
                MessageBox.Show("Please select a protocol");
                return;
            }

            comboBox_closedLoopProtocol.Refresh();
            ClosedLoopExperiment CLE = (ClosedLoopExperiment)comboBox_closedLoopProtocol.SelectedItem;// ClosedLoopTest();//new SilentBarrageClosedLoop();//

            //setup
            NRAcquisitionSetup();
            Task BuffLoadTask = NROutputSetup();
            //create closed loop code, throw into it's own thread


            if (checkBox_useManStimWaveformCL.Checked)
            {
                double[] stimWaveform = ReturnOpenLoopStimPulse();
                closedLoopSynchronizedOutput = new ClosedLoopOut(CLE, 100000, datSrv, stimSrv, BuffLoadTask, Debugger, filenameBase, switch_record.Value, stimWaveform);
            }
            else
            {
                closedLoopSynchronizedOutput = new ClosedLoopOut(CLE, 100000, datSrv, stimSrv, BuffLoadTask, Debugger, filenameBase, switch_record.Value);
            }

            closedLoopSynchronizedOutput.Start();

            NRStartRecording();
            //start everything
            //Console.WriteLine(stimSrv);

            //gui stuff
            button_startClosedLoopStim.Enabled = false;
            button_stopClosedLoopStim.Enabled = true;
            buttonStop.Enabled = false;
        }
        private void button_stopClosedLoopStim_Click(object sender, EventArgs e)
        {
            lock (this)
            {
                this.Invoke((MethodInvoker)delegate //this code is executed on the main thread
                {
                    Console.WriteLine("Closed loop stimulation stop initiated");
                    Debugger.Write("Closed loop stimulation stop initiated");
                    closedLoopSynchronizedOutput.Stop();
                    Debugger.Write("closed loop code has indicated it has completed.");
                    NROutputShutdown();
                    Debugger.Write("output buffers successfully shut down.  Goodbye.");
                    this.buttonStop.Enabled = true;
                    buttonStop.PerformClick();
                    Console.WriteLine("Closed loop stimulation closed mid process");

                    // Get rid of old CL objects
                    RefreshCLComboBox();
                });
            }
            

            //AppDomain.Unload(NewAppDomain);

        }
        #endregion

        #region IISZapper

        internal delegate void IISDetectedHandler(object sender, double[][] lfpData, int numReads);
        internal event IISDetectedHandler IISDetected;
        private IISZapper iisZap;

        private void button_IISZapper_start_Click(object sender, EventArgs e)
        {
            button_IISZapper_start.Enabled = false;
            iisZap = new IISZapper((int)numericUpDown_IISZapper_phaseWidth.Value,
                (double)numericUpDown_IISZapper_voltage.Value, (int)numericUpDown_IISZapper_channel.Value,
                (int)numericUpDown_IISZapper_pulsePerTrain.Value, (double)numericUpDown_IISZapper_rate.Value,
                stimDigitalTask, stimPulseTask, stimDigitalWriter, stimPulseWriter, Properties.Settings.Default.ADCPollingPeriodSec,
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
        #endregion

        // lame!
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

                UpdateStimulationSettings();

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
    }
}
