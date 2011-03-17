// PLOTTING.CS
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


namespace NeuroRighter
{
    ///<summary>Methods for plotting spike traces and waveforms.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        private void spikePlotData_dataAcquired(object sender)
        {
            PlotData pd = (PlotData)sender;

            if (spikeGraph.Visible && !checkBox_freeze.Checked)
            {
                float[][] data = pd.read();
                float[][] currentThresh = spikeDet.spikeDetector.GetCurrentThresholds();
                float[][] threshdata1 = pd.readthresh(currentThresh[0]);
                float[][] threshdata2 = pd.readthresh(currentThresh[1]);

                for (int i = 0; i < data.Length; ++i)
                    spikeGraph.plotYWithThresh(data[i], threshdata1[i], threshdata2[i],
                        0, 1, NRBrainbow, pd.numSamplesPerPlot,
                        Microsoft.Xna.Framework.Graphics.Color.SlateGray, i);
                spikeGraph.Invalidate();
            }

            else { pd.skipRead(); }

            #region Recording_LED
            if (switch_record.Value)
            {
                //Toggle recording light
                if (recordingLEDState++ == 1)
                {
                    if (led_recording.OnColor == Color.DarkGreen)
                        led_recording.OnColor = Color.Lime;
                    else
                        led_recording.OnColor = Color.DarkGreen;
                }
                recordingLEDState %= 2;
            }
            #endregion
        }

        private void waveformPlotData_dataAcquired(object sender)
        {
            EventPlotData pd = (EventPlotData)sender;
            if (spkWfmGraph.Visible && !checkBox_freeze.Checked)
            {
                int maxWaveforms = pd.getMaxWaveforms();

                List<PlotSpikeWaveform> wfms = pd.read();
                for (int i = 0; i < wfms.Count; ++i)
                {
                    int channel = wfms[i].channel;
                    if (Properties.Settings.Default.ChannelMapping == "invitro")
                        channel = (MEAChannelMappings.ch2rc[channel, 0] - 1) * 8 + MEAChannelMappings.ch2rc[channel, 1] - 1;
                    spkWfmGraph.plotY(wfms[i].waveform, pd.horizontalOffset(channel), 1, NRBrainbow, channel,
                        numSpkWfms[channel]++ + channel * maxWaveforms);
                    numSpkWfms[channel] %= maxWaveforms;
                }
                spkWfmGraph.Invalidate();
            }
            else { pd.skipRead(); }
        }

        void lfpPlotData_dataAcquired(object sender)
        {
            //if (lfpGraph.InvokeRequired)
            //{
            //    lfpGraph.BeginInvoke(new plotData_dataAcquiredDelegate(lfpPlotData_dataAcquired), sender);
            //}
            //else
            //{
            PlotData pd = (PlotData)sender;
            if (lfpGraph.Visible && !checkBox_freeze.Checked)
            {
                float[][] data = pd.read();
                for (int i = 0; i < data.Length; ++i)
                    //lfpGraph.Plots.Item(i + 1).PlotY(data[i], (double)pd.downsample / (double)lfpSamplingRate,
                    //    (double)pd.downsample / (double)lfpSamplingRate);
                    lfpGraph.plotY(data[i], 0F, 1F, NRBrainbow, i);
                lfpGraph.Invalidate();
            }
            else { pd.skipRead(); }
            //}
        }

        void muaPlotData_dataAcquired(object sender)
        {
            PlotData pd = (PlotData)sender;
            if (muaGraph.Visible && !checkBox_freeze.Checked)
            {
                float[][] data = pd.read();
                for (int i = 0; i < data.Length; ++i)
                    //lfpGraph.Plots.Item(i + 1).PlotY(data[i], (double)pd.downsample / (double)lfpSamplingRate,
                    //    (double)pd.downsample / (double)lfpSamplingRate);
                    muaGraph.plotY(data[i], 0F, 1F, NRBrainbow, i);
                muaGraph.Invalidate();
            }
            else { pd.skipRead(); }
        }

        private void resetSpkWfm()
        {
            numSpkWfms = new int[numChannels];
            for (int i = 0; i < numSpkWfms.Length; ++i)
                numSpkWfms[i] = 0; //Set to 1

            int numCols, numRows;
            switch (Convert.ToInt32(comboBox_numChannels.SelectedItem))
            {
                case 16:
                    numRows = numCols = 4; break;
                case 32:
                    numRows = numCols = 6; break;
                case 64:
                    numRows = numCols = 8; break;
                default:
                    numRows = numCols = 4; break;
            }
            if (spkWfmGraph != null) { spkWfmGraph.Dispose(); spkWfmGraph = null; }
            spkWfmGraph = new SnipGridGraph();
            if (spikeTask != null && spikeTask[0] != null)
                spkWfmGraph.setup(numRows, numCols, numPre + numPost + 1, true, (double)(numPre + numPost + 1) / spikeSamplingRate, spikeTask[0].AIChannels.All.RangeHigh * 2.0);
            else
            {
                double gain = 20.0 / Convert.ToInt32(comboBox_SpikeGain.SelectedItem);
                spkWfmGraph.setup(numRows, numCols, numPre + numPost + 1, true, (double)(numPre + numPost + 1) / spikeSamplingRate, gain);
            }
            //spkWfmGraph.Resize += new EventHandler(spkWfmGraph.resize);
            //spkWfmGraph.SizeChanged += new EventHandler(spkWfmGraph.resize);
            //spkWfmGraph.VisibleChanged += new EventHandler(spkWfmGraph.resize);
            spkWfmGraph.Parent = tabPage_waveforms;
            spkWfmGraph.BringToFront();
            spkWfmGraph.Dock = DockStyle.Fill;

            //Adjust ranges
            if (spikeTask != null && spikeTask[0] != null)
            {
                double wfmLength = numPre + numPost + 1;
                spkWfmGraph.setMinMax(1, (float)(numCols * wfmLength), (float)(spikeTask[0].AIChannels.All.RangeLow * (numRows * 2 - 1)), (float)(spikeTask[0].AIChannels.All.RangeHigh));
            }
            //spkWfmGraph.clear();
        }
    }
}
