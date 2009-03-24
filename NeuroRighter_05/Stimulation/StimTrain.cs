// NeuroRighter v0.04
// Copyright (c) 2008 John Rolston
//
// This file is part of NeuroRighter v0.04.
//
// NeuroRighter v0.04 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter v0.04 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter v0.04.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal class StimTrain
    {
        private List<Int32> width1; //in microseconds
        private List<Int32> width2;
        private List<Int32> channel;
        private List<Int32> interphaseLength;
        private List<Int32> prePadding;
        private List<Int32> postPadding;
        private List<Double> amp1; //in volts
        private List<Double> amp2;
        private List<Double> offsetVoltage;
        private List<Int32> interpulseIntervals; //in samples; construed as time between first sample of pulse 1 and first sample of pulse 2

        internal Double[,] analogPulse;
        internal UInt32[] digitalData;

        //Constants
        private const int BLANKING_BIT_32bitPort = 31; //DO line that will have the blanking signal
        private const int BLANKING_BIT_8bitPort = 7;
        private const int PORT_OFFSET_32bitPort = 7;
        private const int PORT_OFFSET_8bitPort = 0;
        private const int WATCH_TIME = 100; //In ms--how long to watch firing rate after last stim pulse

        internal StimTrain(Int32 pulseWidth, Double amplitude, List<Int32> channels, List<Int32> interpulseIntervals)
        {
            //Interpulse intervals come in as us

            this.channel = new List<int>(channels.Count);
            for (int i = 0; i < channels.Count; ++i)
                this.channel.Add(channels[i]);
            
            width1 = new List<int>(channels.Count);
            width2 = new List<int>(channels.Count);
            interphaseLength = new List<int>(channels.Count);
            prePadding = new List<int>(channels.Count);
            postPadding = new List<int>(channels.Count);
            amp1 = new List<double>(channels.Count);
            amp2 = new List<double>(channels.Count);
            offsetVoltage = new List<double>(channels.Count);
            this.interpulseIntervals = new List<Int32>(interpulseIntervals.Count);
            
            for (int c = 0; c < channels.Count; ++c)
            {
                width1.Add(pulseWidth);
                width2.Add(pulseWidth);
                amp1.Add(amplitude);
                amp2.Add(-amplitude);

                offsetVoltage.Add(0.0); //Default to no offset voltage

                interphaseLength.Add(0);
                prePadding.Add(Convert.ToInt32((double)StimPulse.STIM_SAMPLING_FREQ * (double)100 / 1000000)); //Fix at 100 us
                postPadding.Add(Convert.ToInt32((double)StimPulse.STIM_SAMPLING_FREQ * (double)100 / 1000000)); //Fix at 100 us
            }
            for (int c = 0; c < interpulseIntervals.Count; ++c) //There'll be one less 
                this.interpulseIntervals.Add(Convert.ToInt32((double)StimPulse.STIM_SAMPLING_FREQ * (double)interpulseIntervals[c] / 1000)); 
        }

        /*************************************************************
         * POPULATE
         * Argument 'addTrigger' specifies whether we add start/stop
         * 100 ms triggers, for summing responses post-stimulus
         *************************************************************/
        internal void populate() { populate(false); /*false is default*/ }
        internal void populate(Boolean addTrigger)
        {
            //Compute total length of pulse, in samples
            int totalLength = 0;
            for (int c = 0; c < interpulseIntervals.Count; ++c)
                totalLength += interpulseIntervals[c]; //All interpulse intervals
            totalLength += prePadding[0]; //The 'off' time of first pulse
            totalLength += postPadding[channel.Count - 1]; //'off' time of last pulse
            totalLength += width1[channel.Count - 1] + width2[channel.Count - 1] +
                interphaseLength[channel.Count - 1];  //and length of last pulse
            if (addTrigger) //If trigger is being added to end, add that length too
                totalLength += 1 + StimPulse.STIM_SAMPLING_FREQ * WATCH_TIME / 1000;

            //This experiment REQUIRES triggers, therefore MUST use 32-bit ports!!!
            int numRows = 4;
            if (Properties.Settings.Default.StimPortBandwidth != 32)
            {
                System.Windows.Forms.MessageBox.Show("Must use 32-bit port for this experiment, since triggers must be used.", "Port-size error");
                return;
            }

            //Set aside space for analog pulse
            analogPulse = new double[numRows, totalLength]; //Only make one pulse of train, the padding zeros will ensure proper rate when sampling is regenerative
            digitalData = new UInt32[totalLength + 2*(StimPulse.NUM_SAMPLES_BLANKING + 2)];

            //Bookkeeping variable for constructing pulse
            int offset = 0;

            for (int c = 0; c < channel.Count; ++c) //for each pulse
            {
                //Setup voltage waveform, pos. then neg.
                int size = Convert.ToInt32((((double)width1[c] + (double)width2[c]) / 1000000.0) * StimPulse.STIM_SAMPLING_FREQ + prePadding[c] + postPadding[c] + interphaseLength[c]); //Num. pts. in pulse
                //What was that doing? Convert width to seconds, divide by sample duration, mult. by
                //two since the pulse is biphasic, add padding to both sides

                #region AnalogPulseCreation
                //v1 and v2 encode channel number
                double v1 = Math.Ceiling((double)channel[c] / 8.0);
                double v2 = (double)((channel[c] - 1) % 8) + 1.0;

                for (int j = 0; j < prePadding[c]; ++j)
                    analogPulse[2, j + offset] = offsetVoltage[c];
                for (int j = prePadding[c]; j < prePadding[c] + Convert.ToInt32(((double)width1[c] / 1000000.0) * StimPulse.STIM_SAMPLING_FREQ); ++j)
                    analogPulse[2, j + offset] = amp1[c] + offsetVoltage[c];
                for (int j = prePadding[c] + Convert.ToInt32(((double)width1[c] / 1000000.0) * StimPulse.STIM_SAMPLING_FREQ); j < prePadding[c] + Convert.ToInt32(((double)width1[c] / 1000000.0) * StimPulse.STIM_SAMPLING_FREQ) + interphaseLength[c]; ++j)
                    analogPulse[2, j + offset] = offsetVoltage[c];
                for (int j = prePadding[c] + Convert.ToInt32(((double)width1[c] / 1000000.0) * StimPulse.STIM_SAMPLING_FREQ) + interphaseLength[c]; j < size - postPadding[c]; ++j)
                    analogPulse[2, j + offset] = amp2[c] + offsetVoltage[c];
                for (int j = size - postPadding[c]; j < size; ++j)
                    analogPulse[2, j + offset] = offsetVoltage[c];
                for (int j = prePadding[c]; j < 20 + prePadding[c]; ++j)
                    analogPulse[3, j + offset] = v1;
                for (int j = 20 + prePadding[c]; j < 40 + prePadding[c]; ++j)
                    analogPulse[3, j + offset] = v2;
                for (int j = 40 + prePadding[c]; j < 60 + prePadding[c]; ++j)
                    analogPulse[3, j + offset] = amp1[c];
                for (int j = 60 + prePadding[c]; j < 80 + prePadding[c]; ++j)
                    analogPulse[3, j + offset] = (double)(width1[c]) / 100.0;

                //Add trigger, if applicable
                if (addTrigger && c == channel.Count - 1)
                {
                    for (int j = size; j < StimPulse.STIM_SAMPLING_FREQ * WATCH_TIME / 1000; ++j)
                        analogPulse[0, j + offset] = 4.0; //4 Volts, TTL-compatible
                    analogPulse[0, analogPulse.GetLength(1) - 1] = 0.0;
                }
                #endregion
                
                #region DigitalPulseCreation
                //Get data bits lined up to control MUXes
                UInt32 temp;
                if (c == 0)
                    temp = StimPulse.channel2MUX((double)channel[c]);
                else
                    temp = StimPulse.channel2MUX((double)channel[c], true, false); //select channel without start trigger for AO
                UInt32 temp_noEn = StimPulse.channel2MUX_noEN((double)channel[c]);
                UInt32 temp_blankOnly = Convert.ToUInt32(Math.Pow(2, (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort)));

                for (int j = 1; j <= StimPulse.NUM_SAMPLES_BLANKING; ++j)
                    digitalData[j + offset] = temp_blankOnly;
                digitalData[StimPulse.NUM_SAMPLES_BLANKING + 1 + offset] = temp_noEn;
                for (int j = StimPulse.NUM_SAMPLES_BLANKING + 2; j < size + StimPulse.NUM_SAMPLES_BLANKING + 2; ++j)
                    digitalData[j + offset] = temp;
                digitalData[size + StimPulse.NUM_SAMPLES_BLANKING + 2 + offset] = temp_noEn;
                for (int j = size + StimPulse.NUM_SAMPLES_BLANKING + 3; j < size + 2 * StimPulse.NUM_SAMPLES_BLANKING + 3; ++j)
                    digitalData[j + offset] = temp_blankOnly;
                #endregion

                //Update offset to account for interphase length
                if (c < interpulseIntervals.Count)
                    offset += interpulseIntervals[c]; //this has one less member than all the stimpulses
                //NB: this form of offset changing assumes homogeneous prepaddings
            }
        }

        internal void unpopulate()
        {
            digitalData = null;
            analogPulse = null;
        }

        internal void shuffleChannelOrder()
        {
            Random rand = new Random();

            List<int> channelNew = new List<int>(channel.Count);
            for (int i = 0; channel.Count > 0; ++i)
            {
                int index = (int)Math.Floor(channel.Count * rand.NextDouble());
                channelNew.Add(channel[index]);
                channel.RemoveAt(index);
            }
            channel = channelNew;
        }
    }
}
