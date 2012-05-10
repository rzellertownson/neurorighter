using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// Digital output event type.
    /// </summary>
    [Serializable]
    public sealed class DigitalOutEvent : NREvent
    {   
        //internal ulong EventTime; // digital time (in 100ths of ms)
        private UInt32 portInt32; // Integer specifying output Byte corresponding to event time

        /// <summary>
        /// Data type defining digital output events.
        /// </summary>
        /// <param name="EventTime"> Sample of digital event</param>
        /// <param name="PortInt32">The port state of the event represented as a 32 bit unsigned integer</param>
        public DigitalOutEvent(ulong EventTime, UInt32 PortInt32)
        {
            this.sampleIndex = EventTime;
            this.portInt32 = PortInt32;
        }

        /// <summary>
        /// The port state represented as a 32 bit unsigned integer.
        /// </summary>
        public UInt32 PortInt32
        {
            get
            {
                return portInt32;
            }
        }
    }
}
