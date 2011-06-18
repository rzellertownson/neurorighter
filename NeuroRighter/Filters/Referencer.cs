using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    internal abstract class Referencer
    {
        internal abstract void reference(double[][] data, int startChannel, int numChannels);
    }
}
