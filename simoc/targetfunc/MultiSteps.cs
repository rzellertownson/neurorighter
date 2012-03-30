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
    class MultiSteps : TargetFunc
    {
        // Change me!
        //private double[] targets = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0};
        private double[] targets = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5, 10.0, 10.5, 11.0, 11.5, 12.0};
        private double estimationTime = 14400; //600; //300; //= 5;//  seconds before steps to estimate nominal obs
        private double stepTime = 300; //60; // = 10;  // seconds
        private double pauseTime = 1500; //300; //= 20; // seconds
        
        // Don't worry about me!
        private double prePulseTime = 120; // = 10; // Time that we warm up the culture
        private double prePulseWidth= 20; // = 5; //  


        public MultiSteps(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref  NRDataSrv datSrv)
            : base(cp, DACPollingPeriodSec, numTargetSamplesGenerated, ref datSrv) { }

        internal override void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            double secondsSinceStart = (double)(currentOutputSample / outputSampleRateHz) - simocVariableStorage.LastTargetSwitchedSec;
            

            if (!simocVariableStorage.TargetOn)
            {
                simocVariableStorage.GenerateRandPerm(targets.Length);
            }

            if (simocVariableStorage.GenericInt1 < targets.Length)
            {

            startStep:
                if (secondsSinceStart <= estimationTime)
                {
                    currentTargetValue = 0;
                }
                else if (secondsSinceStart > estimationTime + simocVariableStorage.GenericInt1 * stepTime + simocVariableStorage.GenericInt1 * pauseTime
                    &&
                    secondsSinceStart <= estimationTime + (simocVariableStorage.GenericInt1 + 1) * stepTime + simocVariableStorage.GenericInt1 * pauseTime)
                {
                    simocVariableStorage.TargetOn = true;
                    
                    // Pick a random target
                    currentTargetValue = targets[simocVariableStorage.RandPerm[simocVariableStorage.GenericInt1]];
                }
                else if (secondsSinceStart > estimationTime + (simocVariableStorage.GenericInt1 + 1) * stepTime + simocVariableStorage.GenericInt1 * pauseTime
                    &&
                    secondsSinceStart <= estimationTime + (simocVariableStorage.GenericInt1 + 1) * stepTime + (simocVariableStorage.GenericInt1 + 1) * pauseTime)
                {

                    currentTargetValue = 0;

                    // Pre-pulse
                    if (secondsSinceStart > (estimationTime + (simocVariableStorage.GenericInt1 + 1) * stepTime + (simocVariableStorage.GenericInt1 + 1) * pauseTime) - prePulseTime
                        &&
                        secondsSinceStart <= (estimationTime + (simocVariableStorage.GenericInt1 + 1) * stepTime + (simocVariableStorage.GenericInt1 + 1) * pauseTime) - (prePulseTime - prePulseWidth))
                        
                    {
                        currentTargetValue = 0; //warmup pulse
                    }

                }
                else
                {
                    simocVariableStorage.GenericInt1++;
                    goto startStep;
                }
            }
        }
    }
}
