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
    class Filt2PropPowerFB : Filt2Out
    {
        ulong pulseWidthSamples;
        double offVoltage = -0.5;
        double K;
        double Ti;
        bool targetZero = false;

        public Filt2PropPowerFB(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            numberOutStreams = 1;
            K = c0;
            Ti = 1/c3;
        }

        internal override void CalculateError(ref double currentError, double currentTarget, double currentFilt)
        {
            base.CalculateError(ref currentError, currentTarget, currentFilt);
            currentError = (currentTarget - currentFilt);  // currentTarget;
            currentErrorIntenal = currentError;

            if (currentTarget == 0)
                targetZero = true;
        }


        internal override void SendFeedBack(PersistentSimocVar simocVariableStorage)
        {
            base.SendFeedBack(simocVariableStorage);

            // Generate output frequency\
            simocVariableStorage.GenericDouble2 += currentErrorIntenal;
            if (targetZero)
                simocVariableStorage.GenericDouble1 = 0;
            else
                simocVariableStorage.GenericDouble1 = simocVariableStorage.GenericDouble1 + c0 * currentErrorIntenal;

            // Set upper and lower bounds
            if (simocVariableStorage.GenericDouble1 < 0)
                simocVariableStorage.GenericDouble1 = 0;
            if (simocVariableStorage.GenericDouble1 > 5)
                simocVariableStorage.GenericDouble1 = 5;

            // set the currentFeedback array
            currentFeedbackSignals = new double[numberOutStreams];
            currentFeedbackSignals[0] = simocVariableStorage.GenericDouble1;

            // Get the pulse width (msec)
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * c2 / 1000);

            // Create the output buffer
            List<AuxOutEvent> toAppendAux = new List<AuxOutEvent>();
            List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
            ulong isi = (ulong)(hardwareSampFreqHz / c1);

            // Get the current buffer sample and make sure that we are going
            // to produce stimuli that are in the future
            if (simocVariableStorage.NextAuxEventSample < nextAvailableSample)
            {
                simocVariableStorage.NextAuxEventSample = nextAvailableSample;
            }

            // Make periodic stimulation
            while (simocVariableStorage.NextAuxEventSample <= (nextAvailableSample + (ulong)stimSrv.GetBuffSize()))
            {
                // Send a V_ctl = simocVariableStorage.GenericDouble1 volt pulse to channel 0 for c2 milliseconds.
                toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), 0, simocVariableStorage.GenericDouble1));
                toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset) + pulseWidthSamples, 0, offVoltage));
                
                // Encode light power as 10000*V_ctl = port-state
                toAppendDig.Add(new DigitalOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset),(uint)(10000.0*simocVariableStorage.GenericDouble1)));

                simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                simocVariableStorage.NextAuxEventSample += isi;
            }


            // Send to bit 0 of the digital output port
            SendAuxAnalogOutput(toAppendAux);
            SendAuxDigitalOutput(toAppendDig);

        }
    }


}
