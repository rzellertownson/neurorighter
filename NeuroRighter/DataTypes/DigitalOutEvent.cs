using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter.DataTypes
{
    [Serializable]
    public sealed class DigitalOutEvent : NREvent
    {   
        //internal ulong EventTime; // digital time (in 100ths of ms)
        public  UInt32 Byte; // Integer specifying output Byte corresponding to event time

        /// <summary>
        /// Data stucture for holding digital output events
        /// </summary>
        /// <param name="EventTime"> Sample of digital event</param>
        /// <param name="Byte">The port state of the event, 32 bit unsigned integer</param>
        public DigitalOutEvent(ulong EventTime,UInt32 Byte)
        {
            this.sampleIndex = EventTime;
            this.Byte = Byte;
            //this.sampleDuration = 0;
        }
    }
}
