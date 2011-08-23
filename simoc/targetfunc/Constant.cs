using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using NeuroRighter.StimSrv;
using simoc.UI;
using simoc.persistantstate;

namespace simoc.targetfunc
{

    /// <summary>
    /// Class for constant target function.
    /// <author> Jon Newman</author>
    /// </summary>
    class Constant : TargetFunc
    {

        public Constant(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRStimSrv stimSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref stimSrv)
        {

        }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            currentTargetValue = meanValue;
        }
    }
}
