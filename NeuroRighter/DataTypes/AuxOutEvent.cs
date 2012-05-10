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
    /// An auxiliary analog output event type.
    /// </summary>
    [Serializable]
    public sealed class AuxOutEvent:NREvent
    {
        internal ushort eventChannel; // the analog channel that the event corresponds to (0-3)
        internal double eventVoltage; // analog state corresponding to event time

        /// <summary>
        /// NeuroRighters auxiliary analog out event data type.
        /// </summary>
        /// <param name="sampleIndex">auxiliary event sample</param>
        /// <param name="channel">the analog channel (0-3), corresponding to the event time</param>
        /// <param name="voltage">analog voltage state, -10 to 10 volts, corresponding to the event time</param>
        public AuxOutEvent(ulong sampleIndex, ushort channel, double voltage)
        {
            this.sampleIndex = sampleIndex;
            this.eventChannel = channel;
            this.eventVoltage = voltage;
        }

        /// <summary>
        /// The channel on which the auxilary event occurs.
        /// </summary>
        public ushort Channel
        {
            get
            {
                return eventChannel;
            }
        }

        /// <summary>
        /// The resulting voltage of the channel after the event has occured.
        /// </summary>
        public double Voltage
        {
            get
            {
                return eventVoltage;
            }
        }
    }
}
