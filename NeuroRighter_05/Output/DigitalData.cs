using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter.Output
{
    public sealed class DigitalData
    {   
        public ulong EventTime; // digital time (in 100ths of ms)
        public UInt32 Byte; // Integer specifying output Byte corresponding to event time

        public DigitalData(ulong EventTime,UInt32 Byte)
        {
            this.EventTime = EventTime;
            this.Byte = Byte;
        }

    }
}
