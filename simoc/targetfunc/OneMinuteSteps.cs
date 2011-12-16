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
    class OneMinuteSteps : TargetFunc
    {
        private double[] muliplierTargets = { 1.0, 1.2915, 1.6680, 2.1542, 2.7821, 3.5931, 4.6405, 5.9932, 7.7403, 10 };
        private double estimationTime = 30; // seconds before steps to estimate nominal obs
        private double stepTime = 60; // seconds
        private double pauseTime = 30; // seconds

        public OneMinuteSteps(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref  NRDataSrv datSrv)
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

            if (secondsSinceStart > estimationTime + 0 * stepTime + 0 * pauseTime && secondsSinceStart <= estimationTime + 1 * stepTime + 0 * pauseTime)
            {
                simocVariableStorage.TargetOn = true;
                currentTargetValue = muliplierTargets[0] * simocVariableStorage.FrozenCumulativeAverageObs;
            }

            // Step 2
            else if (secondsSinceStart > estimationTime + 1 * stepTime + 0 * pauseTime && secondsSinceStart <= estimationTime + 1 * stepTime + 1 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 1 * stepTime + 1 * pauseTime && secondsSinceStart <= estimationTime + 2 * stepTime + 1 * pauseTime)
            {
                currentTargetValue = muliplierTargets[1] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 3
            else if (secondsSinceStart > estimationTime + 2 * stepTime + 1 * pauseTime && secondsSinceStart <= estimationTime + 2 * stepTime + 2 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 2 * stepTime + 2 * pauseTime && secondsSinceStart <= estimationTime + 3 * stepTime + 2 * pauseTime)
            {
                currentTargetValue = muliplierTargets[2] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 4
            else if (secondsSinceStart > estimationTime + 3 * stepTime + 2 * pauseTime && secondsSinceStart <= estimationTime + 3 * stepTime + 3 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 3 * stepTime + 3 * pauseTime && secondsSinceStart <= estimationTime + 4 * stepTime + 3 * pauseTime)
            {
                currentTargetValue = muliplierTargets[3] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 5
            else if (secondsSinceStart > estimationTime + 4 * stepTime + 3 * pauseTime && secondsSinceStart <= estimationTime + 4 * stepTime + 4 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 4 * stepTime + 4 * pauseTime && secondsSinceStart <= estimationTime + 5 * stepTime + 4 * pauseTime)
            {
                currentTargetValue = muliplierTargets[4] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 6
            else if (secondsSinceStart > estimationTime + 5 * stepTime + 4 * pauseTime && secondsSinceStart <= estimationTime + 5 * stepTime + 5 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 5 * stepTime + 5 * pauseTime && secondsSinceStart <= estimationTime + 6 * stepTime + 5 * pauseTime)
            {
                currentTargetValue = muliplierTargets[5] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 7
            else if (secondsSinceStart > estimationTime + 6 * stepTime + 5 * pauseTime && secondsSinceStart <= estimationTime + 6 * stepTime + 6 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 6 * stepTime + 6 * pauseTime && secondsSinceStart <= estimationTime + 7 * stepTime + 6 * pauseTime)
            {
                currentTargetValue = muliplierTargets[6] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 8
            else if (secondsSinceStart > estimationTime + 7 * stepTime + 6 * pauseTime && secondsSinceStart <= estimationTime + 7 * stepTime + 7 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 7 * stepTime + 7 * pauseTime && secondsSinceStart <= estimationTime + 8 * stepTime + 7 * pauseTime)
            {
                currentTargetValue = muliplierTargets[7] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 9
            else if (secondsSinceStart > estimationTime + 8 * stepTime + 7 * pauseTime && secondsSinceStart <= estimationTime + 8 * stepTime + 8 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 8 * stepTime + 8 * pauseTime && secondsSinceStart <= estimationTime + 9 * stepTime + 8 * pauseTime)
            {
                currentTargetValue = muliplierTargets[8] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 10
            else if (secondsSinceStart > estimationTime + 9 * stepTime + 8 * pauseTime && secondsSinceStart <= estimationTime + 9 * stepTime + 9 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart > estimationTime + 9 * stepTime + 9 * pauseTime && secondsSinceStart <= estimationTime + 10 * stepTime + 9 * pauseTime)
            {
                currentTargetValue = muliplierTargets[9] * simocVariableStorage.FrozenCumulativeAverageObs;
            }

            else
            {
                currentTargetValue = 0;
            }
        }

    }
}
