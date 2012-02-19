using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.UI;
using simoc.srv;
using simoc.persistantstate;
using NeuroRighter.StimSrv;
using NeuroRighter.DataTypes;

namespace simoc.filt2out
{
    class Filt2IBangBangFB : Filt2Out
    {
        ulong pulseWidthSamples;
        double offVoltage = -0.5;
        double Ti;
        double K;
        double currentFilteredValue;
        double maxStimPowerVolts;
        double stimPulseWidthMSec;
        double currentTargetIntenal;
        double lastErrorIntenal;
        double N = 2;
        double maxStimFreq;
        double maxStimPulseWidthMSec;

        public Filt2IBangBangFB(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            numberOutStreams = 7; // P and I streams
            K = c0;
            if (c1 != 0)
                Ti = 1 / c1;
            else
                Ti = 0;
            maxStimFreq = c3;
            maxStimPulseWidthMSec = c4;
            maxStimPowerVolts = c5;
        }

        internal override void CalculateError(ref double currentError, double currentTarget, double currentFilt)
        {
            currentFilteredValue = currentFilt;
            base.CalculateError(ref currentError, currentTarget, currentFilt);
            if (currentTarget != 0)
            {
                lastErrorIntenal = currentError;
                currentError = (currentTarget - currentFilt);  // currentTarget;
            }
            else
            {
                lastErrorIntenal = currentError;
                currentError = 0;
            }
            currentErrorIntenal = currentError;
            currentTargetIntenal = currentTarget;
        }


        internal override void SendFeedBack(PersistentSimocVar simocVariableStorage)
        {
            base.SendFeedBack(simocVariableStorage);

            simocVariableStorage.LastErrorValue = lastErrorIntenal;

            // Generate output frequency
            if (currentTargetIntenal != 0)
            {
                // Derivative Approx
                simocVariableStorage.GenericDouble4 = 0;

                // Tustin's Integral approximation
                simocVariableStorage.GenericDouble3 += Ti * stimSrv.DACPollingPeriodSec * currentErrorIntenal;

                // Proportional Term
                simocVariableStorage.GenericDouble2 = 0;

                // PI feedback signal
                simocVariableStorage.GenericDouble1 = simocVariableStorage.GenericDouble2 + simocVariableStorage.GenericDouble3;
            }
            else
            {
                simocVariableStorage.GenericDouble4 = 0;
                simocVariableStorage.GenericDouble3 = 0;
                simocVariableStorage.GenericDouble2 = 0;
                simocVariableStorage.GenericDouble1 = 0;
            }

            // Set bang-bange control signal
            if (simocVariableStorage.GenericDouble1 <= 0)
                simocVariableStorage.GenericDouble1 = 0;
            else
                simocVariableStorage.GenericDouble1 = 1;

            // Get the pulse width (msec)
            stimPulseWidthMSec = maxStimPulseWidthMSec * simocVariableStorage.GenericDouble1;
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * stimPulseWidthMSec / 1000);

            // Stim current (volts ~ amps)
            double stimPowerVolts = maxStimPowerVolts * simocVariableStorage.GenericDouble1;

            // Get stim frequency
            double stimFreqHz = maxStimFreq * simocVariableStorage.GenericDouble1 + 1;

            // set the currentFeedback array
            currentFeedbackSignals = new double[numberOutStreams];

            // Put P,I and D error components in the rest of the currentFeedBack array
            currentFeedbackSignals[0] = simocVariableStorage.GenericDouble1;
            currentFeedbackSignals[1] = simocVariableStorage.GenericDouble2;
            currentFeedbackSignals[2] = simocVariableStorage.GenericDouble3;
            currentFeedbackSignals[3] = simocVariableStorage.GenericDouble4; 
            currentFeedbackSignals[4] = stimFreqHz;
            currentFeedbackSignals[5] = stimPulseWidthMSec;
            currentFeedbackSignals[6] = stimPowerVolts; 

            // Create the output buffer
            List<AuxOutEvent> toAppendAux = new List<AuxOutEvent>();
            List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
            ulong isi = (ulong)(hardwareSampFreqHz / stimFreqHz);

            // Get the current buffer sample and make sure that we are going
            // to produce stimuli that are in the future
            if (simocVariableStorage.NextAuxEventSample < nextAvailableSample)
            {
                simocVariableStorage.NextAuxEventSample = nextAvailableSample;
            }

            // Make periodic stimulation
            while (simocVariableStorage.NextAuxEventSample <= (nextAvailableSample + 2*(ulong)stimSrv.GetBuffSize()))
            {
                // Send a V_ctl = simocVariableStorage.GenericDouble1 volt pulse to channel 0 for c2 milliseconds.
                toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), 0, stimPowerVolts));
                toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset) + pulseWidthSamples, 0, offVoltage));

                // Encode light power as 10000*V_ctl = port-state
                toAppendDig.Add(new DigitalOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), (uint)(10000.0 * simocVariableStorage.GenericDouble1)));
                toAppendDig.Add(new DigitalOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset) + pulseWidthSamples, 0));

                simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                simocVariableStorage.NextAuxEventSample += isi;
            }

            // Send to bit 0 of the digital output port
            SendAuxAnalogOutput(toAppendAux);
            SendAuxDigitalOutput(toAppendDig);

        }
    }
}
