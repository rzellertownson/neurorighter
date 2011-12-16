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
    class Filt2RelayPowerFB : Filt2Out
    {
        ulong pulseWidthSamples;
        double offVoltage = -0.5;
        double deltaError;
        double currentFilteredValue;
        double stimPowerVolts;
        double stimPulseWidthMSec;
        double currentTargetIntenal;
        double lastErrorIntenal;
        double N = 2;
        double Ku = 0;
        double Tu = 0;
        double relayAmp;
        double maxStimPowerVolts;
        double maxStimFreq;
        double maxStimPulseWidthMSec;

        public Filt2RelayPowerFB(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            numberOutStreams = 7; // P and I streams
            relayAmp = c0;
            deltaError = c1;
            cp.SetControllerResultText(Tu,Ku);
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

            // Generate output frequency\
            if (currentTargetIntenal != 0)
            {
                // Remember the last output
                double lastOutput = simocVariableStorage.GenericDouble1;

                // Relay FB signal
                if (currentErrorIntenal > deltaError)
                {
                    // Relay on output
                    simocVariableStorage.GenericDouble1 = relayAmp;

                    // Ultimate Gain Data collection
                    simocVariableStorage.ErrorUpStateAmpList.Add(currentErrorIntenal);
                    if (simocVariableStorage.ErrorDownStateAmpList.Count > 0)
                    {
                        simocVariableStorage.ErrorSignalAmplitudeList.Add(simocVariableStorage.ErrorDownStateAmpList.Max());
                        simocVariableStorage.ErrorDownStateAmpList.Clear();
                    }

                }
                else if (currentErrorIntenal < -deltaError)
                {
                    // Relay off output
                    simocVariableStorage.GenericDouble1 = 0;

                    // Ultimate Gain Data collection
                    simocVariableStorage.ErrorDownStateAmpList.Add(currentErrorIntenal);
                    if (simocVariableStorage.ErrorUpStateAmpList.Count > 0)
                    {
                        simocVariableStorage.ErrorSignalAmplitudeList.Add(simocVariableStorage.ErrorUpStateAmpList.Max());
                        simocVariableStorage.ErrorUpStateAmpList.Clear();
                    }
                }

                // Update ultimage period estimate
                if (lastOutput == 0 && simocVariableStorage.GenericDouble1 == relayAmp)
                {
                    simocVariableStorage.RelayCrossingTimeList.Add(stimSrv.AuxOut.GetTime()/1000);
                    if (simocVariableStorage.RelayCrossingTimeList.Count > 1)
                    {
                        int lastIndex =simocVariableStorage.RelayCrossingTimeList.Count;
                        simocVariableStorage.UltimatePeriodList.Add(simocVariableStorage.RelayCrossingTimeList[lastIndex-1] - simocVariableStorage.RelayCrossingTimeList[lastIndex - 2]);
                        simocVariableStorage.UltimatePeriodEstimate = simocVariableStorage.UltimatePeriodList.Average();
                        Tu = simocVariableStorage.UltimatePeriodEstimate;
                    }
                }

                // Update ultimage gain estimate
                if (simocVariableStorage.ErrorSignalAmplitudeList.Count >= 2)
                {
                    double currentPostiveAmp = simocVariableStorage.ErrorSignalAmplitudeList.Where(x => x > 0).Average();
                    double currentNegativeAmp = simocVariableStorage.ErrorSignalAmplitudeList.Where(x => x < 0).Average();
                    double a = (currentPostiveAmp + Math.Abs(currentNegativeAmp)) / 2;
                    simocVariableStorage.UltimateGainEstimate = (4*relayAmp)/(Math.PI*a);
                    Ku = simocVariableStorage.UltimateGainEstimate;
                }

            }
            else
            {
                simocVariableStorage.GenericDouble2 = 0;
                simocVariableStorage.GenericDouble2 = 0;
                simocVariableStorage.GenericDouble1 = 0;
            }

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
            currentFeedbackSignals[1] = 0;
            currentFeedbackSignals[2] = 0;
            currentFeedbackSignals[3] = 0;
            currentFeedbackSignals[4] = stimFreqHz;
            currentFeedbackSignals[5] = stimPulseWidthMSec;
            currentFeedbackSignals[6] = stimPowerVolts;

            // Create the output buffer
            List<AuxOutEvent> toAppendAux = new List<AuxOutEvent>();
            List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
            ulong isi = (ulong)(hardwareSampFreqHz / stimFreqHz);


            // Update the next sample for the new isi 
            simocVariableStorage.NextAuxEventSample += isi;

            // Get the current buffer sample and make sure that we are going
            // to produce stimuli that are in the future
            if (simocVariableStorage.NextAuxEventSample < nextAvailableSample)
            {
                simocVariableStorage.NextAuxEventSample = nextAvailableSample;
            }

            // Send output
            while (simocVariableStorage.NextAuxEventSample <= (nextAvailableSample + 2*(ulong)stimSrv.GetBuffSize()))
            {
                // Send a V_ctl = simocVariableStorage.GenericDouble1 volt pulse to channel 0 for c2 milliseconds.
                toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), 0, stimPowerVolts));
                toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset) + pulseWidthSamples, 0, offVoltage));

                // Encode light power as 10000*V_ctl = port-state
                toAppendDig.Add(new DigitalOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), (uint)(10000.0 * simocVariableStorage.GenericDouble1)));

                simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                simocVariableStorage.NextAuxEventSample += isi;
            }

            // Remove the last isi since it was never sent
            simocVariableStorage.NextAuxEventSample -= isi; 

            // Send to bit 0 of the digital output port
            SendAuxAnalogOutput(toAppendAux);
            SendAuxDigitalOutput(toAppendDig);

        }

        internal void ClearTuningMeasurements(PersistentSimocVar simocVariableStorage)
        {
            simocVariableStorage.RelayCrossingTimeList.Clear();
            simocVariableStorage.UltimatePeriodList.Clear();
            simocVariableStorage.ErrorDownStateAmpList.Clear();
            simocVariableStorage.ErrorUpStateAmpList.Clear();
            simocVariableStorage.ErrorSignalAmplitudeList.Clear();
            simocVariableStorage.UltimateGainEstimate = 0;
            simocVariableStorage.UltimatePeriodEstimate = 0;
        }
    }
}