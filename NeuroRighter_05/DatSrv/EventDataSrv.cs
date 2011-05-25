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
using System.Threading;

namespace NeuroRighter.DatSrv
{
    class EventDataSrv<T> where T : NREvent
    {
        // The mutex class for concurrent read and write access to data buffers
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        // Main storage buffer
        private EventBuffer<T> dataBuffer;
        
        private ulong currentSample;
        private int bufferSizeInSamples; // The maximum number of samples between  the
                                         // current sample and the last avaialbe mixed
                                         // event time before it expires and is removed.
        private int numSamplesPerWrite;  // The number of samples for each buffer that
                                         // mixed events could have been detected in

        internal EventDataSrv(double sampleFrequencyHz, double bufferSizeSec, int numSamplesPerWrite)
        {
            this.currentSample = 0;
           
            this.dataBuffer = new EventBuffer<T>(sampleFrequencyHz);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.bufferSizeInSamples = (int)Math.Ceiling(bufferSizeSec * sampleFrequencyHz);
        }

        internal void WriteToBuffer(EventBuffer<T> newData) 
        { 
            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // First we must remove the expired samples (we cannot assume these are
                // in temporal order since for 64 channels, we have to write 2x, once for
                // each 32 channel recording task)
                for (int i = 0; i < dataBuffer.eventBuffer.Count; ++i)
                {
                    // Remove expired data
                    if (dataBuffer.eventBuffer[i].sampleIndex < currentSample - (ulong)numSamplesPerWrite)
                    {
                        dataBuffer.eventBuffer.RemoveAt(i);
                    }
                }

                // Add new data
                dataBuffer.eventBuffer.AddRange(newData.eventBuffer);
                currentSample += (ulong)numSamplesPerWrite;
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }

        }

        internal void WriteToBufferRelative(EventBuffer<T> newData)
        {
            // This write operation is used when the sampleIndicies in the newData buffer
            // correspond to the start of a DAQ buffer poll rather than the start of the record

            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // First we must remove the expired samples (we cannot assume these are
                // in temporal order since for 64 channels, we have to write 2x, once for
                // each 32 channel recording task)
                for (int i = 0; i < dataBuffer.eventBuffer.Count; ++i)
                {
                    // Remove expired data
                    if (dataBuffer.eventBuffer[i].sampleIndex < currentSample - (ulong)numSamplesPerWrite)
                    {
                        dataBuffer.eventBuffer.RemoveAt(i);
                    }
                }
                
                // Move time stamps to absolute scheme
                for (int i = 0; i < newData.eventBuffer.Count; ++i)
                {
                    // Convert time stamps to absolute scheme
                    T tmp = (T)newData.eventBuffer[i].copy();
                    tmp.sampleIndex = tmp.sampleIndex + currentSample;
                    dataBuffer.eventBuffer.Add(tmp);
                }

                // Add new data
               // dataBuffer.eventBuffer.AddRange(newData.eventBuffer);

                currentSample += (ulong)numSamplesPerWrite;
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }

        }

        internal ulong[] EstimateAvaiableTimeRange()
        {
            ulong[] timeRange = new ulong[2];
            timeRange[0] = ulong.MaxValue;
            timeRange[1] = ulong.MinValue;
            for (int i = 0; i < dataBuffer.eventBuffer.Count; ++i)
            {
                if (timeRange[0] > dataBuffer.eventBuffer[i].sampleIndex)
                {
                    timeRange[0] = dataBuffer.eventBuffer[i].sampleIndex;
                }

                if (timeRange[1] < dataBuffer.eventBuffer[i].sampleIndex)
                {
                    timeRange[1] = dataBuffer.eventBuffer[i].sampleIndex;
                }
            }
            return timeRange;
        }

        internal EventBuffer<T> ReadFromBuffer(double[] desiredSampleRange) 
        {
            EventBuffer<T> returnBuffer = new EventBuffer<T>(dataBuffer.sampleFrequencyHz);

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                // Collect all the data within the desired sample range and add to the returnBuffer
                // object
                int i = 0;
                while (i < dataBuffer.eventBuffer.Count)
                {
                    if (dataBuffer.eventBuffer[i].sampleIndex > desiredSampleRange[0] &&
                        dataBuffer.eventBuffer[i].sampleIndex <= desiredSampleRange[1])
                    {
                        returnBuffer.eventBuffer.Add(dataBuffer.eventBuffer[i]);
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
