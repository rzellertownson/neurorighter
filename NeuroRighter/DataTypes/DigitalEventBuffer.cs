using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.DataTypes
{
        /// <summary>
        /// This class is the standard NR buffer class for digital event type data. 
        /// Properties are the timeSample which specifies the time, in samples, that
        /// the digital event occured and portState, which is the integer
        /// state of the digital port at the time of the event.
        /// </summary>
        class DigitalEventBuffer
        {
            internal double sampleFrequencyHz;
            internal List<ulong> sampleBuffer;
            internal List<int> portStateBuffer;

            public DigitalEventBuffer(double sampleFrequencyHz)
            {
                this.sampleFrequencyHz = sampleFrequencyHz;
                this.sampleBuffer = new List<ulong>();
                this.portStateBuffer = new List<int>();
            }
        }
   
}
