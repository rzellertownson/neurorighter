using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using simoc.UI;
using simoc.persistantstate;

namespace simoc.targetfunc
{
    class MulipleOfAverageObs : TargetFunc
    {
        private double estimationTime = 60; // seconds before steps to estimate nominal obs

        public MulipleOfAverageObs(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRDataSrv datSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref datSrv) { }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            double secondsSinceStart = (double)(currentOutputSample / outputSampleRateHz) - simocVariableStorage.LastTargetSwitchedSec;

            if (!simocVariableStorage.TargetOn)
            {
                simocVariableStorage.FrozenCumulativeAverageObs = simocVariableStorage.CumulativeAverageObs; 
            }
            else
            {
                simocVariableStorage.ResetRunningObsAverage();
            }

            if (secondsSinceStart > estimationTime)
            {
                simocVariableStorage.TargetOn = true;
                currentTargetValue = targetMultiplier * simocVariableStorage.FrozenCumulativeAverageObs;
            }
        }
    }
}
