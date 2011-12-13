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
using NeuroRighter.SpikeDetection;

namespace NeuroRighter
{

    ///<summary>Methods that control spike detection filter selection and parameters via the NR mainform. </summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {
        private void button_OpenSpkDetSettings_Click(object sender, EventArgs e)
        {
            spikeDet.Show();
            spikeDet.BringToFront();
        }

        private void button_TrainSpkDet_Click(object sender, EventArgs e)
        {
            spikeDet.SetSpikeDetector();
        }

        internal void spikeDet_SettingsHaveChanged(object sender, EventArgs e)
        {
            spikeDet.SetSpikeDetector();
            numPre = spikeDet.numPre;
            numPost = spikeDet.numPost;
        }
    }
}
