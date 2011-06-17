﻿// NeuroRighter
// Copyright (c) 2008 John Rolston
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
    /// <author> Riley Zeller-Townson </author>
    /// </summary>
    
    [Serializable]
    internal sealed class AnalogOutEvent : NREvent
    {
        //internal ulong sampleIndex;
        internal uint channel;
        internal double voltage;

        public AnalogOutEvent(ulong sampleIndex, uint channel, double voltage)
        {
            this.sampleIndex = sampleIndex;
            this.channel = channel;
            this.voltage = voltage;
        }

        //internal override NREvent Copy()
        //{
        //    return new AnalogOutEvent(this.sampleIndex, this.channel, this.voltage);
        //}
    }
}
