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
        private ulong firstNeweSample;
        private ulong lastNeweSample;
        
        /// <summary>
        /// Sample index of the least recent sample in the new data that has been imported into the data server object.
        /// </summary>
        public ulong FirstNewSample
        {
            internal set
            {
                firstNeweSample = value;
            }
            get
            {
                return this.firstNeweSample;
            }
        }

        /// <summary>
        /// Sample index of the least recent sample in the new data that has been imported into the data server object.
        /// </summary>
        public ulong LastNewSample
        {
            internal set
            {
                lastNeweSample = value;
            }
            get
            {
                return this.lastNeweSample;
            }
        }
    }
}
