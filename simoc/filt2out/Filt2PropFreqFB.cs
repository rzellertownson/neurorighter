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
    class Filt2PropFreqFB : Filt2Out
    {
        double maxStimFrequency = 50;
        double minStimFrequency = 1;
        ulong pulseWidthSamples;
        double offVoltage = -0.5;
        private ulong nextStimulusTime;

        public Filt2PropFreqFB(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            numberOutStreams = 1;

        }

        internal override void CalculateError(ref double currentError, double currentTarget, double currentFilt)
        {
            base.CalculateError(ref currentError, currentTarget, currentFilt);
            currentError = currentTarget - currentFilt;
            currentErrorInt = currentError;
        }

        internal override void SendFeedBack(PersistentSimocVar simocVariableStorage)
        {
            base.SendFeedBack(simocVariableStorage);

            // Generate output frequency\
            simocVariableStorage.GenericDouble1 = simocVariableStorage.GenericDouble1 + c0 * currentErrorInt;

            // Set upper and lower bounds
            if (simocVariableStorage.GenericDouble1 < 0)
                simocVariableStorage.GenericDouble1 = 0;
            if (simocVariableStorage.GenericDouble1 > maxStimFrequency)
                simocVariableStorage.GenericDouble1 = maxStimFrequency;

            // set the currentFeedback array
            currentFeedbackSignals = new double[numberOutStreams];
            currentFeedbackSignals[0] = simocVariableStorage.GenericDouble1;

            // Get the pulse width
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * c2 / 1000);

            if (simocVariableStorage.GenericDouble1 > 0)
            {
                // Create the output buffer
                List<AuxOutEvent> toAppendAux = new List<AuxOutEvent>();
                ulong isi = (ulong)(hardwareSampFreqHz / simocVariableStorage.GenericDouble1);


                // If increase in stimulation rate overtakes events that were originally prescribed to occur
                if (simocVariableStorage.LastAuxEventSample + isi < simocVariableStorage.NextAuxEventSample)
                {
                    simocVariableStorage.NextAuxEventSample = simocVariableStorage.LastAuxEventSample + isi;
                    stimSrv.AuxOut.EmptyOuterBuffer();
                }

                // Get the current buffer sample and make sure that we are going
                // to produce stimuli that are in the future
                if (simocVariableStorage.NextAuxEventSample < nextAvailableSample)
                {
                    simocVariableStorage.NextAuxEventSample = nextAvailableSample;
                }

                // Make periodic stimulation
                while (simocVariableStorage.NextAuxEventSample <= (nextAvailableSample + (ulong)stimSrv.GetBuffSize()))
                {
                    // Send a c1 volt pulse to channel 0 for c2 milliseconds.
                    toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset), 0, c1));
                    toAppendAux.Add(new AuxOutEvent((ulong)(simocVariableStorage.NextAuxEventSample + loadOffset) + pulseWidthSamples, 0, offVoltage));

                    simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                    simocVariableStorage.NextAuxEventSample += isi;
                }


                // Send to bit 0 of the digital output port
                SendAuxAnalogOutput(toAppendAux);
            }

        }

    }
}
