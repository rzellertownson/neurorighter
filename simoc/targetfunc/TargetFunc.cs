using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using simoc.UI;
using System.Windows.Forms;

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

        public TargetFunc(ControlPanel cp, double DACPollingPeriodSec, ulong numTargetSamplesGenerated)
        {
            // Grab parameters off the form
            this.meanValue = cp.numericEdit_TargetMean.Value;
            this.standardDev = cp.numericEdit_TargetSigma.Value;
            this.frequency = cp.numericEdit_TargetFreq.Value;
            this.numTargetSamplesGenerated = numTargetSamplesGenerated;
            this.DACPollingPeriodSec = DACPollingPeriodSec;
        }

        internal virtual void GetTargetValue(ref double currentTargetValue)
        {
            // Use this to send out the current target value that you want your
            // observation to match
        }

    }
}
