using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using NeuroRighter.DataTypes;

namespace NeuroRighter.Output
{   
    // called when the 2+requested number of buffer loads have occured
    internal delegate void StimulationCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    internal delegate void QueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    internal delegate void DAQLoadCompletedHandler(object sender, EventArgs e);

    public class StimBuffer : NROutBuffer<StimulusOutEvent>
    {


        //DO line that will have the blanking signal for different hardware configurations
        private const int BLANKING_BIT_32bitPort = 31;
        private const int BLANKING_BIT_8bitPort = 7;
        private const int PORT_OFFSET_32bitPort = 7;
        private const int PORT_OFFSET_8bitPort = 0;
        

        // Constants for different systems
        private int NUM_SAMPLES_BLANKING;
        private int NumAOChannels;
        private int RowOffset;

        //Stuff that gets defined with input arguments to constructor
        private int[] TimeVector;
        private int[] ChannelVector;
        private double[,] WaveMatrix;
        private uint BUFFSIZE;
        private uint LengthWave;

        
        internal StimBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int NUM_SAMPLES_BLANKING, int queueThreshold)
            :base( INNERBUFFSIZE, STIM_SAMPLING_FREQ,  queueThreshold)
        {
            
            this.NUM_SAMPLES_BLANKING = NUM_SAMPLES_BLANKING;
            // What are the buffer offset settings for this system?
            NumAOChannels = 2;
            RowOffset = 0;
            if (Properties.Settings.Default.StimPortBandwidth == 32)
            {
                NumAOChannels = 4;
                RowOffset = 2;
            }

            
            }

        internal void Setup(AnalogMultiChannelWriter stimAnalogWriter, DigitalSingleChannelWriter stimDigitalWriter, Task stimDigitalTask, Task stimAnalogTask, Task buffLoadTask)//, ulong starttime)
        {
            AnalogMultiChannelWriter[] analogWriters = new AnalogMultiChannelWriter[1];
            analogWriters[0] = stimAnalogWriter;

            Task[] analogTasks = new Task[1];
            analogTasks[0] = stimAnalogTask;

            DigitalSingleChannelWriter[] digitalWriters = new DigitalSingleChannelWriter[1];
            digitalWriters[0] = stimDigitalWriter;

            Task[] digitalTasks = new Task[1];
            digitalTasks[0] = stimDigitalTask;

            base.Setup(analogWriters, digitalWriters, analogTasks, digitalTasks, buffLoadTask);
        }




        override protected void writeEvent(StimulusOutEvent Sout, ref  List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            //initialize EventValues
            int stimDuration = Sout.waveform.Length + NUM_SAMPLES_BLANKING * 2;
            anEventValues = new List<double[,]>();
            anEventValues.Add(new double[NumAOChannels, stimDuration]);
            digEventValues = new List<uint[]>();
            digEventValues.Add(new uint[ stimDuration]);
            for (uint i = 0; i < stimDuration; i++)
            {
                digEventValues.ElementAt(0)[(int)i] = CalculateDigPointAppending(Sout, i);
                double[] AnalogPoint = CalculateAnalogPointAppending(Sout, i, NumAOChannels);
                anEventValues.ElementAt(0)[0 + RowOffset, (int)i] = AnalogPoint[0];
                anEventValues.ElementAt(0)[1 + RowOffset, (int)i] = AnalogPoint[1];
            }

        }
        //NUM_SAMPLES_BLANKING * 2




        //write as much of the current stimulus as possible
        //agnostic as to whether or not you've finished this stimulus or not.
        //returns if finished the stimulus or not



