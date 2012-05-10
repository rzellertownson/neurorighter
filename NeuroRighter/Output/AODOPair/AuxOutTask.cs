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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NationalInstruments.DAQmx;

namespace NeuroRighter.Output
{
        /// <summary>
    /// Ihnerited frm MakeAODOPair, specialized for auxilary AO/DO.
    /// <author> Jon Newman </author>
    /// </summary>
    class AuxOutTask : MakeAODOPair
    {
        public AuxOutTask(string device, int bufferSize) : 
            base(device, bufferSize) { }

        internal override void MakeAODOTasks(string auxTaskName,int sampRate, 
            bool useDigOut)
        {
            analogTask = new Task("analog" + auxTaskName);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao0", "",
                -10, 10, AOVoltageUnits.Volts);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao1", "",
                -10, 10, AOVoltageUnits.Volts);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao2", "",
                -10, 10, AOVoltageUnits.Volts);
            analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao3", "",
                -10, 10, AOVoltageUnits.Volts);

            analogTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                sampRate, SampleClockActiveEdge.Rising,
                SampleQuantityMode.ContinuousSamples,
                bufferSize);

            analogTask.SynchronizeCallbacks = false;

            if (useDigOut)
            {
                digitalTask = new Task("digital" + auxTaskName);
                digitalTask.DOChannels.CreateChannel(dev + "/Port0/line0:31",
                    "Generic Digital Out",
                    ChannelLineGrouping.OneChannelForAllLines);

                // Setup DO clock
                digitalTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                    sampRate,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.ContinuousSamples,
                    bufferSize);

                digitalTask.SynchronizeCallbacks = false;
            }

           
        }

    }
}
