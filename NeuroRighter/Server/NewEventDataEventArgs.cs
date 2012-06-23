using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using MoreLinq;

namespace NeuroRighter.Server
{
    /// <summary>
    /// Event arguements for NewData events within NREvent server classes.
    /// </summary>
    public class NewEventDataEventArgs<T> where T : NREvent
    {
        private ulong firstNewSample;
        private ulong lastNewSample;
        private EventBuffer<T> newDataBuffer;

        public NewEventDataEventArgs(EventBuffer<T> newDataBuffer)
        {
            this.firstNewSample = newDataBuffer.Buffer.MinBy(x => x.SampleIndex).SampleIndex;
            this.lastNewSample = newDataBuffer.Buffer.MaxBy(x => x.SampleIndex).SampleIndex;
            this.newDataBuffer = newDataBuffer;
        }

        /// <summary>
        /// Sample index of the least recent sample in the new data that has been imported into the data server object.
        /// </summary>
        public ulong FirstNewSample
        {
            get
            {
                return firstNewSample;
            }
        }

        /// <summary>
        /// Sample index of the least recent sample in the new data that has been imported into the data server object.
        /// </summary>
        public ulong LastNewSample
        {
            get
            {
                return lastNewSample;
            }
        }

        /// <summary>
        /// The new data buffer.
        /// </summary>
        public EventBuffer<T> NewDataBuffer
        {
            get
            {
                return newDataBuffer;
            }
        }
    }
}
