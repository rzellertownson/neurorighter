// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using NationalInstruments;
using NationalInstruments.DAQmx;

namespace NeuroRighter
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    class StimPulse
    {
        internal int width1; //Width of first phase of pulse, in microseconds
        internal int width2; //Width of second phase
        internal double amplitude1; //Amplitude of first phase, in volts
        internal double amplitude2; //Amplitude of second phase
        internal int channel; //Channel on which stim. pulse occurs, 1-based (if -1, the train is multiple channels)
        internal int numPulses;
        internal double rate;
        internal double offsetVoltage;
        internal int interphaseLength; //Length between phase 1 and 2, in num. samples
        internal int prePadding; //Num. samples before pulse to set channel to offset voltage
        internal int postPadding; //Number of samples as above, but after pulse

        internal double[,] analogPulse;
        internal UInt32[] digitalData;

        internal const int NUM_SAMPLES_BLANKING = 10;
        internal const int STIM_SAMPLING_FREQ = 100000; //In Hz
        private const int BLANKING_BIT_32bitPort = 31; //DO line that will have the blanking signal
        private const int BLANKING_BIT_8bitPort = 7;
        private const int PORT_OFFSET_32bitPort = 7;
        private const int PORT_OFFSET_8bitPort = 0;
        internal const int NUM_SAMPLES_ENCODING = 3;

        //Constructor for a single pulse
        /// <summary>
        /// 
        /// </summary>
        /// <param name="width1">First phase duration (microseconds)</param>
        /// <param name="width2">Second phase duration (microseconds)</param>
        /// <param name="amp1">First phase amplitude (Volts)</param>
        /// <param name="amp2">Second phase amplitude (Volts)</param>
        /// <param name="ch">Channel to stimulate</param>
        /// <param name="offsetVoltage">Offset voltage for entire waveform (volts)</param>
        /// <param name="interphaseLength">Duration between phase 1 and phase 2, equal to offset voltage (microseconds)</param>
        /// <param name="prePadding">Duration before phase 1, set to offset voltage (microseconds)</param>
        /// <param name="postPadding">Duration after phase 2, set to offset voltage (microseconds)</param>
        /// <param name="generateData">Populate waveform (needs to be done at some point, but takes memory)</param>
        internal StimPulse(int width1, int width2, double amp1, double amp2, int ch, double offsetVoltage, int interphaseLength, int prePadding, int postPadding, bool generateData) :
            this(width1, width2, amp1, amp2, ch, 1, 1, offsetVoltage, interphaseLength, prePadding, postPadding, generateData) {}

        //Constructor for pulse train, where all pulses are on same channel and have same properties
        internal StimPulse(int width1, int width2, double amp1, double amp2, int ch, int numPulses, double rate, double offsetVoltage, int interPhaseLength, int prePadding, int postPadding, bool generateData)
        {
            this.width1 = width1;
            this.width2 = width2;
            this.amplitude1 = amp1;
            this.amplitude2 = amp2;
            this.channel = ch;
            this.numPulses = numPulses;
            this.rate = rate;
            this.interphaseLength = Convert.ToInt32((double)STIM_SAMPLING_FREQ * (double)interphaseLength / 1000000);
            this.prePadding = Convert.ToInt32((double)STIM_SAMPLING_FREQ * (double)prePadding / 1000000);
            this.postPadding = Convert.ToInt32((double)STIM_SAMPLING_FREQ * (double)postPadding / 1000000);
            
            if (generateData)
                this.populate();
        }

        //Constructor for pulse train with multiple channels
        internal StimPulse(int inWidth1, int inWidth2, double inAmp1, double inAmp2, int[] inCh, double inRate, double inOffsetVoltage, int inInterphaseLength, int inPrePadding, int inPostPadding)
        {
            this.width1 = inWidth1;
            this.width2 = inWidth2;
            this.amplitude1 = inAmp1;
            this.amplitude2 = inAmp2;
            this.channel = -1;
            this.numPulses = inCh.GetLength(0);
            this.rate = inRate;
            this.offsetVoltage = inOffsetVoltage;
            this.interphaseLength = Convert.ToInt32((double)STIM_SAMPLING_FREQ * (double)inInterphaseLength / 1000000);
            this.prePadding = Convert.ToInt32((double)STIM_SAMPLING_FREQ * (double)inPrePadding / 1000000);
            this.postPadding = Convert.ToInt32((double)STIM_SAMPLING_FREQ * (double)inPostPadding / 1000000);

            int offset = (int)(STIM_SAMPLING_FREQ / inRate); //The num pts. b/w stim pulses
            int size = Convert.ToInt32((((double)inWidth1 + (double)inWidth2) / 1000000.0) * STIM_SAMPLING_FREQ + this.prePadding + this.postPadding + this.interphaseLength); //Num. pts. in pulse

            int rowOffset = 0; //Some stimulators have extra room for trigger channels
            int numRows = 2;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                rowOffset = 2;
                numRows = 4;
            }

            //Construct the waveforms for each pulse as one big waveform
            this.analogPulse = new double[numRows, offset * this.numPulses];
            digitalData = new UInt32[offset * this.numPulses];

            for (int i = 0; i < this.numPulses; ++i)
            {
                UInt32 temp = channel2MUX((double)inCh[i]); //Get data bits lined up to control MUXes

                //v1 and v2 encode channel number
                double v1, v2;
                v1 = Math.Ceiling((double)inCh[i] / 8.0);
                v2 = (double)((inCh[i] - 1) % 8) + 1.0;

                //Setup digital waveform
                UInt32 temp_noEn = channel2MUX_noEN((double)inCh[i]);
                UInt32 temp_blankOnly = Convert.ToUInt32(Math.Pow(2, (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort)));

                //stimPulse has row 1 for delivered stim., row 2 for event encoding
                for (int j = offset * i; j < this.prePadding + offset * i; ++j)
                    analogPulse[rowOffset + 0, j] = this.offsetVoltage;
                for (int j = this.prePadding + offset * i; j < this.prePadding + Convert.ToInt32(((double)inWidth1 / 1000000.0) * STIM_SAMPLING_FREQ) + offset * i; ++j)
                    analogPulse[rowOffset + 0, j] = inAmp1 + this.offsetVoltage;
                for (int j = this.prePadding + Convert.ToInt32(((double)inWidth1 / 1000000.0) * STIM_SAMPLING_FREQ) + offset * i; j < this.prePadding + Convert.ToInt32(((double)inWidth1 / 1000000.0) * STIM_SAMPLING_FREQ) + this.interphaseLength + offset * i; ++j)
                    analogPulse[rowOffset + 0, j] = this.offsetVoltage;
                for (int j = this.prePadding + Convert.ToInt32(((double)inWidth1 / 1000000.0) * STIM_SAMPLING_FREQ) + this.interphaseLength + offset * i; j < size - this.postPadding + offset * i; ++j)
                    analogPulse[rowOffset + 0, j] = inAmp2 + this.offsetVoltage;
                for (int j = size - this.postPadding + offset * i; j < size + offset * i; ++j)
                    analogPulse[rowOffset + 0, j] = this.offsetVoltage;
                for (int j = this.prePadding + offset * i; j < 20 + this.prePadding + offset * i; ++j)
                    analogPulse[rowOffset + 1, j] = v1;
                for (int j = 20 + this.prePadding + offset * i; j < 40 + this.prePadding + offset * i; ++j)
                    analogPulse[rowOffset + 1, j] = v2;
                for (int j = 40 + this.prePadding + offset * i; j < 60 + this.prePadding + offset * i; ++j)
                    analogPulse[rowOffset + 1, j] = inAmp1;
                double pulseWidthEncoding = ((double)(width1) / 100.0 > 10.0 ? -1 : (double)(width1) / 100.0);
                for (int j = 60 + this.prePadding + offset * i; j < 80 + this.prePadding + offset * i; ++j)
                    analogPulse[rowOffset + 1, j] = pulseWidthEncoding;

                //Make digital waveform
                for (int j = 1 + offset * i; j <= NUM_SAMPLES_BLANKING + offset * i; ++j)
                    digitalData[j] = temp_blankOnly;
                digitalData[NUM_SAMPLES_BLANKING + 1 + offset * i] = temp_noEn;
                for (int j = NUM_SAMPLES_BLANKING + 2 + offset * i; j < size + NUM_SAMPLES_BLANKING + 2 + offset * i; ++j)
                    digitalData[j] = temp;
                digitalData[size + NUM_SAMPLES_BLANKING + 2 + offset * i] = temp_noEn;
                for (int j = size + NUM_SAMPLES_BLANKING + 3 + offset * i; j < size + 2 * NUM_SAMPLES_BLANKING + 3 + offset * i; ++j)
                    digitalData[j] = temp_blankOnly;
            }
        }

        internal void populate() { populate(false); }
        internal void populate(bool SendTrigger)
        {
            //Get data bits lined up to control MUXes
            UInt32 temp = channel2MUX((double)channel);

            //Setup voltage waveform, pos. then neg.
            int size = Convert.ToInt32((((double)width1 + (double)width2) / 1000000.0) * STIM_SAMPLING_FREQ + prePadding + postPadding + interphaseLength); //Num. pts. in pulse
            //What was that doing? Convert width to seconds, divide by sample duration, mult. by
            //two since the pulse is biphasic, add padding to both sides

            //v1 and v2 encode channel number
            double v1 = Math.Ceiling((double)channel / 8.0);
            double v2 = (double)((channel - 1) % 8) + 1.0;

            UInt32 temp_noEn = channel2MUX_noEN((double)channel);
            UInt32 temp_blankOnly = Convert.ToUInt32(Math.Pow(2, (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort)));

            int rowOffset = 0; //Some stimulators have extra room for trigger channels
            int numRows = 2;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                numRows = 4;
                rowOffset = 2;
            }

            if (numPulses > 1)
            {
                int offset = (int)(STIM_SAMPLING_FREQ / rate); //The num pts. b/w stim pulses
                //analogPulse = new double[2, offset * numPulses];
                //digData = new byte[offset * numPulses];
                analogPulse = new double[numRows, offset]; //Only make one pulse of train, the padding zeros will ensure proper rate when sampling is regenerative
                digitalData = new UInt32[offset];

                for (int j = 0; j < prePadding; ++j)
                    analogPulse[rowOffset + 0, j] = offsetVoltage;
                for (int j = prePadding; j < prePadding + Convert.ToInt32(((double)width1 / 1000000.0) * STIM_SAMPLING_FREQ); ++j)
                    analogPulse[rowOffset + 0, j] = amplitude1 + offsetVoltage;
                for (int j = prePadding + Convert.ToInt32(((double)width1 / 1000000.0) * STIM_SAMPLING_FREQ); j < prePadding + Convert.ToInt32(((double)width1 / 1000000.0) * STIM_SAMPLING_FREQ) + interphaseLength; ++j)
                    analogPulse[rowOffset + 0, j] = offsetVoltage;
                for (int j = prePadding + Convert.ToInt32(((double)width1 / 1000000.0) * STIM_SAMPLING_FREQ) + interphaseLength; j < size - postPadding; ++j)
                    analogPulse[rowOffset + 0, j] = amplitude2 + offsetVoltage;
                for (int j = size - postPadding; j < size; ++j)
                    analogPulse[rowOffset + 0, j] = offsetVoltage;
                for (int j = prePadding; j < 20 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = v1;
                for (int j = 20 + prePadding; j < 40 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = v2;
                for (int j = 40 + prePadding; j < 60 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = amplitude1;
                double pulseWidthEncoding = ((double)(width1) / 100.0 > 10.0 ? -1 : (double)(width1) / 100.0);
                for (int j = 60 + prePadding; j < 80 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = pulseWidthEncoding;

                //Add a trigger for all duration of "pulse" that's outside of actual pulse (meaning a 100 Hz train will have ~9 ms of trigger)
                if (SendTrigger)
                    for (int j = size; j < analogPulse.GetLength(1); ++j)
                        analogPulse[0, j] = 5.0;

                for (int j = 1; j <= NUM_SAMPLES_BLANKING; ++j)
                    digitalData[j] = temp_blankOnly;
                digitalData[NUM_SAMPLES_BLANKING + 1] = temp_noEn;
                for (int j = NUM_SAMPLES_BLANKING + 2; j < size + NUM_SAMPLES_BLANKING + 2; ++j)
                    digitalData[j] = temp;
                digitalData[size + NUM_SAMPLES_BLANKING + 2] = temp_noEn;
                for (int j = size + NUM_SAMPLES_BLANKING + 3; j < size + 2 * NUM_SAMPLES_BLANKING + 3; ++j)
                    digitalData[j] = temp_blankOnly;
            }
            else //Only one pulse.  This requires less data.
            {
                if (size >= 80 + prePadding)
                    analogPulse = new double[numRows, size];
                else
                    analogPulse = new double[numRows, 81 + prePadding]; //This ensures that pulse info can be stored in second row of stim pulse
                digitalData = new UInt32[size + 2 * NUM_SAMPLES_BLANKING + 4];

                //stimPulse has row 1 for delivered stim., row 2 for event encoding
                for (int j = 0; j < prePadding; ++j)
                    analogPulse[rowOffset + 0, j] = offsetVoltage;
                for (int j = prePadding; j < (prePadding + Convert.ToInt32(STIM_SAMPLING_FREQ * (double)width1 / 1000000.0)); ++j)
                    analogPulse[rowOffset + 0, j] = amplitude1 + offsetVoltage;
                for (int j = (prePadding + Convert.ToInt32(STIM_SAMPLING_FREQ * (double)width1 / 1000000.0)); j < (prePadding + Convert.ToInt32(STIM_SAMPLING_FREQ * ((double)width1 + (double)width2) / 1000000.0) + interphaseLength); ++j)
                    analogPulse[rowOffset + 0, j] = offsetVoltage;
                for (int j = (prePadding + Convert.ToInt32(STIM_SAMPLING_FREQ * (double)width1 / 1000000.0) + interphaseLength); j < size - postPadding; ++j)
                    analogPulse[rowOffset + 0, j] = amplitude2 + offsetVoltage;
                for (int j = size - postPadding; j < size; ++j)
                    analogPulse[rowOffset + 0, j] = offsetVoltage;
                for (int j = prePadding; j < 20 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = v1;
                for (int j = 20 + prePadding; j < 40 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = v2;
                for (int j = 40 + prePadding; j < 60 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = amplitude1;
                double pulseWidthEncoding = ((double)(width1) / 100.0 > 10.0 ? -1 : (double)(width1) / 100.0);
                for (int j = 60 + prePadding; j < 80 + prePadding; ++j)
                    analogPulse[rowOffset + 1, j] = pulseWidthEncoding;
                analogPulse[1, 80 + prePadding] = 0.0;

                //Make digital waveform, use one time bin (10 us) to let things settle.
                //Order is first 0's, then blanking, then everything but En, the everything, then reverse
                for (int i = 1; i <= NUM_SAMPLES_BLANKING; ++i)
                    digitalData[i] = temp_blankOnly;
                digitalData[NUM_SAMPLES_BLANKING + 1] = temp_noEn;
                for (int j = NUM_SAMPLES_BLANKING + 2; j < size + NUM_SAMPLES_BLANKING + 2; ++j)
                    digitalData[j] = temp;
                digitalData[size + NUM_SAMPLES_BLANKING + 2] = temp_noEn;
                for (int i = size + NUM_SAMPLES_BLANKING + 3; i < size + 2 * NUM_SAMPLES_BLANKING + 3; ++i)
                    digitalData[i] = temp_blankOnly;
            }
        }

        internal static UInt32 channel2MUX(double channel)
        {
            if (Properties.Settings.Default.ChannelMapping == "invitro")
                channel = MEAChannelMappings.ch2stimChannel[(short)(--channel)];

            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8:  return _channel2MUX_8chMUX(channel, true, true);
                case 16: return _channel2MUX_16chMUX(channel, true, true);
                default: return 0; //Error!
            }
        }

        internal static UInt32 channel2MUX_noEN(double channel)
        {
            if (Properties.Settings.Default.ChannelMapping == "invitro")
                channel = MEAChannelMappings.ch2stimChannel[(short)(--channel)];

            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8:  return _channel2MUX_8chMUX(channel, false, false);
                case 16: return _channel2MUX_16chMUX(channel, false, false);
                default: return 0; //Error!
            }
        }

        internal static UInt32 channel2MUX(double channel, bool enable, bool trigger)
        {
            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8: return _channel2MUX_8chMUX(channel, enable, trigger);
                case 16: return _channel2MUX_16chMUX(channel, enable, trigger);
                default: return 0; //Error!
            }
        }


        //Convert channel number into control signal for deMUX
        private static UInt32 _channel2MUX_16chMUX(double channel, bool enable, bool trigger) //'channel' is 1-based
        {
            bool[] data = new bool[32];

            int portOffset = (Properties.Settings.Default.StimPortBandwidth == 32 ? PORT_OFFSET_32bitPort : PORT_OFFSET_8bitPort);
            if (enable)
            {
                //Pick which mux to use
                double tempDbl = (channel - 1) / 16.0;
                int tempInt = (int)Math.Floor(tempDbl);
                data[portOffset + 5 + tempInt] = true;
            }
            if (trigger)
                data[portOffset] = true;  //Always true, since this is start trigger for AO

            channel = 1 + ((channel - 1) % 16.0);
            if (channel / 8.0 > 1) { data[portOffset + 4] = true; channel -= 8; }
            if (channel / 4.0 > 1) { data[portOffset + 3] = true; channel -= 4; }
            if (channel / 2.0 > 1) { data[portOffset + 2] = true; channel -= 2; }
            if (channel / 1.0 > 1) { data[portOffset + 1] = true; }

            int blankingBit = (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort);
            data[blankingBit] = true; //Always true, since this is the "something's happening" signal
            
            UInt32 temp = 0;
            for (int p = 0; p < 32; ++p)
                temp += (UInt32)Math.Pow(2, p) * Convert.ToUInt32(data[p]);

            return temp;
        }

        private static UInt32 _channel2MUX_8chMUX(double channel, bool enable, bool trigger) //'channel' is 1-based
        {
            bool[] data = new bool[32];

            int portOffset = (Properties.Settings.Default.StimPortBandwidth == 32 ? PORT_OFFSET_32bitPort : PORT_OFFSET_8bitPort);
            if (enable)
            {
                //Pick which mux to use
                double tempDbl = (channel - 1) / 8.0;
                int tempInt = (int)Math.Floor(tempDbl);
                data[portOffset + 4 + tempInt] = true;
            }
            if (trigger)
                data[portOffset] = true;  //Always true, since this is start trigger for AO

            channel = 1 + ((channel - 1) % 8.0);

            if (channel / 4.0 > 1) { data[portOffset + 3] = true; channel -= 4; }
            if (channel / 2.0 > 1) { data[portOffset + 2] = true; channel -= 2; }
            if (channel / 1.0 > 1) { data[portOffset + 1] = true; }

            int blankingBit = (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort);
            data[blankingBit] = true; //Always true, since this is the "something's happening" signal

            UInt32 temp = 0;
            for (int p = 0; p < 32; ++p)
                temp += (UInt32)Math.Pow(2, p) * Convert.ToUInt32(data[p]);

            return temp;
        }

        //private static UInt32 _channel2MUX_16chMUX_noEN(double channel) //'channel' is 1-based
        //{
        //    bool[] data = new bool[32];

        //    int portOffset = (Properties.Settings.Default.StimPortBandwidth == 32 ? PORT_OFFSET_32bitPort : PORT_OFFSET_8bitPort);

        //    //Pick which mux to use
        //    channel = 1 + ((channel - 1) % 16.0);

        //    if (channel / 8.0 > 1) { data[portOffset + 4] = true; channel -= 8; }
        //    if (channel / 4.0 > 1) { data[portOffset + 3] = true; channel -= 4; }
        //    if (channel / 2.0 > 1) { data[portOffset + 2] = true; channel -= 2; }
        //    if (channel / 1.0 > 1)   data[portOffset + 1] = true;

        //    int blankingBit = (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort);
        //    data[blankingBit] = true; //Always true, since this is the "something's happening" signal

        //    UInt32 temp = 0;
        //    for (int p = 0; p < 32; ++p)
        //        temp += (UInt32)Math.Pow(2, p) * Convert.ToUInt32(data[p]);

        //    return temp;
        //}

        //private static UInt32 _channel2MUX_8chMUX_noEN(double channel) //'channel' is 1-based
        //{
        //    bool[] data = new bool[32];

        //    int portOffset = (Properties.Settings.Default.StimPortBandwidth == 32 ? PORT_OFFSET_32bitPort : PORT_OFFSET_8bitPort);
        //    //Pick which mux to use
        //    channel = 1 + ((channel - 1) % 8.0);
        //    if (channel / 4.0 > 1) { data[portOffset + 3] = true; channel -= 4; }
        //    if (channel / 2.0 > 1) { data[portOffset + 2] = true; channel -= 2; }
        //    if (channel / 1.0 > 1)   data[portOffset + 1] = true;

        //    int blankingBit = (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort);
        //    data[blankingBit] = true; //Always true, since this is the "something's happening" signal

        //    UInt32 temp = 0;
        //    for (int p = 0; p < 32; ++p)
        //        temp += (UInt32)Math.Pow(2, p) * Convert.ToUInt32(data[p]);

        //    return temp;
        //}

        internal static Byte[] convertTo8Bit(UInt32[] data)
        {
            Byte[] output = new Byte[data.Length];
            for (int i = 0; i < data.Length; ++i) output[i] = (Byte)data[i];
            return output;
        }
    }
}
