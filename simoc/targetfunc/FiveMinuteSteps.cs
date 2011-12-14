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
    class FiveMinuteSteps : TargetFunc
    {
        private double[] muliplierTargets = { 1.2, 1.4, 1.6, 1.8, 2.0, 5.0, 10 };
        private double estimationTime = 60; // seconds before steps to estimate nominal obs
        private double stepTime = 300; // seconds
        private double pauseTime = 60; // seconds

        public FiveMinuteSteps(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRStimSrv stimSrv)
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

            if (secondsSinceStart > estimationTime + 0 * stepTime + 0 * pauseTime && secondsSinceStart <= estimationTime + 1 * stepTime + 0 * pauseTime)
            {
                if (simocVariableStorage.LastTargetIndex == 0)
                {
                    simocVariableStorage.LastTargetIndex = 1;
                    simocVariableStorage.FrozenCumulativeAverageObs = simocVariableStorage.CumulativeAverageObs;
                }

                currentTargetValue = 1.2 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else if (secondsSinceStart > estimationTime + 1 * stepTime + 0 * pauseTime && secondsSinceStart <= estimationTime + 1 * stepTime + 1 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 1 * stepTime + 1 * pauseTime && secondsSinceStart <= estimationTime + 2 * stepTime + 1 * pauseTime)
            {
                currentTargetValue = 1.4 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else if (secondsSinceStart > estimationTime + 2 * stepTime + 1 * pauseTime && secondsSinceStart <= estimationTime + 2 * stepTime + 2 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 2 * stepTime + 2 * pauseTime && secondsSinceStart <= estimationTime + 3 * stepTime + 2 * pauseTime)
            {
                currentTargetValue = 1.6 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else if (secondsSinceStart > estimationTime + 3 * stepTime + 2 * pauseTime && secondsSinceStart <= estimationTime + 3 * stepTime + 3 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 3 * stepTime + 3 * pauseTime && secondsSinceStart <= estimationTime + 4 * stepTime + 3 * pauseTime)
            {
                currentTargetValue = 1.8 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else if (secondsSinceStart > estimationTime + 4 * stepTime + 3 * pauseTime && secondsSinceStart <= estimationTime + 4 * stepTime + 4 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 4 * stepTime + 4 * pauseTime && secondsSinceStart <= estimationTime + 5 * stepTime + 4 * pauseTime)
            {
                currentTargetValue = 2.0 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else if (secondsSinceStart > estimationTime + 5 * stepTime + 4 * pauseTime && secondsSinceStart <= estimationTime + 5 * stepTime + 5 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 5 * stepTime + 5 * pauseTime && secondsSinceStart <= estimationTime + 6 * stepTime + 5 * pauseTime)
            {
                currentTargetValue = 5.0 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else if (secondsSinceStart > estimationTime + 6 * stepTime + 5 * pauseTime && secondsSinceStart <= estimationTime + 6 * stepTime + 6 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 6 * stepTime + 6 * pauseTime && secondsSinceStart <= estimationTime + 7 * stepTime + 6 * pauseTime)
            {
                currentTargetValue = 10 * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            else
            {
                currentTargetValue = 0;
            }
        }

    }
}
