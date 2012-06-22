using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Server
{
    /// <summary>
    /// Event arguements for NewData events within server classes.
    /// </summary>
    public class NewDataEventArgs : EventArgs
    {
        private ulong firstNewSample;
        private ulong lastNewSample;
        
        /// <summary>
        /// Sample index of the least recent sample in the new data that has been imported into the data server object.
        /// </summary>
        public ulong FirstNewSample
        {
            internal set
            {
                firstNewSample = value;
            }
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
            internal set
            {
                lastNewSample = value;
            }
            get
            {
                return lastNewSample;
            }
        }
    }
}
