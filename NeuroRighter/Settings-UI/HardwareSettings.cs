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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
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
        /// NeuroRighter hardware settings.
        /// </summary>
        public HardwareSettings()
        {

            InitializeComponent();

            comboBox_analogInputDevice1.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_analogInputDevice1.Items.Count > 0)
            {
                int idx=0;
                if (Properties.Settings.Default.AnalogInDevice!=null)
                    idx = comboBox_analogInputDevice1.Items.IndexOf(Properties.Settings.Default.AnalogInDevice[0]);
                
                if (idx >= 0)
                    comboBox_analogInputDevice1.SelectedIndex = idx;
                else
                    comboBox_analogInputDevice1.SelectedIndex = 0;
            }
            comboBox_analogInputDevice2.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_analogInputDevice2.Items.Count > 0)
            {
                int idx = -1;
                if (Properties.Settings.Default.AnalogInDevice!=null)
                    if (Properties.Settings.Default.AnalogInDevice.Count > 1)
                        idx = comboBox_analogInputDevice2.Items.IndexOf(Properties.Settings.Default.AnalogInDevice[1]);
                
                    
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
            comboBox_stimInfoDev.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_stimInfoDev.Items.Count > 0)
            {
                int idx = comboBox_stimInfoDev.Items.IndexOf(Properties.Settings.Default.StimInfoDevice);
                if (idx >= 0)
                    comboBox_stimInfoDev.SelectedIndex = idx;
                else
                    comboBox_stimInfoDev.SelectedIndex = 0;
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
            comboBox_impedanceDevice.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_impedanceDevice.Items.Count > 0)
            {
                int idx = comboBox_impedanceDevice.Items.IndexOf(Properties.Settings.Default.ImpedanceDevice);
                if (idx >= 0)
                    comboBox_impedanceDevice.SelectedIndex = idx;
                else
                    comboBox_impedanceDevice.SelectedIndex = 0;
            }
            comboBox_singleChannelPlaybackDevice.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_singleChannelPlaybackDevice.Items.Count > 0)
            {
                int idx = comboBox_singleChannelPlaybackDevice.Items.IndexOf(Properties.Settings.Default.SingleChannelPlaybackDevice);
                if (idx >= 0)
                    comboBox_singleChannelPlaybackDevice.SelectedIndex = idx;
                else
                    comboBox_singleChannelPlaybackDevice.SelectedIndex = 0;
            }
            comboBox_IVControlDevice.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_IVControlDevice.Items.Count > 0)
            {
                int idx = comboBox_IVControlDevice.Items.IndexOf(Properties.Settings.Default.StimIvsVDevice);
                if (idx >= 0)
                    comboBox_IVControlDevice.SelectedIndex = idx;
                else
                    comboBox_IVControlDevice.SelectedIndex = 0;
            }

            comboBox_SigOutDev.Items.AddRange(DaqSystem.Local.Devices);
            if (comboBox_SigOutDev.Items.Count > 0)
            {
                int idx = comboBox_SigOutDev.Items.IndexOf(Properties.Settings.Default.SigOutDev);
                if (idx >= 0)
                    comboBox_SigOutDev.SelectedIndex = idx;
                else
                    comboBox_SigOutDev.SelectedIndex = 0;
            }

            comboBox_AuxAnalogInputDevice.Items.AddRange
                (DaqSystem.Local.Devices);
            if (comboBox_AuxAnalogInputDevice.Items.Count > 0)
            {
                int idx = comboBox_AuxAnalogInputDevice.Items.IndexOf(Properties.Settings.Default.auxAnalogInDev);
                if (idx >= 0)
                    comboBox_AuxAnalogInputDevice.SelectedIndex = idx;
                else
                    comboBox_AuxAnalogInputDevice.SelectedIndex = 0;
            }

            listBox_AuxAnalogInChan.Items.AddRange
                (DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI,PhysicalChannelAccess.External));
            for (int i = listBox_AuxAnalogInChan.Items.Count-1; i >= 0 ; --i)
            {
                string tempChan = listBox_AuxAnalogInChan.Items[i].ToString();
                string[] whatDev = tempChan.Split('/');
                if (!((string)whatDev[0] == comboBox_AuxAnalogInputDevice.SelectedItem.ToString()))
                    listBox_AuxAnalogInChan.Items.RemoveAt(i);
            }

            if (Properties.Settings.Default.auxAnalogInChan != null)
            {
                for (int i = 0; i < Properties.Settings.Default.auxAnalogInChan.Count; ++i)
                {
                    int idx = listBox_AuxAnalogInChan.Items.IndexOf(Properties.Settings.Default.auxAnalogInChan[i]);
                    if (idx >=0)
                        listBox_AuxAnalogInChan.SetSelected(idx, true);
                    else
                        listBox_AuxAnalogInChan.SetSelected(0, true);
                }
            }

            comboBox_AuxDigInputPort.Items.AddRange
                (DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.DIPort, PhysicalChannelAccess.External));
            
            // Remove ports 1 and 2
            for (int i = comboBox_AuxDigInputPort.Items.Count - 1; i >= 0; --i)
            {
                string tempChan = comboBox_AuxDigInputPort.Items[i].ToString();
                string[] splitPort = tempChan.Split('/');
                string whatPort = splitPort[1];
                char portNo = whatPort[whatPort.Length-1];
                if (! (portNo == '0'))
                    comboBox_AuxDigInputPort.Items.RemoveAt(i);
            }

            if (comboBox_AuxDigInputPort.Items.Count > 0)
            {
                int idx = comboBox_AuxDigInputPort.Items.IndexOf(Properties.Settings.Default.auxDigitalInPort);
                if (idx >= 0)
                    comboBox_AuxDigInputPort.SelectedIndex = idx;
                else
                    comboBox_AuxDigInputPort.SelectedIndex = 0;
            }

            // set control states
            comboBox_stimulatorDevice.Enabled = checkBox_useStimulator.Checked;
            if (!checkBox_useStimulator.Checked) checkBox_RecStimTimes.Checked = false;
            checkBox_RecStimTimes.Enabled = checkBox_useStimulator.Checked;
            comboBox_stimInfoDev.Enabled = checkBox_useStimulator.Checked;
            radioButton_16Mux.Enabled = checkBox_useStimulator.Checked;
            radioButton_32bit.Enabled = checkBox_useStimulator.Checked;
            radioButton_8bit.Enabled = checkBox_useStimulator.Checked;
            radioButton_8Mux.Enabled = checkBox_useStimulator.Checked;
            numericUpDown_ADCPollingPeriodSec.Value = (decimal)Properties.Settings.Default.ADCPollingPeriodSec;
            numericUpDown_DACPollingPeriodSec.Value = (decimal)Properties.Settings.Default.DACPollingPeriodSec;
            numericUpDown_datSrvBufferSizeSec.Value = (decimal)Properties.Settings.Default.datSrvBufferSizeSec;
            numericUpDown_PreAmpGain.Value = (decimal)Properties.Settings.Default.PreAmpGain;
            checkBox_useCineplex.Checked = Properties.Settings.Default.UseCineplex;
            checkBox_useStimulator.Checked = Properties.Settings.Default.UseStimulator;
            checkBox_RecStimTimes.Checked = Properties.Settings.Default.RecordStimTimes;
            checkBox_sepLFPBoard1.Checked = Properties.Settings.Default.SeparateLFPBoard;
            comboBox_LFPDevice1.Enabled = Properties.Settings.Default.SeparateLFPBoard;
            checkBox_useProgRef.Checked = Properties.Settings.Default.UseProgRef;
            comboBox_progRefSerialPort.Enabled = Properties.Settings.Default.UseProgRef;
            checkBox_useEEG.Checked = Properties.Settings.Default.UseEEG;
            checkBox_processLFPs.Checked = Properties.Settings.Default.UseLFPs;
            checkBox_processMUA.Checked = Properties.Settings.Default.ProcessMUA;
            comboBox_EEG.Enabled = Properties.Settings.Default.UseEEG;
            checkBox_UseAODO.Checked = Properties.Settings.Default.UseSigOut;
            checkBox_useChannelPlayback.Checked = Properties.Settings.Default.UseSingleChannelPlayback;
            comboBox_singleChannelPlaybackDevice.Enabled = (Properties.Settings.Default.UseSingleChannelPlayback ? true : false);
            comboBox_analogInputDevice2.Enabled = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            checkBox_useSecondBoard.Checked = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            checkBox_sepLFPBoard2.Enabled = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            comboBox_LFPDevice2.Enabled = (Properties.Settings.Default.NumAnalogInDevices == 2 ? true : false);
            checkBox_UseAuxAnalogInput.Checked = Properties.Settings.Default.useAuxAnalogInput;
            comboBox_AuxAnalogInputDevice.Enabled = Properties.Settings.Default.useAuxAnalogInput;
            listBox_AuxAnalogInChan.Enabled = Properties.Settings.Default.useAuxAnalogInput;
            checkBox_UseAuxDigitalInput.Checked = Properties.Settings.Default.useAuxDigitalInput;
            comboBox_AuxDigInputPort.Enabled = Properties.Settings.Default.useAuxDigitalInput;

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
            // Recording selections
            Properties.Settings.Default.datSrvBufferSizeSec = (double)numericUpDown_datSrvBufferSizeSec.Value;
            Properties.Settings.Default.DACPollingPeriodSec = (double)numericUpDown_DACPollingPeriodSec.Value;
            Properties.Settings.Default.ADCPollingPeriodSec = (double)numericUpDown_ADCPollingPeriodSec.Value;
            Properties.Settings.Default.AnalogInDevice = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.AnalogInDevice.Clear();
            Properties.Settings.Default.auxAnalogInChan = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.auxAnalogInChan.Clear();
            Properties.Settings.Default.PreAmpGain = (double)numericUpDown_PreAmpGain.Value;
            Properties.Settings.Default.AnalogInDevice.Add(Convert.ToString(comboBox_analogInputDevice1.SelectedItem));
            Properties.Settings.Default.UseCineplex = checkBox_useCineplex.Checked;
            Properties.Settings.Default.UseStimulator = checkBox_useStimulator.Checked;
            Properties.Settings.Default.RecordStimTimes = checkBox_RecStimTimes.Checked;
            Properties.Settings.Default.SeparateLFPBoard = checkBox_sepLFPBoard1.Checked;
            Properties.Settings.Default.UseProgRef = checkBox_useProgRef.Checked;
            Properties.Settings.Default.UseEEG = checkBox_useEEG.Checked;
            Properties.Settings.Default.UseSingleChannelPlayback = checkBox_useChannelPlayback.Checked;
            Properties.Settings.Default.UseSigOut = checkBox_UseAODO.Checked;
            Properties.Settings.Default.useAuxAnalogInput = checkBox_UseAuxAnalogInput.Checked;
            Properties.Settings.Default.useAuxDigitalInput = checkBox_UseAuxDigitalInput.Checked;
            Properties.Settings.Default.UseLFPs = checkBox_processLFPs.Checked;
            Properties.Settings.Default.ProcessMUA = checkBox_processMUA.Checked;
            
            
            // Set up devices
            if (checkBox_useSecondBoard.Checked)
                Properties.Settings.Default.AnalogInDevice.Add(Convert.ToString(comboBox_analogInputDevice2.SelectedItem));
            if (checkBox_useChannelPlayback.Checked)
                Properties.Settings.Default.SingleChannelPlaybackDevice = Convert.ToString(comboBox_singleChannelPlaybackDevice.SelectedItem);
            if (checkBox_sepLFPBoard1.Checked)
                Properties.Settings.Default.LFPDevice = Convert.ToString(comboBox_LFPDevice1.SelectedItem);   
            if (checkBox_useCineplex.Checked)
                Properties.Settings.Default.CineplexDevice = Convert.ToString(comboBox_cineplexDevice.SelectedItem);
            if (checkBox_useStimulator.Checked)
                Properties.Settings.Default.StimulatorDevice = Convert.ToString(comboBox_stimulatorDevice.SelectedItem);
            if (checkBox_RecStimTimes.Checked)
                Properties.Settings.Default.StimInfoDevice = Convert.ToString(comboBox_stimInfoDev.SelectedItem);
            if (checkBox_useProgRef.Checked)
                Properties.Settings.Default.SerialPortDevice = Convert.ToString(comboBox_progRefSerialPort.SelectedItem);
            if (checkBox_useEEG.Checked)
                Properties.Settings.Default.EEGDevice = Convert.ToString(comboBox_EEG.SelectedItem);
            if (radioButton_8Mux.Checked)
                Properties.Settings.Default.MUXChannels = 8;
            else
                Properties.Settings.Default.MUXChannels = 16;
            if (radioButton_8bit.Checked) 
                Properties.Settings.Default.StimPortBandwidth = 8;
            else 
                Properties.Settings.Default.StimPortBandwidth = 32;
            if (checkBox_UseAODO.Checked)
                Properties.Settings.Default.SigOutDev = Convert.ToString(comboBox_SigOutDev.SelectedItem);
            if (checkBox_UseAuxAnalogInput.Checked)
            {
                if ((checkBox_useSecondBoard.Checked && comboBox_AuxAnalogInputDevice.SelectedItem.ToString() == comboBox_analogInputDevice2.SelectedItem.ToString())
                    || comboBox_AuxAnalogInputDevice.SelectedItem.ToString() == comboBox_analogInputDevice1.SelectedItem.ToString()
                    || (checkBox_sepLFPBoard1.Checked && comboBox_AuxAnalogInputDevice.SelectedItem.ToString() == comboBox_LFPDevice1.SelectedItem.ToString()))
                {
                    MessageBox.Show("Auxiliary analog input cannot be recorded from a device that is also being used to record neural data (such as" +
                                    " raw electrode input or LFPs). Please make a change in your selections.");
                    return;
                }


                Properties.Settings.Default.auxAnalogInDev = Convert.ToString(comboBox_AuxAnalogInputDevice.SelectedItem);
                for (int i = 0; i < listBox_AuxAnalogInChan.SelectedItems.Count; ++i)
                    Properties.Settings.Default.auxAnalogInChan.Add(listBox_AuxAnalogInChan.SelectedItems[i].ToString());
                if (Properties.Settings.Default.auxAnalogInChan.Count == 0)
                {
                    MessageBox.Show("Cannot read auxiliary analog signals if no physical channels are selected. Please select physical recording channels.");
                    return;
                }
            }
            if (checkBox_UseAuxDigitalInput.Checked)
                Properties.Settings.Default.auxDigitalInPort = Convert.ToString(comboBox_AuxDigInputPort.SelectedItem);


            Properties.Settings.Default.NumAnalogInDevices = (short)Properties.Settings.Default.AnalogInDevice.Count;
            Properties.Settings.Default.ImpedanceDevice = Convert.ToString(comboBox_impedanceDevice.SelectedItem);
            Properties.Settings.Default.StimIvsVDevice = Convert.ToString(comboBox_IVControlDevice.SelectedItem);

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
            if (!checkBox_useStimulator.Checked) checkBox_RecStimTimes.Checked = false;
            checkBox_RecStimTimes.Enabled = checkBox_useStimulator.Checked;
            comboBox_stimInfoDev.Enabled = checkBox_useStimulator.Checked;
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

        private void checkBox_useChannelPlayback_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_singleChannelPlaybackDevice.Enabled = checkBox_useChannelPlayback.Checked;
        }

        private void checkBox_UseAODO_CheckedChanged(object sender, EventArgs e)
        {
            comboBox_SigOutDev.Enabled = checkBox_UseAODO.Checked;
            Properties.Settings.Default.UseAODO = checkBox_UseAODO.Checked;
        }

        private void comboBox_AuxAnalogInputDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox_AuxAnalogInChan.Items.AddRange
                (DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            for (int i = listBox_AuxAnalogInChan.Items.Count - 1; i >= 0; --i)
            {
                string tempChan = listBox_AuxAnalogInChan.Items[i].ToString();
                string[] whatDev = tempChan.Split('/');
                if (!((string)whatDev[0] == comboBox_AuxAnalogInputDevice.SelectedItem.ToString()))
                    listBox_AuxAnalogInChan.Items.RemoveAt(i);
            }
        }

        private void checkBox_UseAuxAnalogInput_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.useAuxAnalogInput = checkBox_UseAuxAnalogInput.Checked;
            comboBox_AuxAnalogInputDevice.Enabled = checkBox_UseAuxAnalogInput.Checked;
            listBox_AuxAnalogInChan.Enabled = checkBox_UseAuxAnalogInput.Checked;
        }

        private void checkBox_UseAuxDigitalInput_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.useAuxDigitalInput = checkBox_UseAuxDigitalInput.Checked;
            comboBox_AuxDigInputPort.Enabled = checkBox_UseAuxDigitalInput.Checked;
        }

        private void checkBox_RecStimTimes_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.RecordStimTimes = checkBox_RecStimTimes.Checked;
            comboBox_stimInfoDev.Enabled = checkBox_RecStimTimes.Checked;
        }
    }
}