using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.StimSrv;
using simoc.UI;

namespace simoc.filt2out
{
    class Filt2None : Filt2Out
    {
        double stimFrequency = 0;
        double lastStimTime = 0;
        double stimPulseWidthSec = 0.001;
        ulong pulseWidthSamples;
        int inc = 0;

        public Filt2None(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * stimPulseWidthSec);

            cp.label_ContC0.Text = "null";
            cp.label_ContC1.Text = "null";
            cp.label_ContC2.Text = "null";

        }

        internal override void SendFeedBack()
        {
            base.SendFeedBack();

            // Do nothing
        }

    }
}
