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
    /// This class is the standard NR buffer class for event type data. 
    /// The eventBuffer property contains generic events of a user defined type.
    /// </summary>
    public class EventBuffer<T> where T : NREvent
    {
        /// <summary>
        /// Sampling frequency of data in the buffer</param>
        /// </summary>
        public double sampleFrequencyHz;

        /// <summary>
        /// The event buffer.
        /// </summary>
        public List<T> eventBuffer = new List<T>();

        /// <summary>
        /// Standard NR buffer class for generic event data
        /// </summary>
        /// <param name="sampleFrequencyHz"> Sampling frequency of data in the buffer</param>
        public EventBuffer(double sampleFrequencyHz)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
        }
    }

}

