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
    /// 
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
    }
}
