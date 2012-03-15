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
    class TwelveHourStep : TargetFunc
    {
        //private double estimationTime = 3 * 60 * 60; // seconds before steps to estimate nominal obs
        private double estimationTime = 300; // seconds before clamping (amount of time for let CNQX take effect)
        private double stepTime = 24 * 60 * 60; // seconds

        public TwelveHourStep(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref  NRDataSrv datSrv)
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

            if (secondsSinceStart > estimationTime + 0 * stepTime && secondsSinceStart <= estimationTime + 1 * stepTime)
            {
                simocVariableStorage.TargetOn = true;
                currentTargetValue = meanValue;
            }
            else
            {
                currentTargetValue = 0;
            }
        }

    }
}
