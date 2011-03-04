// SPKDET.CS
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

    ///<summary>Methods that control spike detection filter selection and parameters via the NR mainform. </summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        private void comboBox_spikeDetAlg_SelectedIndexChanged(object sender, EventArgs e) { setSpikeDetector(); }

        private void button_ForceDetectTrain_Click(object sender, EventArgs e) { setSpikeDetector(); }

        private void numericUpDown_DeadTime_ValueChanged(object sender, EventArgs e)
        {
            setSpikeDetector();
        }

        private void setSpikeDetector()
        {
            detectionDeadTime = (int)Math.Round(Convert.ToDouble(textBox_spikeSamplingRate.Text)*
                (double)numericUpDown_DeadTime.Value/1.0e6);
            switch (comboBox_spikeDetAlg.SelectedIndex)
            {
                case 0:  //RMS Fixed
                    spikeDetector = new RMSThresholdFixed(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value),detectionDeadTime, DEVICE_REFRESH);
                    break;
                case 1:  //RMS Adaptive
                    spikeDetector = new AdaptiveRMSThreshold(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime, DEVICE_REFRESH);
                    break;
                case 2:  //Limada
                    spikeDetector = new LimAda(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime,
                        Convert.ToInt32(textBox_spikeSamplingRate.Text));
                    break;
                default:
                    break;
            }
        }

        private void thresholdMultiplier_ValueChanged(object sender, EventArgs e)
        {
            spikeDetector.thresholdMultiplier = (rawType)Convert.ToDouble(thresholdMultiplier.Value);
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
