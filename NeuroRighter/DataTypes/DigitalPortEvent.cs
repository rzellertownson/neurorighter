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

    /// <author> Jon Newman </author>
    /// </summary>
    [Serializable]
    public sealed class DigitalPortEvent : NREvent
    {
        //internal ulong sampleIndex;
        internal uint portState;

        /// <summary>
        /// Generic class for holding digial events. That is, those that are defined
        /// by a discrete point in time, but also can have a port state, represented
        /// by an 32 bit integer, associated with them.
        /// </summary>
        /// <param name="sampleIndex">Event sample index</param>
        /// <param name="portState">32 bit integer representing the port state at the corresponding sample index</param>
        public DigitalPortEvent(ulong sampleIndex, uint portState)
        {
            this.sampleIndex = sampleIndex;
            this.portState = portState;
        }

        /// <summary>
        /// The 32 bit integer state of the digital port.
        /// </summary>
        public uint PortState
        {
            get
            {
                return portState; 
            }
        }

    }
}
