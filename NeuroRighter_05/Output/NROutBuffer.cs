using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;

namespace NeuroRighter.Output
{
    internal abstract class NROutBuffer<T> where T : NREvent
    {
        internal abstract void Append(List<T> addtobuffer);
        internal ulong currentSample;
        public void writeToBuffer(List<T> addtobuffer)
        {
            //error checking
            foreach (NREvent n in addtobuffer)
            {
                if (n.sampleIndex<currentSample)
                    throw new Exception(this.ToString()+ ": Attempted to stimulate in the past (sample "+n.sampleIndex +" at sample "+currentSample+")");
            }
            //passed, add to buffer
            Append(addtobuffer);
        }
    }
}
