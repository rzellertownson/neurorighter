using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter.DataTypes
{
    public sealed class DigitalOutEvent:NREvent
    {   
        //internal ulong EventTime; // digital time (in 100ths of ms)
        public  UInt32 Byte; // Integer specifying output Byte corresponding to event time
        /// <summary>
        /// Data stucture for holding digital output events
        /// <author> Jon Newman
        /// </author>
        /// </summary>
        /// <param name="EventTime"></param>
        /// <param name="Byte"></param>
        public DigitalOutEvent(ulong EventTime,UInt32 Byte)
        {
            this.sampleIndex = EventTime;
            this.Byte = Byte;
            this.sampleDuration = 0;
        }
        internal override NREvent Copy()
        {
            return new DigitalOutEvent(this.sampleIndex, this.Byte);
        }

    }
}
