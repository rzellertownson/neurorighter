// NeuroRighter
// Copyright (c) 2008-2012 Potter Lab
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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ExtensionMethods;

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// Electrical stimulation event type.
    /// </summary>
    [Serializable]
    public sealed class StimulusOutEvent:NREvent
    {

        //DO line that will have the blanking signal for different hardware configurations
        private const int BLANKING_BIT_32bitPort = 31;
        private const int BLANKING_BIT_8bitPort = 7;
        private const int PORT_OFFSET_32bitPort = 7;
        private const int PORT_OFFSET_8bitPort = 0;

        // Internal to NR
        private Int16 channel; //1 based
        private double[] waveform; //Stim voltage
        private double[] analogEncode;
        private UInt32[] digitalEncode;

        /// <summary>
        /// Electrical stimulation event to be interpreted by NeuroRighter's all channel stimulation board.
        /// </summary>
        /// <param name="channel">Channel to stimulate</param>
        /// <param name="time"> Time sample that stimulation should be applied </param>
        /// <param name="waveform"> Stimulation waveform. </param>
        public StimulusOutEvent(int channel, ulong time, double[] waveform)
        {
            try
            {
                this.channel = (short)channel;
                this.sampleIndex = time;
                this.waveform = new double[waveform.Length];
                for (int i = 0; i < waveform.Length; i++)
                {
                    this.waveform[i] = waveform[i];
                }

                this.analogEncode = new double[2];
                this.digitalEncode = new uint[3];
                this.analogEncode[0] = Math.Ceiling((double)channel / 8.0);
                this.analogEncode[1] = (double)((channel - 1) % 8) + 1.0;

                this.digitalEncode[0] = Convert.ToUInt32(Math.Pow(2, (Properties.Settings.Default.StimPortBandwidth == 32 ? BLANKING_BIT_32bitPort : BLANKING_BIT_8bitPort)));
                this.digitalEncode[1] = channel2MUX_noEN((double)channel);
                this.digitalEncode[2] = channel2MUX((double)channel);
                this.sampleDuration = (uint)waveform.Length;
                if (DigitalEncode[1] == 0 || DigitalEncode[2] == 0)
                    throw new Exception("NR stimulation exception: you are attempting to stimulate on channel " + channel.ToString() + " which has resulted in an error. \n" +
                        "this could be caused by your Properties.Settings.Default.MUXChannels, which is set to " + Properties.Settings.Default.MUXChannels.ToString() + " and must be either 16 or 8"
                            );
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// Electrical stimulation event to be interpreted by NeuroRighter's all channel stimulation board.
        /// </summary>
        /// <param name="channel">Channel to stimulate</param>
        /// <param name="time"> Time sample that stimulation should be applied </param>
        /// <param name="waveform"> Stimulation waveform. </param>
        /// <param name="sampleDuration"> The duration of the stimulation waveform</param>
        public StimulusOutEvent(int channel, ulong time, double[] waveform, uint sampleDuration)
            :this(channel,time,waveform)
        {
            this.sampleDuration = sampleDuration;
        }

        //User should need to only call the constructor, first instance in which this data is being used by code that knows the 
        //stim sampling frequency, perform this operation.  shouldn't be a problem if it is called twice, assuming using the same
        //sampling frequency data.
       
        #region MUX conversion Functions
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
        #endregion MUX conversion Functions


        #region Accessors

        /// <summary>
        /// 1-based channel number that this stimulus event will occur on.
        /// </summary>
        public Int16 Channel
        {
            get
            {
                return channel;
            }
        }
        
        /// <summary>
        /// returns the voltage waveform placed across the output electrode
        /// </summary>
        public double[] Waveform //Stim voltage
        {
            get
            {
                return waveform;
            }
        }

        internal double[] AnalogEncode
        {
            get
            {
                return analogEncode;
            }
        }

        internal UInt32[] DigitalEncode
        {
            get
            {
                return digitalEncode;
            }
        }


        #endregion
    }
}


