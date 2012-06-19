// NeuroRighter
// Copyright (c) 2008-2012 Potter Lab
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

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// Generic class for holding Analog Output events. That is, those that are defined
    /// by a discrete point in time, but also have a channel and voltage
    /// </summary>
    [Serializable]
    public sealed class AnalogOutEvent : NREvent
    {
        //internal ulong sampleIndex;
        private uint channel;
        private double voltage;

        /// <summary>
        /// Generic class for holding Analog Output events. That is, those that are defined
        /// by a discrete point in time, but also have a channel and voltage
        /// </summary>
        /// <param name="sampleIndex"> event time (in 100ths of ms)</param>
        /// <param name="channel">the analog channel (0-3), corresponding to the event time</param>
        /// <param name="voltage">analog voltage state, -10 to 10 volts, corresponding to the event time</param>
        public AnalogOutEvent(ulong sampleIndex, uint channel, double voltage)
        {
            this.sampleIndex = sampleIndex;
            this.channel = channel;
            this.voltage = voltage;
        }

        /// <summary>
        /// The channel on which the analog output event will occur.
        /// </summary>
        public uint Channel
        {
            get
            {
                return channel;
            }
        }

        /// <summary>
        /// The voltage that the output channel will take after the event has occured.
        /// </summary>
        public double Voltage
        {
            get
            {
                return voltage;
            }
        }


    }
}
