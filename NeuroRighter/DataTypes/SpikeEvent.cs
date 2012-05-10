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
    /// Generic class for holding spike events. That is, those that are defined
    /// by a discrete point in time, but also can have a channel, a snip of analog data
    /// and,  a threshold value associated with them.
    /// <author> Jon Newman </author>
    /// </summary>
    
    [Serializable]
    public sealed class SpikeEvent : NREvent
    {
        private Int16 channel;
        private double threshold;
        private Int16 unit;
        private double[] waveform;

        /// <summary>
        /// Generic class for holding spike events generated within NR.
        /// </summary>
        /// <param name="channel">HW Channel that the spike occured on</param>
        /// <param name="sampleIndex"> The sample index, relative to recording start, in which the peak of the spike occured.</param>
        /// <param name="threshold"> The voltage threshold at which the spike was detected</param>
        /// <param name="waveform">A voltage vector specifying a spike snippet</param>
        public SpikeEvent(int channel, ulong sampleIndex, double threshold, double[] waveform)
        {
            this.channel = (short)channel;
            this.sampleIndex = sampleIndex;
            this.threshold = threshold;
            this.waveform = waveform;
            this.unit = 0;
        }

        /// <summary>
        /// Set the unit number of waveform after sorting
        /// </summary>
        /// <param name="unit">The spike's unit number</param>
        internal void SetUnit(Int16 unit)
        {
            this.unit = unit;
        }

        /// <summary>
        /// Set the unit number of waveform after sorting
        /// </summary>
        /// <param name="unit">The spike's unit number</param>
        internal void SetUnit(int unit)
        {
            this.unit = (short)unit;
        }

        /// <summary>
        /// Channel on which the spike was dectected.
        /// </summary>
        public Int16 Channel
        {
            get
            {
                return channel;
            }
            set
            {
                channel = value;
            }
        }

        /// <summary>
        /// The voltage threshold at which the spike was detected
        /// </summary>
        public double Threshold
        {
            get
            {
                return threshold;
            }
        }

        /// <summary>
        /// The sorted unit number that generated the spike.
        /// </summary>
        public short Unit
        {
            get
            {
                return unit;
            }
        }

        /// <summary>
        /// A voltage vector specifying a spike snippet
        /// </summary>
        public double[] Waveform
        {
            get
            {
                return waveform;
            }
        }
    }
}
