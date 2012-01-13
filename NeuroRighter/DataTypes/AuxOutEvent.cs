using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.DataTypes
{
    [Serializable]
    public sealed class AuxOutEvent:NREvent
    {
        internal ushort eventChannel; // the analog channel that the event corresponds to (0-3)
        internal double eventVoltage; // analog state corresponding to event time

        /// <summary>
        /// NeuroRighters auxiliary analog out event data type.
        /// </summary>
        /// <param name="time">auxiliary event sample</param>
        /// <param name="channel">the analog channel (0-3), corresponding to the event time</param>
        /// <param name="voltage">analog voltage state, -10 to 10 volts, corresponding to the event time</param>
        public AuxOutEvent(ulong sampleIndex, ushort channel, double voltage)
        {
            this.sampleIndex = sampleIndex;
            this.eventChannel = channel;
            this.eventVoltage = voltage;
        }

    }
}
