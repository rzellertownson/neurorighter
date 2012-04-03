using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.StimSrv;
using simoc.UI;
using simoc.persistantstate;

namespace simoc.filt2out
{
    class Filt2Zero : Filt2Out
    {
        double stimFrequency = 0;
        double lastStimTime = 0;
        double stimPulseWidthSec = 0.001;
        ulong pulseWidthSamples;
        int inc = 0;
        double offVoltage = -0.5;

        public Filt2Zero(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * stimPulseWidthSec);
            numberOutStreams = 0;
        }

        internal override void SendFeedBack(PersistentSimocVar simocVariableStorage)
        {
            base.SendFeedBack(simocVariableStorage);

            // Create the output buffer
            List<AuxOutEvent> toAppendAux = new List<AuxOutEvent>();
            List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
           
            // zero outputs
            toAppendDig.Add(new DigitalOutEvent((ulong)nextAvailableSample, 0));
            toAppendAux.Add(new AuxOutEvent((ulong)nextAvailableSample, 0, offVoltage));
            toAppendAux.Add(new AuxOutEvent((ulong)nextAvailableSample, 1, offVoltage));
            SendAuxAnalogOutput(toAppendAux);
            SendAuxDigitalOutput(toAppendDig);
        }

    }
}

