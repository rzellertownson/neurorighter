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
        private bool isEmpty;
        private ulong firstNewSample;
        private ulong lastNewSample;
        private EventBuffer<T> newDataBuffer;

        /// <summary>
        /// Contains information about the New Data Event.
        /// </summary>
        /// <param name="newDataBuffer">New Data buffer that resulted from the NewData Event.</param>
        public NewEventDataEventArgs(EventBuffer<T> newDataBuffer)
        {
            if (newDataBuffer.Buffer.Count == 0)
            {
                this.isEmpty = true;
                this.firstNewSample = 0;
                this.lastNewSample = 0;
                this.newDataBuffer = newDataBuffer;
            }
            else
            {
                this.isEmpty = false;
                this.firstNewSample = newDataBuffer.Buffer.MinBy(x => x.SampleIndex).SampleIndex;
                this.lastNewSample = newDataBuffer.Buffer.MaxBy(x => x.SampleIndex).SampleIndex;
                this.newDataBuffer = newDataBuffer;
            }

        }

        /// <summary>
        /// Boolean that is true of the buffer is empty and false otherwise.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return isEmpty;
            }
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
