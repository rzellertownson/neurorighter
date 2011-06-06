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
    /// Generic class for holding spike events. That is, those that are defined
    /// by a discrete point in time, but also can have a channel, a snip of analog data
    /// and,  a threshold value associated with them.
    /// <author> Jon Newman </author>
    /// </summary>
    
    public sealed class SpikeEvent : NREvent
    {
        //internal ulong sampleIndex;
        public Int16 channel;
        public double threshold;
        public double[] waveform;

        public SpikeEvent(int channel, ulong sampleIndex, double threshold, double[] waveform)
        {
            this.channel = (short)channel;
            this.sampleIndex = sampleIndex;
            this.threshold = threshold;
            this.waveform = waveform;
        }
        override internal NREvent Copy()
        {
            double[] wavecpy = new double[waveform.Length];
            for (int i = 0; i < waveform.Length; i++)
            {
                wavecpy[i] = waveform[i];
            }
            return new SpikeEvent(this.channel, this.sampleIndex, this.threshold, wavecpy);
        }
    }
}
