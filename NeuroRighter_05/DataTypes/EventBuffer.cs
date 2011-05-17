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
    /// This class is the standard NR buffer class for digital event type data. 
    /// Properties are the timeSample which specifies the time, in samples, that
    /// the digital event occured and portState, which is the integer
    /// state of the digital port at the time of the event.
    /// </summary>
    class EventBuffer<T> where T : NREvent
    {
        internal double sampleFrequencyHz;
        internal List<T> eventBuffer = new List<T>();

        internal EventBuffer(double sampleFrequencyHz)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
        }
    }

}

