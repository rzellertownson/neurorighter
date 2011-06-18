// NeuroRighter
// Copyright (c) 2008 John Rolston
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

    class DigitalEventSrv
    {

        // The mutex class for concurrent read and write access to data buffers
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        // Main storage buffer
        private DigitalEventBuffer dataBuffer;
        private ulong currentSample;
        private int bufferSizeInSamples; // The maximum number of samples between  the
                                         // current sample and the last avaialbe mixed
                                         // event time before it expires and is removed.
        private int numSamplesPerWrite;  // The number of samples for each buffer that
                                         // mixed events could have been detected in

        // Public variables
        public double sampleFrequencyHz;

        internal DigitalEventSrv(double sampleFrequencyHz, double bufferSizeSec, int numSamplesPerWrite)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.dataBuffer = new DigitalEventBuffer(sampleFrequencyHz);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.bufferSizeInSamples = (int)Math.Ceiling(bufferSizeSec * sampleFrequencyHz);
        }

        protected void WriteToBuffer(DigitalEventBuffer newData) 
        { 
            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // First we must remove the expired samples (we cannot assume these are
                // in temporal order since for 64 channels, we have to write 2x, once for
                // each 32 channel recording task)
                int i = 0;
                while (i < dataBuffer.sampleBuffer.Count)
                {
                    // Remove expired data
                    if (dataBuffer.sampleBuffer.ElementAt(i) < currentSample - (ulong)bufferSizeInSamples)
                    {
                        dataBuffer.sampleBuffer.RemoveAt(i);
                        dataBuffer.portStateBuffer.RemoveAt(i);
                    }

                    // Add new data
                    dataBuffer.sampleBuffer.AddRange(newData.sampleBuffer);
                    dataBuffer.portStateBuffer.AddRange(newData.portStateBuffer);
                }

                currentSample += (ulong)numSamplesPerWrite;
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }

        }

        internal ulong[] EstimateAvailableTimeRange()
        {

            ulong[] timeRange = new ulong[2];

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                timeRange[0] = dataBuffer.sampleBuffer.Min();
                timeRange[1] = dataBuffer.sampleBuffer.Max();
            }
            finally
            {
                // release the read lock
                bufferLock.ExitReadLock();
            }

            return timeRange;
        }

        internal DigitalEventBuffer ReadFromBuffer(ulong desiredStartIndex, ulong desiredStopIndex) 
        {
            DigitalEventBuffer returnBuffer = new DigitalEventBuffer(dataBuffer.sampleFrequencyHz);

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                // Collect all the data within the desired sample range and add to the returnBuffer
                // object
                int i = 0;
                while (i < dataBuffer.sampleBuffer.Count)
                {
                    if (dataBuffer.sampleBuffer.ElementAt(i) > desiredStartIndex &&
                        dataBuffer.sampleBuffer.ElementAt(i) <= desiredStopIndex)
                    {
                        returnBuffer.sampleBuffer.Add(dataBuffer.sampleBuffer.ElementAt(i));
                        returnBuffer.portStateBuffer.Add(dataBuffer.portStateBuffer.ElementAt(i));
                    }
                }
            }
            finally
            {
                // release the read lock
                bufferLock.ExitReadLock();
            }

            // Return the data
            return returnBuffer;

        }
    }

}