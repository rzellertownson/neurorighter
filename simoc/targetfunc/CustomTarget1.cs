using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DataTypes;
using NeuroRighter.StimSrv;
using simoc.UI;
using simoc.persistantstate;

namespace simoc.targetfunc
{
    class CustomTarget1 : TargetFunc
    {
        private double[] ASDRTargets = {5, 10, 20, 40, 80, 160, 320, 640, 0};

        public CustomTarget1(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRStimSrv stimSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref stimSrv)
        {
        }


        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            // target value should increment by 25 every other minute. Otherwise it should be 0.
            double secondsSinceStart = ((double)(currentOutputSample - simocVariableStorage.SimocStartSample)) / outputSampleRateHz;
            double targetPeriodSec = 60;

            // Really very crappy if thinger
            if (secondsSinceStart < targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 1 * targetPeriodSec && secondsSinceStart < 2 * targetPeriodSec)
                currentTargetValue = ASDRTargets[0];
            if (secondsSinceStart >= 2 * targetPeriodSec && secondsSinceStart < 3 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 3 * targetPeriodSec && secondsSinceStart < 4 * targetPeriodSec)
                currentTargetValue = ASDRTargets[1];
            if (secondsSinceStart >= 4 * targetPeriodSec && secondsSinceStart < 5 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 5 * targetPeriodSec && secondsSinceStart < 6 * targetPeriodSec)
                currentTargetValue = ASDRTargets[2];
            if (secondsSinceStart >= 6 * targetPeriodSec && secondsSinceStart < 7 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 7 * targetPeriodSec && secondsSinceStart < 8 * targetPeriodSec)
                currentTargetValue = ASDRTargets[3];
            if (secondsSinceStart >= 8 * targetPeriodSec && secondsSinceStart < 9 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 9 * targetPeriodSec && secondsSinceStart < 10 * targetPeriodSec)
                currentTargetValue = ASDRTargets[4];;
            if (secondsSinceStart >= 10 * targetPeriodSec && secondsSinceStart < 11 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 11 * targetPeriodSec && secondsSinceStart < 12 * targetPeriodSec)
                currentTargetValue = ASDRTargets[5];
            if (secondsSinceStart >= 12 * targetPeriodSec && secondsSinceStart < 13 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 13 * targetPeriodSec && secondsSinceStart < 14 * targetPeriodSec)
                currentTargetValue = ASDRTargets[6];
            if (secondsSinceStart >= 14 * targetPeriodSec && secondsSinceStart < 15 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 15 * targetPeriodSec && secondsSinceStart < 16 * targetPeriodSec)
                currentTargetValue = ASDRTargets[7];
            if (secondsSinceStart >= 16 * targetPeriodSec && secondsSinceStart < 17 * targetPeriodSec)
                currentTargetValue = 0;
            if (secondsSinceStart >= 17 * targetPeriodSec && secondsSinceStart < 18 * targetPeriodSec)
                currentTargetValue = ASDRTargets[8];
            if (secondsSinceStart >= 18 * targetPeriodSec && secondsSinceStart < 19 * targetPeriodSec)
                currentTargetValue = 0;
        }



    }
}
