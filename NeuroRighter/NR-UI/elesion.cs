// ELESION.CS
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
using rawType = System.Double;
using NationalInstruments.Analysis.Dsp.Filters;
using csmatio.types;
using NeuroRighter.Output;

namespace NeuroRighter
{
    ///<summary>Declarations for the NeuroRighter UI.</summary>
    ///<author>Jon Newman</author>
    sealed internal partial class NeuroRighter
    {
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

    }
}
