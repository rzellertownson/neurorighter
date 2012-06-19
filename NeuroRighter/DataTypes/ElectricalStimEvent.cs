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
    [Serializable]
    public sealed class ElectricalStimEvent : NREvent
    {

        private short channel;
        private double amplitude;
        private double width;
        //private List<double> auxInfo = null;

        /// <summary>
        /// Generic class for holding mixed digital/analog events corresponding to electrical
        /// stimulation recordings in NR. These are defined by a time sample, a channel, a amplitude 
        /// value and an optional a list of auxiliary doubles that can be inserted by the user.
        /// </summary>
        /// <param name="sampleIndex"> The time of the detected electrical stimulus. </param>
        /// <param name="channel">The channel on which the stimulus occured.</param>
        /// <param name="amplitude"> The peak ampltude of the stimulus waveform</param>
        /// <param name="width">The width of the stimulus waveform</param>
        internal ElectricalStimEvent(ulong sampleIndex, short channel, double amplitude, double width)
        {
            this.sampleIndex = sampleIndex;
            this.channel = channel;
            this.amplitude = amplitude;
            this.width = width;
        }

        /// <summary>
        /// The channel on which the stimulation occurs.
        /// </summary>
        public short Channel
        {
            get
            {
                return channel;
            }
        }

        /// <summary>
        /// The resulting stimulation event amplitude.
        /// </summary>
        public double Amplitude
        {
            get
            {
                return amplitude;
            }
        }

        /// <summary>
        /// The width of the stimulus waveform
        /// </summary>
        public double Width
        {
            get
            {
                return width;
            }
        }
    }
}
