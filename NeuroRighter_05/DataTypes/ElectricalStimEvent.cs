// NeuroRighter
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
// NeuroRighter
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
    /// Generic class for holding mixed digital/analog events corresponding to electrical
    /// stimulation recordings in NR. These are defined by a time sample, a channel, a amplitude 
    /// value and an optional a list of auxiliary doubles that can be inserted by the user.
    /// <author> Jon Newman </author>
    /// </summary>

    public sealed class ElectricalStimEvent : NREvent
    {

        //internal ulong sampleIndex;
        public short channel;
        internal double amplitude;
        internal double width;
        internal List<double> auxInfo = null;
        //public double[] waveform;

       
        internal ElectricalStimEvent(ulong sampleIndex, short channel, double amplitude, double width)
        {
            this.sampleIndex = sampleIndex;
            this.channel = channel;
            this.amplitude = amplitude;
            this.width = width;
        }

        override internal NREvent Copy()
        {
            return new ElectricalStimEvent(this.sampleIndex, this.channel, this.amplitude, this.width);
        }

       
    }
}
