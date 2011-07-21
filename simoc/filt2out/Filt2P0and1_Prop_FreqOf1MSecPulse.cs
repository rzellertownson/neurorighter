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
    class Filt2P0and1_Prop_FreqOf1MSecPulse : Filt2Out
    {
        double stimFrequency = 0;
        double appStimFreq = 0;
        ulong lastStimTime = 0;
        double stimPulseWidthSec = 0.001;
        ulong pulseWidthSamples;
        bool negError = false;
        ulong currentLoad = 0;
        ulong nextAvailableSample;

        public Filt2P0and1_Prop_FreqOf1MSecPulse(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv, cp)
        {
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * stimPulseWidthSec);
        }

        internal override void SendFeedBack()
        {
            base.SendFeedBack();

            // Generate output frequency
            stimFrequency = stimFrequency + c0 * currentError;

            // If culture is firing too fast, need the yellow light
            if (stimFrequency < 0)
            {
                appStimFreq = -stimFrequency;
                negError  = true;
            }

            // Make sure stimulation is not too fast using the second coefficient
            if (stimFrequency > c1)
            {
                stimFrequency = c1;
                appStimFreq = c1;
            }
            else if (stimFrequency < -c1)
            {
                stimFrequency = -c1;
                appStimFreq = c1;
            }

            // What buffer load are we currently processing?
            currentLoad = stimSrv.DigitalOut.GetNumberBuffLoadsCompleted() + 1;
            nextAvailableSample = currentLoad*(ulong)stimSrv.GetBuffSize();

         
            if (appStimFreq != 0)
            {
                // Create the output buffer
                List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
                ulong isi = (ulong)(hardwareSampFreqHz / appStimFreq);

                // Get the current buffer sample and make sure that we are going
                // to produce stimuli that are in the future
                if (lastStimTime + isi < nextAvailableSample)
                    lastStimTime = nextAvailableSample;

                // Find the last sample we are responsible for in this bufferload
                ulong finalSample = lastStimTime + (ulong)stimSrv.GetBuffSize();

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
                if (toAppendDig.Count > 0)
                    SendAuxDigitalOuput(toAppendDig);
            }
        }
    }
}
