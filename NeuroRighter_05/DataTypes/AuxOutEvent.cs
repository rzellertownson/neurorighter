using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.DataTypes
{
    internal sealed class AuxOutEvent:NREvent
    {
        //internal ulong eventTime; // auxiliary event time (in 100ths of ms)
        internal ushort eventChannel; // the analog channel that the event corresponds to (1-4)
        internal double eventVoltage; // 1X4 analog state corresponding to event time

        public AuxOutEvent(ulong time, ushort channel, double voltage)
        {
            this.sampleIndex = time;
            this.eventChannel = channel;
            this.eventVoltage = voltage;
        }

    }
}
