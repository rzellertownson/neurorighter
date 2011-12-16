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
    class CustomTarget2 : TargetFunc
    {
        private double[] muliplierTargets = {0,1.5,0,2.0};
        private double t1 = 1800;

        public CustomTarget2(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRDataSrv datSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref datSrv)
        {
        }


        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            double secondsSinceStart = ((double)(currentOutputSample - simocVariableStorage.SimocStartSample)) / outputSampleRateHz;
            int currentTargetIndex = (int)Math.Floor(secondsSinceStart / t1);

            if (currentTargetIndex == simocVariableStorage.LastTargetIndex)
            {
                if (muliplierTargets.Length > currentTargetIndex)
                    currentTargetValue = simocVariableStorage.FrozenCumulativeAverageObs * muliplierTargets[currentTargetIndex];
                else
                    currentTargetValue = 0.0;
            }
            else
            {
                simocVariableStorage.FrozenCumulativeAverageObs = simocVariableStorage.CumulativeAverageObs;
                simocVariableStorage.ResetRunningObsAverage();
            }

            simocVariableStorage.LastTargetIndex = currentTargetIndex;

        }


    }
}
