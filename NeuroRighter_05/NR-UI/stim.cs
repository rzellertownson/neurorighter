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
            // Update stim hardware settings
            updateStimSettings();

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

        private double[] returnOpenLoopStimPulse()
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

        private void button_BrowseOLDFile_Click(object sender, EventArgs e)
        {
            // Set dialog's default properties
            OpenFileDialog OLDigFileDialog = new OpenFileDialog();
            OLDigFileDialog.DefaultExt = "*.oldig";         //default extension is for olstim files
            OLDigFileDialog.Filter = "Open Loop Digital Files|*.oldig|All Files|*.*";

            // Display Save File Dialog (Windows forms control)
            DialogResult result = OLDigFileDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                filenameOutput = OLDigFileDialog.FileName;
                textBox_digitalProtocolFileLocation.Text = filenameOutput;
            }
        }

        private void button_startStimFromFile_Click(object sender, EventArgs e)
        {
            updateSettings();
            // Get info off of open-loop stimulation form
            bool useManStimWave = checkBox_useManStimWaveform.Checked;
            string stimfile = textBox_protocolFileLocations.Text;
            string digfile = textBox_digitalProtocolFileLocation.Text;
            bool stimFileProvided = stimfile.Length > 0;
            bool digFileProvided = digfile.Length > 0;


            // Make sure that the user provided a file of some sort
            if (!stimFileProvided && !digFileProvided)
            {
                MessageBox.Show("You need to provide a *.olstim and/or a *.oldig to use the open-loop stimulator.");
                return;
            }

            // Make sure that the user has input a valid file path for the stimulation file
            if (stimFileProvided && !checkFilePath(stimfile))
            {
                MessageBox.Show("The *.olstim file provided does not exist");
                return;
            }

            // Make sure that the user has input a valid file path for the digital file
            if (digFileProvided && !checkFilePath(digfile))
            {
                MessageBox.Show("The *.oldig file provided does not exist");
                return;
            }

            // Prep for take-off
            button_startStimFromFile.Enabled = false;
            button_stopStimFromFile.Enabled = true;

            // This task will govern the periodicity of DAQ circular-buffer loading so that
            // all digital and stimulus output from the system is hardware timed
            configureCounter();

            // Set up stimulus output support
            if (stimFileProvided)
            {
                #region If the user provided a .olstim file

                if (!Properties.Settings.Default.UseStimulator)
                {
                    MessageBox.Show("You must use configure your hardware to use NeuroRighter's Stimulator for this feature");
                    return;
                }

                configureStim();

                // Create a File2Stim object and start to run the protocol via its methods
                if (useManStimWave)
                {
                    double[] waveform = returnOpenLoopStimPulse();
                    OLStimProtocol = new File2Stim4(stimfile, STIM_SAMPLING_FREQ, STIMBUFFSIZE, stimDigitalTask, stimPulseTask, buffLoadTask, stimDigitalWriter, stimPulseWriter, waveform);
                }
                else
                {
                    OLStimProtocol = new File2Stim4(stimfile, STIM_SAMPLING_FREQ, STIMBUFFSIZE, stimDigitalTask, stimPulseTask, buffLoadTask, stimDigitalWriter, stimPulseWriter);
                }

                OLStimProtocol.AlertProgChanged += new File2Stim4.ProgressChangedHandler(protProgressChangedHandler);
                OLStimProtocol.AlertAllFinished += new File2Stim4.AllFinishedHandler(protFinisheddHandler);
                OLStimProtocol.setup();
                #endregion
            }

            // Set up digital output support
            if (digFileProvided)
            {
                #region If the user provided a .oldig file
                if (!Properties.Settings.Default.UseDO)
                {
                    MessageBox.Show("You must use configure your hardware to use NeuroRighter's digital output to use this feature");
                    return;
                }

                configureDig(stimFileProvided);
                OLDigitalProtocol = new File2Dig(digfile, STIM_SAMPLING_FREQ, STIMBUFFSIZE, digitalOutputTask, buffLoadTask, digitalOutputWriter);
                OLDigitalProtocol.setup();
                #endregion
            }

            // Start NeuroRighter's visual/recording functions
            buttonStart.PerformClick();

            // Start the master load syncing task
            buffLoadTask.Start();

            // Start the output tasks
            if (digFileProvided && stimFileProvided)
            {
                OLStimProtocol.start();
                OLDigitalProtocol.start();
                
            }
            else if (digFileProvided)
                OLDigitalProtocol.start();
            else if (stimFileProvided)
                OLStimProtocol.start();

            // UI care-taking
            buttonStop.Enabled = false;
            progressBar_protocolFromFile.Minimum = 0;
            progressBar_protocolFromFile.Maximum = 100;
            progressBar_protocolFromFile.Value = 0;
        }

        private void button_stopStimFromFile_Click(object sender, EventArgs e)
        {
            buttonStop.PerformClick();
            button_stopStimFromFile.Enabled = false;
            if (textBox_protocolFileLocations.Text.Length > 0) { OLStimProtocol.stop(); updateStim(); }
            if (textBox_digitalProtocolFileLocation.Text.Length > 0) { OLDigitalProtocol.stop(); updateDig(); }

            if (buffLoadTask != null) { buffLoadTask.Dispose(); buffLoadTask = null; }
            buttonStop.Enabled = true;
        }

        private void protProgressChangedHandler(object sender, EventArgs e, int percentage)
        {
            Console.WriteLine("Percent complete : " + percentage);
            updateProgressPercentage(percentage);
        }

        private void updateProgressPercentage(int percentage)
        {
            if (progressBar_protocolFromFile.InvokeRequired)
            {
                crossThreadFormUpdateDelegate del = updateProgressPercentage;
                progressBar_protocolFromFile.Invoke(del, new object[] { percentage });
            }
            else
            {
                progressBar_protocolFromFile.Value = percentage;
            }

        }

        private void protFinisheddHandler(object sender, EventArgs e)
        {
            // Return buttons to default configuration when finished
            updateProgressPercentage(0);
            MessageBox.Show("Stimulation protocol " + textBox_protocolFileLocations.Text + " is complete. Click Stop to end recording.");
            buttonStop.Enabled = true;
            button_startStimFromFile.Enabled = true;
            button_stopStimFromFile.Enabled = false;
        }

        private bool checkFilePath(string filePath)
        {
            string sourcefile = @filePath;
            bool check = File.Exists(sourcefile);
            return (check);
        }

        private void configureCounter()
        {
            //configure counter
            if (buffLoadTask != null) { buffLoadTask.Dispose(); buffLoadTask = null; }

            buffLoadTask = new Task("stimBufferTask");
            // Trigger a load event off every edge of this channel
            buffLoadTask.COChannels.CreatePulseChannelFrequency(Properties.Settings.Default.DODevice + "/ctr1",
                "BufferLoadCounter", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, ((double)STIM_SAMPLING_FREQ / (double)STIMBUFFSIZE) / 2.0, 0.5);
            buffLoadTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);
            buffLoadTask.SynchronizeCallbacks = false;
            buffLoadTask.Timing.ReferenceClockSource = "OnboardClock";
            buffLoadTask.Control(TaskAction.Verify);
        }

        private void configureStim()
        {

            //configure stim
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

            // Setup the AO task for continuous stimulaiton
            stimPulseTask.Timing.ConfigureSampleClock("100kHzTimeBase", Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, STIMBUFFSIZE);
            stimPulseTask.SynchronizeCallbacks = false;
            stimPulseTask.Control(TaskAction.Verify);

            // Setup the DO task for continuous stimulaiton
            stimDigitalTask.Timing.ConfigureSampleClock(stimPulseTask.Timing.SampleClockSource, Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, STIMBUFFSIZE);
            stimDigitalTask.SynchronizeCallbacks = false;
            stimDigitalTask.Control(TaskAction.Verify);

            stimPulseWriter = new AnalogMultiChannelWriter(stimPulseTask.Stream);
            stimDigitalWriter = new DigitalSingleChannelWriter(stimDigitalTask.Stream);
            stimPulseTask.Control(TaskAction.Verify);
            stimDigitalTask.Control(TaskAction.Verify);

        }

        private void configureDig(bool usingOLStim)
        {

            // Refresh digital output DAQ task
            if (digitalOutputTask != null) { digitalOutputTask.Dispose(); digitalOutputTask = null; }

            // Create new DAQ tasks and corresponding writers
            digitalOutputTask = new Task("digitalOutputTask");

            //  Create an Digital Output channel and name it.
            digitalOutputTask.DOChannels.CreateChannel(Properties.Settings.Default.DODevice + "/Port0/line0:31", "Generic Digital Out",
                ChannelLineGrouping.OneChannelForAllLines);

            // Setup DO tasks for continuous output
            if (!usingOLStim)
            {
                digitalOutputTask.Timing.ConfigureSampleClock("100kHzTimeBase", Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, STIMBUFFSIZE);
            }
            else
            {
                // Reference Clock source
                string RefClkSource = "/" + Properties.Settings.Default.StimulatorDevice + "/" + stimPulseTask.Timing.ReferenceClockSource;
                double RefClckRate = stimPulseTask.Timing.ReferenceClockRate;

                // Sample Clock source
                string SampClkSource = stimPulseTask.Timing.SampleClockSource;

                // Set up DO sample clock
                digitalOutputTask.Timing.ConfigureSampleClock(SampClkSource, Convert.ToDouble(STIM_SAMPLING_FREQ), SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, STIMBUFFSIZE);
            }
            digitalOutputTask.SynchronizeCallbacks = false;

            // Create writer
            digitalOutputWriter = new DigitalSingleChannelWriter(digitalOutputTask.Stream);

            // Verify Task
            digitalOutputTask.Control(TaskAction.Verify);

        }

        //private void configureAO()
        //{
        //    if (generalAnalogOutTask != null) { generalAnalogOutTask.Dispose(); generalAnalogOutTask = null; }

        //}
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
