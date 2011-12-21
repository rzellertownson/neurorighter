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
    class OneMinuteStepsUNFRAbs : TargetFunc
    {
        private double[] muliplierTargets = { 0.1000,0.1668,0.2783,0.4642,0.7743,1.2915,2.1544,3.5938,5.9948,10.0000 };
        private double stepTime; // seconds
        private double pauseTime = 60; // seconds

        public OneMinuteStepsUNFRAbs(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref  NRDataSrv datSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref datSrv) 
        {
            stepTime = cp.TargetMultiplier;
        }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            double secondsSinceStart = (double)(currentOutputSample / outputSampleRateHz) - simocVariableStorage.LastTargetSwitchedSec;

          
            if (secondsSinceStart >  0 * stepTime + 0 * pauseTime && secondsSinceStart <=  1 * stepTime + 0 * pauseTime)
            {
                currentTargetValue = muliplierTargets[0] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 2
            else if (secondsSinceStart >  1 * stepTime + 0 * pauseTime && secondsSinceStart <=  1 * stepTime + 1 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  1 * stepTime + 1 * pauseTime && secondsSinceStart <=  2 * stepTime + 1 * pauseTime)
            {
                currentTargetValue = muliplierTargets[1] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 3
            else if (secondsSinceStart >  2 * stepTime + 1 * pauseTime && secondsSinceStart <=  2 * stepTime + 2 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  2 * stepTime + 2 * pauseTime && secondsSinceStart <=  3 * stepTime + 2 * pauseTime)
            {
                currentTargetValue = muliplierTargets[2] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 4
            else if (secondsSinceStart >  3 * stepTime + 2 * pauseTime && secondsSinceStart <=  3 * stepTime + 3 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  3 * stepTime + 3 * pauseTime && secondsSinceStart <=  4 * stepTime + 3 * pauseTime)
            {
                currentTargetValue = muliplierTargets[3] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 5
            else if (secondsSinceStart >  4 * stepTime + 3 * pauseTime && secondsSinceStart <=  4 * stepTime + 4 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  4 * stepTime + 4 * pauseTime && secondsSinceStart <=  5 * stepTime + 4 * pauseTime)
            {
                currentTargetValue = muliplierTargets[4] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 6
            else if (secondsSinceStart >  5 * stepTime + 4 * pauseTime && secondsSinceStart <=  5 * stepTime + 5 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  5 * stepTime + 5 * pauseTime && secondsSinceStart <=  6 * stepTime + 5 * pauseTime)
            {
                currentTargetValue = muliplierTargets[5] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 7
            else if (secondsSinceStart >  6 * stepTime + 5 * pauseTime && secondsSinceStart <=  6 * stepTime + 6 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  6 * stepTime + 6 * pauseTime && secondsSinceStart <=  7 * stepTime + 6 * pauseTime)
            {
                currentTargetValue = muliplierTargets[6] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 8
            else if (secondsSinceStart >  7 * stepTime + 6 * pauseTime && secondsSinceStart <=  7 * stepTime + 7 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  7 * stepTime + 7 * pauseTime && secondsSinceStart <=  8 * stepTime + 7 * pauseTime)
            {
                currentTargetValue = muliplierTargets[7] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 9
            else if (secondsSinceStart >  8 * stepTime + 7 * pauseTime && secondsSinceStart <=  8 * stepTime + 8 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  8 * stepTime + 8 * pauseTime && secondsSinceStart <=  9 * stepTime + 8 * pauseTime)
            {
                currentTargetValue = muliplierTargets[8] * simocVariableStorage.FrozenCumulativeAverageObs;
            }
            // Step 10
            else if (secondsSinceStart >  9 * stepTime + 8 * pauseTime && secondsSinceStart <=  9 * stepTime + 9 * pauseTime)
            {
                currentTargetValue = 0;
            }
            else if (secondsSinceStart >  9 * stepTime + 9 * pauseTime && secondsSinceStart <=  10 * stepTime + 9 * pauseTime)
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
