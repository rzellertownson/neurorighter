using System;
using System.Collections.Generic;
using System.Text;
using NationalInstruments.DAQmx;
using NationalInstruments;
using NationalInstruments.Analysis;
using System.IO;

namespace NeuroRighter
{
    class StimWave
    {
        // Internal Declarations go here
        internal int channel;
        internal double[] waveform;
        internal double[,] analogPulse;
        internal UInt32[] digitalData;

        // Hardware parameters
        internal const int NUM_SAMPLES_BLANKING = 10;
        internal const int STIM_SAMPLING_FREQ = 100000; //In Hz
        private const int BLANKING_BIT_32bitPort = 31; //DO line that will have the blanking signal
        private const int BLANKING_BIT_8bitPort = 7;
        private const int PORT_OFFSET_32bitPort = 7;
        private const int PORT_OFFSET_8bitPort = 0;
        internal const int NUM_SAMPLES_ENCODING = 3;

        //Constructor for a single arbitrary waveform
        /// <summary>
        /// StimWave will read a file containing voltage or current command information and then create a stimulation waveform that can be read by the
        /// the neurorighter system
        /// </summary>
        /// <param name="ch">Channel to stimulate</param>
        /// <param name="waveform">Vector of voltages (in volts) that specifies the stimulation command voltage - the offset</param>
        internal StimWave(int ch, double[] waveform) : 
            this(ch, waveform, false){}
                //Constructor for a single arbitrary waveform
        /// <summary>
        /// StimWave will read a file containing voltage or current command information and then create a stimulation waveform that can be read by the
        /// the neurorighter system
        /// </summary>
        /// <param name="ch">Channel to stimulate</param>
        /// <param name="waveform">Vector of voltages (in volts) that specifies the stimulation command voltage - the offset</param>
        /// <param name="generateData">Populate waveform (needs to be done at some point, but takes memory)</param>
        internal StimWave(int ch, double[] waveform, bool generateData)
        {
            this.channel = ch;
            this.waveform = waveform;
            if (generateData)
                this.populate();
        }

        internal void populate() { populate(false); }
        internal void populate(bool SendTrigger)
        {
            //Get data bits lined up to control MUXes
            UInt32 temp = channel2MUX((double)channel);
            
            // find the length of the command waveform
            int size = waveform.Length; // number of points in the pulse

            //v1 and v2 encode channel number in row column format
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

            if (size >= 80)
            {
                analogPulse = new double[numRows, size];
            }
            else
            {
                analogPulse = new double[numRows, 81];//This ensures that pulse info can be stored in second row of stim pulse
            }

            digitalData = new UInt32[size + 2 * NUM_SAMPLES_BLANKING + 4];

            // Create stimulation waveform
            for (int j = 0; j < size; ++j)
                analogPulse[rowOffset + 0, j] = waveform[j];

            // Create pulse endcoding stimulation channel, time, duration
            for (int j = 0; j < 20; ++j)
                analogPulse[rowOffset + 1, j] = v1;
            for (int j = 20; j < 40; ++j)
                analogPulse[rowOffset + 1, j] = v2;
            for (int j = 40; j < 60; ++j)
                analogPulse[rowOffset + 1, j] = 0; // For now the ampltitude pulse is set to 0 since this is an abitrary waveform
            double stimduration = 1000 * size / STIM_SAMPLING_FREQ; //Convert stim time to ms
            double pulseWidthEncoding = ((double)(stimduration) / 100.0 > 10.0 ? -1 : (double)(stimduration) / 100.0); //Make sure encoding is less than 10 volts (max DAQ voltage)
            for (int j = 60; j < 80; ++j)
                analogPulse[rowOffset + 1, j] = pulseWidthEncoding;
            analogPulse[1, 80] = 0.0;


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
        internal static UInt32 channel2MUX(double channel)
        {
            if (Properties.Settings.Default.ChannelMapping == "invitro")
                channel = MEAChannelMappings.ch2stimChannel[(short)(--channel)];

            switch (Properties.Settings.Default.MUXChannels)
            {
                case 8: return _channel2MUX_8chMUX(channel, true, true);
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
                case 8: return _channel2MUX_8chMUX(channel, false, false);
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

        internal static Byte[] convertTo8Bit(UInt32[] data)
        {
            Byte[] output = new Byte[data.Length];
            for (int i = 0; i < data.Length; ++i) output[i] = (Byte)data[i];
            return output;
        }
    }
}
