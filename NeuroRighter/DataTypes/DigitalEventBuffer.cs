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

            private double sampleFrequencyHz;
            private List<ulong> sampleBuffer;
            private List<int> portStateBuffer;

            /// <summary>
            /// Standard NR buffer class for digital event type data
            /// </summary>
            /// <param name="SampleFrequencyHz"> Sampling frequency of data in the buffer</param>
            public DigitalEventBuffer(double SampleFrequencyHz)
            {
                this.sampleFrequencyHz = SampleFrequencyHz;
                this.sampleBuffer = new List<ulong>();
                this.portStateBuffer = new List<int>();
            }

            /// <summary>
            /// Sampling frequency of data in the buffer</param>
            /// </summary>
            public uint SampleFrequencyHz
            {
                get
                {
                    return sampleFrequencyHz;
                }
                //set
                //{
                //    sampleFrequencyHz = value;
                //}
            }

            /// <summary>
            /// The time stamp buffer.
            /// </summary>
            public List<ulong> SampleBuffer
            {
                get
                {
                    return sampleBuffer;
                }
                //set
                //{
                //    sampleBuffer = value;
                //}
            }

            /// <summary>
            /// The port state buffer.
            /// </summary>
            public List<int> PortStateBuffer
            {
                get
                {
                    return portStateBuffer;
                }
                //set
                //{
                //    portStateBuffer = value;
                //}
            }

        }
   
}