        internal void Append(ulong[] TimeVector, int[] ChannelVector, double[,] WaveMatrix)
       {
             //okay, passed the tests, start appending
            
            List<StimulusOutEvent> stimToPass = new List<StimulusOutEvent>(0);
            
            
                for (int i = 0; i < WaveMatrix.GetLength(0); i++)
                {
                    double[] wave = new double[WaveMatrix.GetLength(1)];
                    //this.TimeVector[outerIndexWrite] = TimeVector[i];
                    for (int j = 0; j < WaveMatrix.GetLength(1); j++)
                    {
                        wave[j] = WaveMatrix[i, j];
                    }
                    // MessageBox.Show("finished a wave");
                    // double[] w = {1.0,1.0};
                    // stim = new StimulusData(1,1.0,w);
                    StimulusOutEvent tmp = new StimulusOutEvent(ChannelVector[i], TimeVector[i], wave);
                    tmp.sampleDuration = tmp.sampleDuration + (uint)NUM_SAMPLES_BLANKING * 2;
                    stimToPass.Add(tmp);

                    
                }
                this.writeToBuffer(stimToPass);
            
            //  MessageBox.Show("finished Append");
       }
        

        

        //appending versions
        internal uint CalculateDigPointAppending(StimulusOutEvent currentStim, uint NumSampLoadedForCurr)
        {
            uint wavelength = (uint)currentStim.waveform.Length;
            if (NumSampLoadedForCurr < NUM_SAMPLES_BLANKING || NumSampLoadedForCurr > NUM_SAMPLES_BLANKING + wavelength)
            {
                return currentStim.DigitalEncode[0];
            }
            else if (NumSampLoadedForCurr == NUM_SAMPLES_BLANKING || NumSampLoadedForCurr == NUM_SAMPLES_BLANKING + wavelength)
            {
                return currentStim.DigitalEncode[1];
            }
            else
            {
                return currentStim.DigitalEncode[2];
            }

        }

        internal double[] CalculateAnalogPointAppending(StimulusOutEvent currentStim, uint NumSampLoadedForCurr, int NumAOChannels)
        {
            uint WaveLength = (uint)currentStim.waveform.Length;
            double[] AnalogPoint = new double[2];
            //Get the analog encoding for this stimulus
            // Case when we are in blanking before a stimulus
            if (NumSampLoadedForCurr < (NUM_SAMPLES_BLANKING + 1))
            {
                AnalogPoint[0] = 0;
                AnalogPoint[1] = 0;
            }
            // Case when we are in blanking after stimulus
            if (NumSampLoadedForCurr >= NUM_SAMPLES_BLANKING + 1 + WaveLength)
            {
                AnalogPoint[0] = 0;
                AnalogPoint[1] = 0;
            }
            // OK, we actually need to load the buffer with some meat
            if (NumSampLoadedForCurr >= NUM_SAMPLES_BLANKING + 1 && NumSampLoadedForCurr < NUM_SAMPLES_BLANKING + 1 + WaveLength)
            {
                //how much has been loaded so far?
                uint NumSamplesLoadedForWave = NumSampLoadedForCurr - 1 - (uint)NUM_SAMPLES_BLANKING;

                //what is the current analog value to be sent to the buffer?
                double voltageOut;
                if (NumSamplesLoadedForWave < WaveLength)
                    voltageOut = currentStim.waveform[NumSamplesLoadedForWave];
                else
                    voltageOut = 0;
                
                // NeuroWronger's crazy analog simulus encoding scheme
                if (NumSamplesLoadedForWave < 20)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = currentStim.AnalogEncode[0];
                }
                else if (NumSamplesLoadedForWave < 40)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = currentStim.AnalogEncode[1];
                }
                else if (NumSamplesLoadedForWave < 60)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = 0;
                }
                else if (NumSamplesLoadedForWave < 80)
                {
                    AnalogPoint[0] = voltageOut;
                    AnalogPoint[1] = ((double)(WaveLength) / 100.0 > 10.0 ? -1 : (double)(WaveLength) / 100.0);
                }
                else
                {
                    AnalogPoint[0] = currentStim.waveform[NumSamplesLoadedForWave]; 
                    AnalogPoint[1] = 0;
                }

            }
            return AnalogPoint;

        }

       

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

    }

}
