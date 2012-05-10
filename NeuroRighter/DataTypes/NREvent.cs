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
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// Base NeuroRighter output event data type.
    /// </summary>
    [Serializable] 
    public abstract class NREvent 

    {
        /// <summary>
        /// The sample, measured from the start of protocol executation, of the event.
        /// </summary>
        protected ulong sampleIndex;

        /// <summary>
        /// The duration of the event if it is longer than on sample.
        /// </summary>
        protected uint sampleDuration;

        /// <summary>
        /// Base class for ouput event type data classes in NR.
        /// </summary> 
        public NREvent() {}

        /// <summary>
        /// Specifies when the output event occurs, in samples, relative to recording start
        /// </summary>
        public ulong SampleIndex
        {
            get
            {
                return sampleIndex;
            }
            set
            {
                sampleIndex=value;
            }
        }

        /// <summary>
        /// Duration of stimulation event in samples.
        /// </summary>
        internal uint SampleDuration
        {
            get
            {
                return sampleDuration;
            }
            set
            {
                sampleDuration = value;
            }
        }
    }
}
