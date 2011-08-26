// IMPEDANCE.CS
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

    ///<summary>Methods relating to impedance measurement.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        private void button_impedanceTest_Click(object sender, EventArgs e)
        {
            if (stimDigitalTask != null)
            {
                stimDigitalTask.Dispose();
                stimDigitalTask = null;
            }
            if (stimPulseTask != null)
            {
                stimPulseTask.Dispose();
                stimPulseTask = null;
            }

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
                {
                    int intFreq = (int)Math.Round(freqs[f]);
                    textBox_impedanceResults.Text += "\t" + intFreq.ToString() + "\t\t" + string.Format("{0:0.00}", impedance[channelIndex][f]) + "\r\n";
                }
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
    }
}
