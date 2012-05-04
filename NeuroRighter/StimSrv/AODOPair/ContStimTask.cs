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
using NationalInstruments.DAQmx;

namespace NeuroRighter.StimSrv
{
    /// <summary>
    /// Ihnerited frm MakeAODOPair, specialized for stimulation output.
    /// <author> Jon Newman </author>
    /// </summary>
    class ContStimTask : MakeAODOPair
    {
        public ContStimTask(string device, int bufferSize) :
            base(device, bufferSize) { }

        internal override void MakeAODOTasks(string stimTaskName,
            int stimPortBandWidth, int sampRate)
        {

            analogTask  = new Task("analog" + stimTaskName);
            digitalTask = new Task("digital" + stimTaskName);

            if (stimPortBandWidth == 32)
                digitalTask.DOChannels.CreateChannel("/" + dev + "/Port0/line0:31", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            else if (stimPortBandWidth == 8)
                digitalTask.DOChannels.CreateChannel("/" + dev + "/Port0/line0:7", "",
                    ChannelLineGrouping.OneChannelForAllLines); //To control MUXes
            if (stimPortBandWidth == 32)
            {
                analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao0", "",
                    -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao1", "",
                    -10.0, 10.0, AOVoltageUnits.Volts); //Triggers
                analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao2", "",
                    -10.0, 10.0, AOVoltageUnits.Volts); //Actual Pulse
                analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao3", "",
                    -10.0, 10.0, AOVoltageUnits.Volts); //Timing
            }
            else if (stimPortBandWidth == 8)
            {
                analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao0", "",
                    -10.0, 10.0, AOVoltageUnits.Volts);
                analogTask.AOChannels.CreateVoltageChannel("/" + dev + "/ao1", "",
                    -10.0, 10.0, AOVoltageUnits.Volts);
            }

            // setup AO clock
            analogTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                sampRate, 
                SampleClockActiveEdge.Rising,
                SampleQuantityMode.ContinuousSamples,
                bufferSize);

            // Setup DO clock
            digitalTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                sampRate, 
                SampleClockActiveEdge.Rising,
                SampleQuantityMode.ContinuousSamples, 
                bufferSize);

            digitalTask.SynchronizeCallbacks = false;
            analogTask.SynchronizeCallbacks = false;
        }
    }
}
