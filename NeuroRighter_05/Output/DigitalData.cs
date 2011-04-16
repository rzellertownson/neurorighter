using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter.Output
{
    internal sealed class DigitalData
    {   
        internal ulong EventTime; // digital time (in 100ths of ms)
        internal  UInt32 Byte; // Integer specifying output Byte corresponding to event time
        /// <summary>
        /// Data stucture for holding digital output events
        /// <author> Jon Newman
        /// </author>
        /// </summary>
        /// <param name="EventTime"></param>
        /// <param name="Byte"></param>
        public DigitalData(ulong EventTime,UInt32 Byte)
        {
            this.EventTime = EventTime;
            this.Byte = Byte;
        }

    }
}
