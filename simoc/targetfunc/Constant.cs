using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using simoc.UI;
namespace simoc.targetfunc
{

    /// <summary>
    /// Class for constant target function.
    /// <author> Jon Newman</author>
    /// </summary>
    class Constant : TargetFunc
    {

        public Constant(ControlPanel cp)
            : base(cp)
        {

        }

        internal override void GetTargetValue(ref double currentTargetValue)
        {
            currentTargetValue = meanValue;
        }



    }
}
