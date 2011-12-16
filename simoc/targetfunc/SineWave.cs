using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.UI;
using NeuroRighter.DatSrv;
using simoc.persistantstate;

namespace simoc.targetfunc
{
    class SineWave : TargetFunc
    {
        private double sqrt2 = Math.Sqrt(2);

        public SineWave(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref  NRDataSrv datSrv)
            : base(cp, DACPollingPeriodSec,numTargetSamplesGenerated, ref datSrv)
        {

        }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            currentTargetValue =
                Math.Cos(frequency * 2 * Math.PI * numTargetSamplesGenerated * DACPollingPeriodSec) * (sqrt2 * standardDev) + meanValue;
        }




    }
}
