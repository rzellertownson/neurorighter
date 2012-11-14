// PLOTCONT.CS
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

    ///<summary>Methods for the control of the plots shown in the NR mainform. For instance zooming, freezing and replay methods. </summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        // Mouse-over graphics for scaling icons
        private void button_scaleUp_MouseEnter(object sender, EventArgs e) { mouseOver(sender, 1); }
        private void button_scaleUp_MouseLeave(object sender, EventArgs e) { mouseOver(sender, 0); }
        private void button_scaleDown_MouseEnter(object sender, EventArgs e) { mouseOver(sender, 3); }
        private void button_scaleDown_MouseLeave(object sender, EventArgs e) { mouseOver(sender, 2); }
        private void button_scaleReset_MouseEnter(object sender, EventArgs e) { mouseOver(sender, 5); }
        private void button_scaleReset_MouseLeave(object sender, EventArgs e) { mouseOver(sender, 4); }
        private void mouseOver(object sender, int imageNumber)
        {
            Button b = (Button)sender;
            b.Image = imageList_zoomButtons.Images[imageNumber];
        }

        private void button_scaleDown_Click(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            {
                case "Spikes":
                    spikePlotData.setGain(spikePlotData.getGain() * 0.5F);
                    spikeGraph.setDisplayGain(spikePlotData.getGain());
                    break;
                case "Spk Wfms":
                    waveformPlotData.setGain(waveformPlotData.getGain() / 2);
                    spkWfmGraph.setDisplayGain(waveformPlotData.getGain());
                    break;
                case "LFPs":
                    lfpPlotData.setGain(lfpPlotData.getGain() * 0.5F);
                    lfpGraph.setDisplayGain(lfpPlotData.getGain());
                    break;
                case "MUA":
                    muaPlotData.setGain(muaPlotData.getGain() * 0.5F);
                    muaGraph.setDisplayGain(muaPlotData.getGain());
                    break;
                case "EEG":
                    eegDisplayGain /= 2;
                    break;
                default:
                    break;
                //do nothing
            }
        }

        private void button_scaleUp_Click(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            {
                case "Spikes":
                    spikePlotData.setGain(spikePlotData.getGain() * 2F);
                    spikeGraph.setDisplayGain(spikePlotData.getGain());
                    break;
                case "Spk Wfms":
                    waveformPlotData.setGain(waveformPlotData.getGain() * 2F);
                    spkWfmGraph.setDisplayGain(waveformPlotData.getGain());
                    break;
                case "LFPs":
                    lfpPlotData.setGain(lfpPlotData.getGain() * 2F);
                    lfpGraph.setDisplayGain(lfpPlotData.getGain());
                    break;
                case "MUA":
                    muaPlotData.setGain(muaPlotData.getGain() * 2F);
                    muaGraph.setDisplayGain(muaPlotData.getGain());
                    break;
                case "EEG":
                    eegDisplayGain *= 2;
                    break;
                default:
                    break;
                //do nothing
            }
        }

        private void button_scaleReset_Click(object sender, EventArgs e)
        {
            switch (tabControl.SelectedTab.Text)
            //switch (tabControl.SelectedIndex)
            {
                //case 0:
                case "Spikes":
                    spikePlotData.setGain(1F);
                    spikeGraph.setDisplayGain(spikePlotData.getGain());
                    break;
                case "Spk Wfms":
                    waveformPlotData.setGain(1F);
                    spkWfmGraph.setDisplayGain(waveformPlotData.getGain());
                    break;
                case "LFPs":
                    lfpPlotData.setGain(1F);
                    lfpGraph.setDisplayGain(lfpPlotData.getGain());
                    break;
                case "MUA":
                    muaPlotData.setGain(1F);
                    muaGraph.setDisplayGain(muaPlotData.getGain());
                    break;
                case "EEG":
                    eegDisplayGain = 1;
                    break;
                default:
                    break;
                //do nothing
            }
        }

        private void button_clearSpkWfms_Click(object sender, EventArgs e)
        {
            spkWfmGraph.clear();
            spkWfmGraph.Invalidate();
        }

        private void numericUpDown_NumSnipsDisplayed_ValueChanged(object sender, EventArgs e)
        {
            if (spkWfmGraph != null)
            {
                spkWfmGraph.clear();
                waveformPlotData.MaxWaveforms = (int)numericUpDown_NumSnipsDisplayed.Value;
                spkWfmGraph.WaveformsToPlot = (int)numericUpDown_NumSnipsDisplayed.Value;
                spkWfmGraph.clear();
            }
        }
    }
}
