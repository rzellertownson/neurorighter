// NeuroRighter
// Copyright (c) 2008-2010 John Rolston
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
using System.IO;
using System.Threading;
using System.Timers;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System.ComponentModel;

namespace NeuroRighter.FileWriting
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal class SpikeFileOutput
    {
        private int numChannels;
        private int numSamplesPerWaveform;
        private Stream outStream;

        private const int VERSION = 2;

        internal SpikeFileOutput(string filenameBase, int numChannels, int samplingRate,
            int numSamplesPerWaveform, Task recordingTask, string extension)
        {
            this.numChannels = numChannels;
            this.numSamplesPerWaveform = numSamplesPerWaveform;

            //Create output file
            outStream = createStream(filenameBase + extension, 256 * 1024);

            writeHeader(numChannels, samplingRate, recordingTask);
        }

        protected virtual Stream createStream(String filename, Int32 bufferSize)
        {
            return new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, false);
        }

        protected virtual void writeHeader(int numChannels, int samplingRate, Task recordingTask)
        {
            DateTime dt = DateTime.Now; //Get current time (local to computer)
            double[] scalingCoeffs = recordingTask.AIChannels[0].DeviceScalingCoefficients;

            //Write header info: #chs, sampling rate, gain, date/time
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(-VERSION)), 0, 2); //Int: negative of file version (negative will prevent this from being confused with older versions, where this would be a positive numChannels field
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(numChannels)), 0, 2); //Int: Num channels
            outStream.Write(BitConverter.GetBytes(Convert.ToInt32(samplingRate)), 0, 4); //Int: Sampling rate
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(numSamplesPerWaveform)), 0, 2); //Int: Num samples per waveform
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / recordingTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

            //Now we list the fields that each spike will have
            ASCIIEncoding encoder = new ASCIIEncoding();

            const string DELIMITER = "|";
            const string ChannelField = "channel";
            outStream.Write(encoder.GetBytes(ChannelField), 0, encoder.GetByteCount(ChannelField));
            outStream.Write(encoder.GetBytes(DELIMITER), 0, encoder.GetByteCount(DELIMITER));
            outStream.Write(BitConverter.GetBytes(Convert.ToInt32(16)), 0, 4); //Num bits for this field
            outStream.Write(BitConverter.GetBytes('I'), 0, 1); //type: i for int, f for float

            const string TimeField = "time";
            outStream.Write(encoder.GetBytes(TimeField), 0, encoder.GetByteCount(TimeField));
            outStream.Write(encoder.GetBytes(DELIMITER), 0, encoder.GetByteCount(DELIMITER));
            outStream.Write(BitConverter.GetBytes(Convert.ToInt32(32)), 0, 4); //Num bits for this field
            outStream.Write(BitConverter.GetBytes('I'), 0, 1); //type: i for int, f for float

            const string ThresholdField = "threshold";
            outStream.Write(encoder.GetBytes(ThresholdField), 0, encoder.GetByteCount(ThresholdField));
            outStream.Write(encoder.GetBytes(DELIMITER), 0, encoder.GetByteCount(DELIMITER));
            outStream.Write(BitConverter.GetBytes(Convert.ToInt32(64)), 0, 4); //Num bits for this field
            outStream.Write(BitConverter.GetBytes('F'), 0, 1); //type: i for int, f for float

            outStream.Write(encoder.GetBytes(DELIMITER), 0, encoder.GetByteCount(DELIMITER)); //Terminal delimiter means we're done defining fields

            //Everything else is assumed to be the waveform samples
        }

        internal void WriteSpikeToFile(Int16 channel, Int32 timeIndex, double threshold, double[] waveform)
        {
            outStream.Write(BitConverter.GetBytes(channel), 0, 2);
            outStream.Write(BitConverter.GetBytes(timeIndex), 0, 4);
            outStream.Write(BitConverter.GetBytes(threshold), 0, 8);
            for (int s = 0; s < numSamplesPerWaveform; ++s)
                outStream.Write(BitConverter.GetBytes(waveform[s]), 0, 8); //Write value as double -- much easier than writing raw value, but takes more space
        }

        internal void flush()
        {
            outStream.Close();
        }
    }
}
