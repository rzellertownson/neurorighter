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
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeuroRighter.DataTypes
{

    [Serializable] 
    public abstract class NREvent 

    {
        protected ulong sampleIndex;
        protected uint sampleDuration;

        /// <summary>
        /// Base class for event type data wrapper classes in NR.
        /// <author>Jon Newman </author>
        /// </summary> 
        public NREvent() {}

        /// <summary>
        /// Specifies when event occured relative to recording start
        /// </summary>
        public ulong SampleIndex
        {
            get
            {
                return sampleIndex;
            }
            set
            {
                value = sampleIndex;
            }
        }
    }
}
