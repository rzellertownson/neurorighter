// NeuroRighter v0.04
// Copyright (c) 2008 John Rolston
//
// This file is part of NeuroRighter v0.04.
//
// NeuroRighter v0.04 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter v0.04 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter v0.04.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;

namespace NeuroRighter
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    public partial class HardwareSettings : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public HardwareSettings()
        {
            InitializeComponent();

            comboBox_analogInputDevice1.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_analogInputDevice1.Items.Count > 0)
            {
                int idx = comboBox_analogInputDevice1.Items.IndexOf(Properties.Settings.Default.AnalogInDevice[0]);
                if (idx >= 0)
                    comboBox_analogInputDevice1.SelectedIndex = idx;
                else
                    comboBox_analogInputDevice1.SelectedIndex = 0;
            }
            comboBox_analogInputDevice2.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_analogInputDevice2.Items.Count > 0)
            {
                int idx;
                if (Properties.Settings.Default.AnalogInDevice.Count > 1)
                    idx = comboBox_analogInputDevice2.Items.IndexOf(Properties.Settings.Default.AnalogInDevice[1]);
                else
                    idx = -1;
                if (idx >= 0)
                    comboBox_analogInputDevice2.SelectedIndex = idx;
                else
                    comboBox_analogInputDevice2.SelectedIndex = 0;
            }
            comboBox_stimulatorDevice.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_stimulatorDevice.Items.Count > 0)
            {
                int idx = comboBox_stimulatorDevice.Items.IndexOf(Properties.Settings.Default.StimulatorDevice);
                if (idx >= 0)
                    comboBox_stimulatorDevice.SelectedIndex = idx;
                else
                    comboBox_stimulatorDevice.SelectedIndex = 0;
            }
            comboBox_stimInfoDevice.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_stimInfoDevice.Items.Count > 0)
            {
                int idx = comboBox_stimInfoDevice.Items.IndexOf(Properties.Settings.Default.StimInfoDevice);
                if (idx >= 0)
                    comboBox_stimInfoDevice.SelectedIndex = idx;
                else
                    comboBox_stimInfoDevice.SelectedIndex = 0;
            }
            comboBox_cineplexDevice.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_cineplexDevice.Items.Count > 0)
            {
                int idx = comboBox_cineplexDevice.Items.IndexOf(Properties.Settings.Default.CineplexDevice);
                if (idx >= 0)
                    comboBox_cineplexDevice.SelectedIndex = idx;
                else
                    comboBox_cineplexDevice.SelectedIndex = 0;
            }
            comboBox_LFPDevice1.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_LFPDevice1.Items.Count > 0)
            {
                int idx = comboBox_LFPDevice1.Items.IndexOf(Properties.Settings.Default.LFPDevice);
                if (idx >= 0)
                    comboBox_LFPDevice1.SelectedIndex = idx;
                else
                    comboBox_LFPDevice1.SelectedIndex = 0;
            }
            comboBox_LFPDevice2.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_LFPDevice2.Items.Count > 0)
            {
                int idx = comboBox_LFPDevice2.Items.IndexOf(Properties.Settings.Default.LFPDevice);
                if (idx >= 0)
                    comboBox_LFPDevice2.SelectedIndex = idx;
                else
                    comboBox_LFPDevice2.SelectedIndex = 0;
            }
            comboBox_progRefSerialPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            if (comboBox_progRefSerialPort.Items.Count > 0)
            {
                int idx = comboBox_progRefSerialPort.Items.IndexOf(Properties.Settings.Default.SerialPortDevice);
                if (idx >= 0)
                    comboBox_progRefSerialPort.SelectedIndex = idx;
                else
                    comboBox_progRefSerialPort.SelectedIndex = 0;
            }
            comboBox_EEG.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_EEG.Items.Count > 0)
            {
                int idx = comboBox_EEG.Items.IndexOf(Properties.Settings.Default.EEGDevice);
                if (idx >= 0)
                    comboBox_EEG.SelectedIndex = idx;
                else 
                    comboBox_EEG.SelectedIndex = 0;
            }

            checkBox_useCineplex.Checked = Properties.Settings.Default.UseCineplex;
            checkBox_useStimulator.Checked = Properties.Settings.Default.UseStimulator;
            checkBox_recordStimulationInfo.Checked = Properties.Settings.Default.RecordStimTimes;
            checkBox_sepLFPBoard1.Checked = Properties.Settings.Default.SeparateLFPBoard;
            comboBox_LFPDevice1.Enabled = Properties.Settings.Default.SeparateLFPBoard;
            checkBox_useProgRef.Checked = Properties.Settings.Default.UseProgRef;
            comboBox_progRefSerialPort.Enabled = Properties.Settings.Default.UseProgRef;
            checkBox_useEEG.Checked = Properties.Settings.Default.UseEEG;
            comboBox_EEG.Enabled = Properties.Settings.Default.UseEEG;
            comboBox_analogInputDevice2.Enabled = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            checkBox_useSecondBoard.Checked = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            checkBox_sepLFPBoard2.Enabled = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            comboBox_LFPDevice2.Enabled = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);

            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8:
                    radioButton_8Mux.Checked = true;
                    radioButton_16Mux.Checked = false;
                    break;
                case 16:
                    radioButton_8Mux.Checked = false;
                    radioButton_16Mux.Checked = true;
                    break;
            }
            switch (Properties.Settings.Default.StimPortBandwidth)
            {
                case 8:
                    radioButton_8bit.Checked = true;
                    radioButton_32bit.Checked = false;
                    break;
                case 32:
                    radioButton_8bit.Checked = false;
                    radioButton_32bit.Checked = true;
                    break;
            }
        }

        private void button_accept_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.AnalogInDevice.Clear();
            Properties.Settings.Default.AnalogInDevice.Add(Convert.ToString(comboBox_analogInputDevice1.SelectedItem));
            if (checkBox_useSecondBoard.Checked)
                Properties.Settings.Default.AnalogInDevice.Add(Convert.ToString(comboBox_analogInputDevice2.SelectedItem));
            Properties.Settings.Default.UseCineplex = checkBox_useCineplex.Checked;
            Properties.Settings.Default.UseStimulator = checkBox_useStimulator.Checked;
            Properties.Settings.Default.RecordStimTimes = checkBox_recordStimulationInfo.Checked;
            Properties.Settings.Default.SeparateLFPBoard = checkBox_sepLFPBoard1.Checked;
            Properties.Settings.Default.UseProgRef = checkBox_useProgRef.Checked;
            Properties.Settings.Default.UseEEG = checkBox_useEEG.Checked;
            if (checkBox_sepLFPBoard1.Checked)
                Properties.Settings.Default.LFPDevice = Convert.ToString(comboBox_LFPDevice1.SelectedItem);
            //if (checkBox_sepLFPBoard2.Checked)
                
            if (checkBox_useCineplex.Checked)
                Properties.Settings.Default.CineplexDevice = Convert.ToString(comboBox_cineplexDevice.SelectedItem);
            if (checkBox_useStimulator.Checked)
                Properties.Settings.Default.StimulatorDevice = Convert.ToString(comboBox_stimulatorDevice.SelectedItem);
            if (checkBox_recordStimulationInfo.Checked)
                Properties.Settings.Default.StimInfoDevice = Convert.ToString(comboBox_stimInfoDevice.SelectedItem);
            if (checkBox_useProgRef.Checked)
                Properties.Settings.Default.SerialPortDevice = Convert.ToString(comboBox_progRefSerialPort.SelectedItem);
            if (checkBox_useEEG.Checked)
                Properties.Settings.Default.EEGDevice = Convert.ToString(comboBox_EEG.SelectedItem);
            if (radioButton_8Mux.Checked)
                Properties.Settings.Default.MUXChannels = 8;
            else
                Properties.Settings.Default.MUXChannels = 16;
            if (radioButton_8bit.Checked) { Properties.Settings.Default.StimPortBandwidth = 8; }
            else { Properties.Settings.Default.StimPortBandwidth = 32; }
            Properties.Settings.Default.NumAnalogInDevices = (short)Properties.Settings.Default.AnalogInDevice.Count;

            Properties.Settings.Default.Save();
            this.Close();
        }


        private void checkBox_useStimulator_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_useStimulator.Checked) comboBox_stimulatorDevice.Enabled = true;
            else comboBox_stimulatorDevice.Enabled = false;
        }

        private void checkBox_useCineplex_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_useCineplex.Checked) comboBox_cineplexDevice.Enabled = true;
            else comboBox_cineplexDevice.Enabled = false;
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkBox_sepLFPBoard_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_LFPDevice1.Enabled = checkBox_sepLFPBoard1.Checked;
        }

        private void checkBox_useProgRef_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_progRefSerialPort.Enabled = checkBox_useProgRef.Checked;
        }

        private void checkBox_useEEG_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_EEG.Enabled = checkBox_useEEG.Checked;
        }

        private void comboBox_LFPs_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_8Mux_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_8Mux.Checked)
                radioButton_16Mux.Checked = false;
        }

        private void radioButton_16Mux_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_16Mux.Checked)
                radioButton_8Mux.Checked = false;
        }

        private void radioButton_8bit_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_8bit.Checked)
                radioButton_32bit.Checked = false;
        }

        private void radioButton_32bit_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_32bit.Checked)
                radioButton_8bit.Checked = false;
        }

        private void checkBox_useSecondBoard_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_analogInputDevice2.Enabled = checkBox_useSecondBoard.Checked;
            comboBox_LFPDevice2.Enabled = checkBox_useSecondBoard.Checked;
            checkBox_sepLFPBoard2.Enabled = checkBox_useSecondBoard.Checked;
        }

        private void checkBox_useStimulator_CheckedChanged_1(object sender, EventArgs e)
        {
            comboBox_stimulatorDevice.Enabled = checkBox_useStimulator.Checked;
            if (!checkBox_useStimulator.Checked) checkBox_recordStimulationInfo.Checked = false;
            checkBox_recordStimulationInfo.Enabled = checkBox_useStimulator.Checked;
            comboBox_stimInfoDevice.Enabled = checkBox_useStimulator.Checked;
            radioButton_16Mux.Enabled = checkBox_useStimulator.Checked;
            radioButton_32bit.Enabled = checkBox_useStimulator.Checked;
            radioButton_8bit.Enabled = checkBox_useStimulator.Checked;
            radioButton_8Mux.Enabled = checkBox_useStimulator.Checked;
        }

        private void checkBox_useCineplex_CheckedChanged_1(object sender, EventArgs e)
        {
            comboBox_cineplexDevice.Enabled = checkBox_useCineplex.Checked;
        }

        private void checkBox_useProgRef_CheckedChanged_1(object sender, EventArgs e)
        {
            comboBox_progRefSerialPort.Enabled = checkBox_useProgRef.Checked;
        }
    }
}