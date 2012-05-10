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
using NeuroRighter.DataTypes;
using ExtensionMethods;
using System.Threading;

namespace NeuroRighter.DatSrv
{
    /// <summary>
    /// Data server for digital data. That is, data tyoes that consist of a discrete time and port state.
    /// </summary>
    public class DigitalEventSrv
    {
        /// <summary>
        /// Locking object to allow thread-safe read/write access to the data buffer.
        /// </summary>
        protected static readonly object lockObj = new object();

        // Main storage buffer
        private DigitalEventBuffer dataBuffer;
        private ulong currentSample;
        private int bufferSizeInSamples; // The maximum number of samples between the current sample and the last avaialbe mixed event time before it expires and is removed.
        private int numSamplesPerWrite;  // The number of samples for each buffer that mixed events could have been detected in
        private double sampleFrequencyHz;

        /// <summary>
        /// Generic digital data server for a given 32 bit port. The main data buffer that this class updates
        /// 'dataBuffer', itself a DigitalEventBuffer object.This method accepts a time range (in seconds referenced to the start of the recording)
        /// as input and will copy the portion of the current data buffer that is within that range to the user as a 
        /// DigitalEventBuffer object. The EstimateAvailableTimeRange method can be used to get an estimate of a valide range
        /// to enter for a Read operation. If there is no data in the time range provided, the method returns a null object.
        /// </summary>
        /// <param name="sampleFrequencyHz"> Sampling frequency of the DAQ that is feeding this server</param>
        /// <param name="bufferSizeSec">The requested history of the buffer in seconds</param>
        /// <param name="numSamplesPerWrite"> How many samples will the DAQ provide when a Write is called?</param>
        public DigitalEventSrv(double sampleFrequencyHz, double bufferSizeSec, int numSamplesPerWrite)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.dataBuffer = new DigitalEventBuffer(sampleFrequencyHz);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.bufferSizeInSamples = (int)Math.Ceiling(bufferSizeSec * sampleFrequencyHz);
        }

        /// <summary>
        /// Write data to the Digital Event Server
        /// </summary>
        /// <param name="newData"> A digital event buffer containing the digital events to be added to the buffer.</param>
        internal void WriteToBuffer(DigitalEventBuffer newData)
        {
            lock (lockObj)
            {
                // First we must remove the expired samples (we cannot assume these are
                // in temporal order since for 64 channels, we have to write 2x, once for
                // each 32 channel recording task)
                int i = 0;
                while (i < dataBuffer.SampleBuffer.Count)
                {
                    // Remove expired data
                    if (dataBuffer.SampleBuffer.ElementAt(i) + (ulong)bufferSizeInSamples < currentSample)
                    {
                        dataBuffer.SampleBuffer.RemoveAt(i);
                        dataBuffer.PortStateBuffer.RemoveAt(i);
                    }
                }

                // Add new data
                dataBuffer.SampleBuffer.AddRange(newData.SampleBuffer);
                dataBuffer.PortStateBuffer.AddRange(newData.PortStateBuffer);

                // Update the most current sample read
                currentSample += (ulong)numSamplesPerWrite;
            }
        }

        /// <summary>
        /// Estimate the avialable samples in the buffer. This can be used to inform
        /// the user of good arguments for the ReadFromBuffer method.
        /// </summary>
        /// <returns>timeRange</returns>
        internal ulong[] EstimateAvailableTimeRange()
        {
            lock (lockObj)
            {
                ulong[] timeRange = new ulong[2];

                timeRange[0] = dataBuffer.SampleBuffer.Min();
                timeRange[1] = dataBuffer.SampleBuffer.Max();

                return timeRange;
            }
        }

        /// <summary>
        /// Read data from buffer. This method will attempt to retrieve samples within the range
        /// specified by the input arguements. The object that is returned
        /// will contain information on the true sample bounds. You can use the EstimateAvailableTimeRange
        /// method to get a (time-sensitive) estimate for good-arguments for this method.
        /// </summary>
        /// <param name="desiredStartIndex">earliest sample, referenced to 0, that should be returned</param>
        /// <param name="desiredStopIndex">latest sample, referenced to 0, that should be returned</param>
        /// <returns>DigitalEventBuffer</returns>
        internal DigitalEventBuffer ReadFromBuffer(ulong desiredStartIndex, ulong desiredStopIndex)
        {
            lock (lockObj)
            {
                DigitalEventBuffer returnBuffer = new DigitalEventBuffer(dataBuffer.SampleFrequencyHz);

                // Collect all the data within the desired sample range and add to the returnBuffer
                // object
                int i = 0;
                while (i < dataBuffer.SampleBuffer.Count)
                {
                    if (dataBuffer.SampleBuffer.ElementAt(i) > desiredStartIndex &&
                        dataBuffer.SampleBuffer.ElementAt(i) <= desiredStopIndex)
                    {
                        returnBuffer.SampleBuffer.Add(dataBuffer.SampleBuffer.ElementAt(i));
                        returnBuffer.PortStateBuffer.Add(dataBuffer.PortStateBuffer.ElementAt(i));
                    }
                }

                // Return the data
                return returnBuffer;
            }
        }

        # region Public Accessors

        /// <summary>
        /// Sampling frequency for data collected for this server.
        /// </summary>
        public double SampleFrequencyHz
        {
            get
            {
                return sampleFrequencyHz;
            }
            set
            {
                sampleFrequencyHz = value;
            }
        }

        # endregion
    }

}