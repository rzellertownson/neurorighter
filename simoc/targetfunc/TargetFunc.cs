using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using simoc.UI;
using System.Windows.Forms;
using NeuroRighter.StimSrv;
using simoc.persistantstate;

namespace simoc.targetfunc
{
    /// <summary>
    /// Base clase for target function creation.
    /// <author> Jon Newman</author>
    /// </summary>
    internal abstract class TargetFunc
    {
        protected double meanValue;
        protected double standardDev;
        protected double frequency;
        protected int daqPollingPeriodSec;
        protected ulong numTargetSamplesGenerated;
        protected double DACPollingPeriodSec;
        protected ulong currentOutputSample;
        protected double outputSampleRateHz;

        public TargetFunc(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated, ref NRStimSrv stimSrv)
        {
            // Grab parameters off the form
            this.meanValue = (double)cp.TargetMean;
            this.standardDev = (double)cp.TargetStd;
            this.frequency = (double)cp.TargetFreqHz;
            this.numTargetSamplesGenerated = numTargetSamplesGenerated;
            this.DACPollingPeriodSec = DACPollingPeriodSec;
            this.currentOutputSample = stimSrv.DigitalOut.GetCurrentSample();
            this.outputSampleRateHz = stimSrv.sampleFrequencyHz;
        }

        internal virtual void GetTargetValue(ref double currentTargetValue, PersistentSimocVar simocVariableStorage)
        {
            // Use this to send out the current target value that you want your
            // observation to match
        }

    }
}
