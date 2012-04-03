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
    class RandomBlocks : TargetFunc
    {

        // Change me!
        private double[] minMax = {2 , 6};
        private double stepTime = 30; //60; // = 10;  // seconds
        private int totalSteps = 10; // total number of random steps

        public RandomBlocks(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref  NRDataSrv datSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref datSrv) { }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            double secondsSinceStart = (double)(currentOutputSample / outputSampleRateHz) - simocVariableStorage.LastTargetSwitchedSec;
            

            if (!simocVariableStorage.TargetOn)
            {
                simocVariableStorage.LastTargetValue = ((minMax[1] - minMax[0]) * simocVariableStorage.RandGen1.NextDouble()) + minMax[0];
            }

            if (simocVariableStorage.GenericInt1 < totalSteps)
            {

            startStep:

                if (secondsSinceStart <= (simocVariableStorage.GenericInt1 + 1) * stepTime)
                {
                    simocVariableStorage.TargetOn = true;
                    currentTargetValue = simocVariableStorage.LastTargetValue;
                }
                else
                {
                    simocVariableStorage.GenericInt1++;
                    simocVariableStorage.LastTargetValue = ((minMax[1] - minMax[0]) * simocVariableStorage.RandGen1.NextDouble()) + minMax[0];
                    goto startStep;
                }
            }
            else
            {
                currentTargetValue = 0;
            }
        }
    }
}
