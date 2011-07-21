using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;

namespace simoc.spk2obs
{
    /// <summary>
    /// Base clase for spike detection. All spike detectors should inherit this
    /// virtual class. 
    /// <author> Jon Newman</author>
    /// </summary>
    internal abstract class Obs2Out
    {
        bool useAnOut;
        bool useDigOut;
        bool useStimOut;

        public Obs2Out()
        {

        }

        internal virtual void SetOuputTypes()
        {
            // Use this to set which types of outputs are going to be sent
        }

        internal virtual List<List<T>> MakeOutputBuffers()
        {
            // Overriden based on a certain algorithm. Sends out a list of one or more lists of ouput
            // buffers.
        }





    }
}
