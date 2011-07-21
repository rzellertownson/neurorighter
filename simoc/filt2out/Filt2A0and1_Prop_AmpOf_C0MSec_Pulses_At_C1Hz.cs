using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.StimSrv;
using NeuroRighter.Output;
using simoc.UI;

namespace simoc.filt2out
{
    class Filt2A0and1_Prop_AmpOf_C0MSec_Pulses_At_C1Hz : Filt2Out
    {
        double appStimFreq = 0;
        ulong lastStimTime = 0;
        double lightPower = 0;
        double appLightPower = 0;
        double tau = 0;
        ulong pulseWidthSamples;
        bool negError = false;
        ulong currentLoad = 0;
        ulong nextAvailableSample;

        public Filt2A0and1_Prop_AmpOf_C0MSec_Pulses_At_C1Hz(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            //cp.label_ContC0.Text = "PW (msec)";
            //cp.label_ContC1.Text = "freq (Hz)";
            //cp.label_ContC2.Text = "Tau (sec)";
        }

        internal override void SendFeedBack()
        {
            base.SendFeedBack();

            // pulseWidth
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * c0 / 1000);

            // Output frequency
            appStimFreq = c1;

            // Tau for this controller
            tau = GetTauPeriods(c2);
            
            // If find the sign of the error.
            if (currentError < 0)
            {
                negError = true;
            }

            // Calculate Light Power (0 to 5)
            lightPower = lightPower + tau * Math.Abs(currentError);
            if (lightPower > 5)
                lightPower = 5;

            appLightPower = 5 - lightPower; // control signal is inverted

            // What buffer load are we currently processing?
            currentLoad = stimSrv.DigitalOut.GetNumberBuffLoadsCompleted() + 1;
            nextAvailableSample = currentLoad * (ulong)stimSrv.GetBuffSize();


            if (appStimFreq != 0)
            {
                // Create the output buffer
                List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
                List<AuxOutEvent> toAppendAux= new List<AuxOutEvent>();
                ulong isi = (ulong)(hardwareSampFreqHz / appStimFreq);

                // Get the current buffer sample and make sure that we are going
                // to produce stimuli that are in the future
                if (lastStimTime + isi < nextAvailableSample)
                    lastStimTime = nextAvailableSample;

                // Find the last sample we are responsible for in this bufferload
                ulong finalSample = lastStimTime + (ulong)stimSrv.GetBuffSize();

                // Switch the light power to both channels
                toAppendAux.Add(new AuxOutEvent((ulong)(lastStimTime + loadOffset),0,appLightPower));
                toAppendAux.Add(new AuxOutEvent((ulong)(lastStimTime + loadOffset),1,appLightPower));

                // Make periodic stimulation
                while ((lastStimTime + isi) <= (nextAvailableSample + (ulong)stimSrv.GetBuffSize()))
                {
                    // Get event time
                    lastStimTime += isi;

    
                    // Are we using the yellow or the blue light?
                    if (negError)
                    {
                        // Turn digital port to 100000000000000000000000000000000
                        toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + loadOffset) + pulseWidthSamples, 1));
                        
                    }
                    else
                    {
                        // Turn digital port to 010000000000000000000000000000000
                        toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + loadOffset) + pulseWidthSamples, 2));
                    }

                    // Turn digital port to 000000000000000000000000000000000
                    toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + loadOffset) + 2 * pulseWidthSamples, 0));
                }


                // Send to bit 0 of the digital output port
                SendAuxAnalogOuput(toAppendAux);
                SendAuxDigitalOuput(toAppendDig);
            }
        }

    }
}
