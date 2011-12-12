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
    class Filt2PIDIrradFB : Filt2Out
    {
        ulong pulseWidthSamples;
        double offVoltage = -0.5;
        double K;
        double Ti;
        double Td;
        double currentFilteredValue;
        double stimFreqHz;
        double stimPulseWidthMSec;
        double currentTargetIntenal;
        double lastErrorIntenal;
        double N = 2;

        public Filt2PIDIrradFB(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            numberOutStreams = 4; // P, I and D streams
            K = c0;
            if (c1 != 0)
                Ti = 1 / c1;
            else
                Ti = 0;
            Td = c2;
            stimFreqHz = c3;
            stimPulseWidthMSec = c4;
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

            // Generate output frequency\
            if (currentTargetIntenal != 0)
            {
                // Derivative Approx
                simocVariableStorage.GenericDouble4 = (Td / (Td + N * stimSrv.DACPollingPeriodSec)) * simocVariableStorage.GenericDouble3 -
                    (K*Td*N)/(Td + N * stimSrv.DACPollingPeriodSec) * (currentFilteredValue - simocVariableStorage.LastFilteredObs);
                
                // Tustin's Integral approximation
                simocVariableStorage.GenericDouble3 += K * Ti * stimSrv.DACPollingPeriodSec * currentErrorIntenal;

                // Proportional part
                simocVariableStorage.GenericDouble3 = K * currentErrorIntenal;
                
                // PID feedback signal
                simocVariableStorage.GenericDouble1 = simocVariableStorage.GenericDouble3 + simocVariableStorage.GenericDouble2 + simocVariableStorage.GenericDouble3;
            }
            else
            {
                simocVariableStorage.GenericDouble4 = 0;
                simocVariableStorage.GenericDouble3 = 0;
                simocVariableStorage.GenericDouble2 = 0;
                simocVariableStorage.GenericDouble1 = 0;
            }

            // Set upper and lower bounds
            if (simocVariableStorage.GenericDouble1 < 0)
                simocVariableStorage.GenericDouble1 = 0;
            if (simocVariableStorage.GenericDouble1 > 5)
                simocVariableStorage.GenericDouble1 = 5;

            // set the currentFeedback array
            currentFeedbackSignals = new double[numberOutStreams];
            currentFeedbackSignals[0] = simocVariableStorage.GenericDouble1;

            // Put P,I and D error components in the rest of the currentFeedBack array
            currentFeedbackSignals[1] = simocVariableStorage.GenericDouble2;
            currentFeedbackSignals[2] = simocVariableStorage.GenericDouble3;
            currentFeedbackSignals[3] = simocVariableStorage.GenericDouble4;

            // Get the pulse width (msec)
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * stimPulseWidthMSec / 1000);

            // Create the output buffer
            List<AuxOutEvent> toAppendAux = new List<AuxOutEvent>();
            List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
            ulong isi;
            if (stimFreqHz != 0)
                isi = (ulong)(hardwareSampFreqHz / stimFreqHz);
            else
                isi = (ulong)(hardwareSampFreqHz / 1.0); ;

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
                toAppendDig.Add(new DigitalOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), (uint)(10000.0 * simocVariableStorage.GenericDouble1)));

                simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                simocVariableStorage.NextAuxEventSample += isi;
            }


            // Send to bit 0 of the digital output port
            SendAuxAnalogOutput(toAppendAux);
            SendAuxDigitalOutput(toAppendDig);

        }
    }
}
