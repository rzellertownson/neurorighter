using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.StimSrv;
using simoc.UI;
using simoc.persistantstate;

namespace simoc.targetfunc
{
    class MulipleOfAverageObs : TargetFunc
    {
        private double estimationTime = 60; // seconds before steps to estimate nominal obs

        public MulipleOfAverageObs(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRStimSrv stimSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref stimSrv) { }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            double secondsSinceStart = (double)(currentOutputSample / outputSampleRateHz) - simocVariableStorage.LastTargetSwitchedSec;

            if (simocVariableStorage.TargetOn)
            {
                simocVariableStorage.TargetOn = true;
                simocVariableStorage.ResetRunningObsAverage();
                simocVariableStorage.LastTargetIndex = 0;
            }

            if (secondsSinceStart > estimationTime)
            {
                if (simocVariableStorage.LastTargetIndex == 0)
                {
                    simocVariableStorage.LastTargetIndex = 1;
                    simocVariableStorage.FrozenCumulativeAverageObs = simocVariableStorage.CumulativeAverageObs;
                }

                currentTargetValue = targetMultiplier * simocVariableStorage.FrozenCumulativeAverageObs;
            }
        }
    }
}
