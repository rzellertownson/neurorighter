// DIAGNOSTICS.CS
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
using NeuroRighter.StimSrv;

namespace NeuroRighter
{
    ///<summary>Declarations for the NeuroRighter UI.</summary>
    ///<author>Jon Newman</author>
    sealed internal partial class NeuroRighter
    {

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
                spikeTask.Add(new Task("spikeTask_Diagnostics_" + i));
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
                    textBox_diagnosticsResults.Text += "Channel " + (c + 1).ToString() + "\r\n\tFrequency (Hz)\tGain (dB)\r\n";
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

        //Compute the RMS of an array.  Use this rather than a stock method, since it has no error checking and is faster.  Error checking is for pansies! 
        //[above comment is from J.R... -J.N.]
        internal static double rootMeanSquared(double[] data)
        {
            double rms = 0;
            for (int i = 0; i < data.Length; ++i)
                rms += data[i] * data[i];
            rms /= data.Length;
            return Math.Sqrt(rms);
        }


    }
}
