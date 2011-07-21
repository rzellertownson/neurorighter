using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.StimSrv;
using simoc.UI;

namespace simoc.filt2out
{
    class Filt2P0_Prop_FreqOf1MSecPulse : Filt2Out
    {
        double stimFrequency = 0;
        ulong lastStimTime = 0;
        double stimPulseWidthSec = 0.001;
        ulong pulseWidthSamples;
        ulong inc = 0;

        public Filt2P0_Prop_FreqOf1MSecPulse(ref NRStimSrv stimSrv, ControlPanel cp)
            : base(ref stimSrv,cp)
        {
            pulseWidthSamples = (ulong)(stimSrv.sampleFrequencyHz * stimPulseWidthSec);
        }

        internal override void SendFeedBack()
        {
            base.SendFeedBack();

            // Generate output frequency
            stimFrequency = stimFrequency + c0 * currentError;

            if (stimFrequency > 0)
            {
                // Create the output buffer
                List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();
                ulong isi = (ulong)(hardwareSampFreqHz / stimFrequency);
                inc++;

                // Make periodic stimulation
                while ((lastStimTime + isi) <= (inc * (ulong)stimSrv.GetBuffSize()))
                {
                    // Get event time
                    lastStimTime += isi;
                    if (lastStimTime < (inc - 1) * (ulong)stimSrv.GetBuffSize())
                        lastStimTime = inc * (ulong)stimSrv.GetBuffSize();

                    // Turn digital port to 100000000000000000000000000000000
                    toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + loadOffset) + pulseWidthSamples, 1));

                    // Turn digital port to 000000000000000000000000000000000
                    toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + loadOffset) + 2 * pulseWidthSamples, 0));
                }


                // Send to bit 0 of the digital output port
                SendAuxDigitalOuput(toAppendDig);
            }

        }

    }
}
