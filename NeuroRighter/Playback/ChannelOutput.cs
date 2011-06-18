// CHANNELOUTPUT.CS
// Copyright (c) 2008-2011 John Rolston
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
using NationalInstruments.DAQmx;
using System.Threading;

namespace NeuroRighter
{
    /// <summary>
    /// Class to handle playback of one channel to BNC or other output (useful for audio playback)
    /// </summary>
    public sealed class ChannelOutput : IDisposable
    {
        private const double GAIN = 10;

        private Task analogOutputTask;
        private AnalogSingleChannelWriter analogOutputWriter;

        private double[] buffer;
        private int writeHead = -1;

        internal ChannelOutput(double samplingRate, double outputRefreshTime, double inputRefreshTime, Task spikeTask, String NIDevice, int NIChannel)
        {
            //Compute buffer length, instantiate buffer
            int multiple = (int)(Math.Round(outputRefreshTime / inputRefreshTime)); //Get number of input buffer reads that approximate the desired output rate
            if (multiple < 1) multiple = 1; //Ensure the multiple is at least 1
            int bufferLength = (int)((double)multiple * inputRefreshTime * samplingRate); //Calculate length
            buffer = new double[bufferLength];

            //Create new task
            analogOutputTask = new Task("Playback Analog Output Task");
            analogOutputTask.AOChannels.CreateVoltageChannel(NIDevice + "/ao" + NIChannel, "",
                            -10.0, 10.0, AOVoltageUnits.Volts);
            analogOutputTask.Timing.ReferenceClockSource = spikeTask.Timing.ReferenceClockSource;
            analogOutputTask.Timing.ReferenceClockRate = spikeTask.Timing.ReferenceClockRate;
            analogOutputTask.Timing.ConfigureSampleClock("", samplingRate,
                            SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, bufferLength);
            analogOutputTask.Control(TaskAction.Verify);

            //Create writer
            analogOutputWriter = new AnalogSingleChannelWriter(analogOutputTask.Stream);
            analogOutputWriter.SynchronizeCallbacks = false;
        }

        internal void write(double[] data)
        {
            for (int i = 0; i < data.Length; ++i)
                buffer[++writeHead] = GAIN * data[i];

            if (writeHead >= buffer.Length - 1)
            {
                generateOutput();
                writeHead = -1;
            }
        }

        private void generateOutput()
        {
            analogOutputWriter.BeginWriteMultiSample(true, buffer, null, null);
            //analogOutputWriter.WriteMultiSample(true, buffer);
        }

        /// <summary>
        /// Dispose of tasks within object.
        /// </summary>
        public void Dispose()
        {
            analogOutputTask.Dispose();
        }
    }
}
