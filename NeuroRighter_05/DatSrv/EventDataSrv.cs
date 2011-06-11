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
using ExtensionMethods;

namespace NeuroRighter.DatSrv
{
    public class EventDataSrv<T> where T : NREvent
    {
        // The mutex class for concurrent read and write access to data buffers
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        // Main storage buffer
        private EventBuffer<T> dataBuffer;
        
        private ulong[] currentSample;
        private ulong bufferSizeInSamples; // The maximum number of samples between  the
                                         // current sample and the last avaialbe mixed
                                         // event time before it expires and is removed.
        private int numSamplesPerWrite;  // The number of samples for each buffer that
                                         // mixed events could have been detected in
        internal int noTasks;
        private ulong mincurrentSample;

        internal EventDataSrv(double sampleFrequencyHz, double bufferSizeSec, int numSamplesPerWrite, int noTasks)
        {
            this.currentSample = new ulong[noTasks];
            this.mincurrentSample = 0;
           
            this.dataBuffer = new EventBuffer<T>(sampleFrequencyHz);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.bufferSizeInSamples = (ulong)Math.Ceiling(bufferSizeSec * sampleFrequencyHz);
            this.noTasks = noTasks;
        }

        internal void WriteToBuffer(EventBuffer<T> newData, int taskNo) 
        { 
            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // First we must remove the expired samples (we cannot assume these are
                // in temporal order since for 64 channels, we have to write 2x, once for
                // each 32 channel recording task)
                int rem = 0;
                for (int i = 0; i < dataBuffer.eventBuffer.Count; ++i)
                {
                    // Remove expired data
                    if (mincurrentSample > bufferSizeInSamples)
                        if (dataBuffer.eventBuffer[i].sampleIndex < (mincurrentSample - bufferSizeInSamples))
                    {
                        dataBuffer.eventBuffer.RemoveAt(i);
                        rem++;
                    }
                }

                // Add new data
                int added = 0;
                foreach (T stim in newData.eventBuffer)
                {
                    dataBuffer.eventBuffer.Add((T)stim.DeepClone());
                    added++;
                }
                //Console.WriteLine(this.ToString() + " added " + added+ " removed " +rem+ " at sample " + currentSample);
                //.AddRange(newData.eventBuffer);
                currentSample[taskNo] += (ulong)numSamplesPerWrite;
                mincurrentSample = currentSample[0];
                for (int i = 1; i < this.noTasks; i++)
                {
                    if (mincurrentSample>currentSample[i])
                        mincurrentSample = currentSample[i];
                }
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }

        }

        internal void WriteToBufferRelative(EventBuffer<T> newData, int taskNo)
        {
            // This write operation is used when the sampleIndicies in the newData buffer
            // correspond to the start of a DAQ buffer poll rather than the start of the record
            int added = 0; int rem = 0;
            string times = "";
            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // First we must remove the expired samples (we cannot assume these are
                // in temporal order since for 64 channels, we have to write 2x, once for
                // each 32 channel recording task)
                
                for (int i = 0; i < dataBuffer.eventBuffer.Count; ++i)
                {
                    if (mincurrentSample > bufferSizeInSamples)
                        if (dataBuffer.eventBuffer[i].sampleIndex < mincurrentSample - (ulong)bufferSizeInSamples)
                    {
                        dataBuffer.eventBuffer.RemoveAt(i);
                        rem++;
                    }
                }
                
                // Move time stamps to absolute scheme
                
                for (int i = 0; i < newData.eventBuffer.Count; ++i)
                {
                    // Convert time stamps to absolute scheme
                    T tmp = (T)newData.eventBuffer[i].DeepClone();
                    tmp.sampleIndex = tmp.sampleIndex + currentSample[taskNo];
                    dataBuffer.eventBuffer.Add(tmp);
                    times += tmp.sampleIndex.ToString() + ", ";
                    added++;
                }
                

                // Add new data
               // dataBuffer.eventBuffer.AddRange(newData.eventBuffer);

                currentSample[taskNo] += (ulong)numSamplesPerWrite;
                mincurrentSample = currentSample[0];
                for (int i = 1; i < this.noTasks; i++)
                {
                    if (mincurrentSample > currentSample[i])
                        mincurrentSample = currentSample[i];
                }
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
            //Console.WriteLine("WriteToBufferRelative: added "+added+ "/removed " +rem+  "at "+currentSample[taskNo]+"/task " +taskNo + " mincur " + mincurrentSample + " ex: " +times);

        }

        public ulong[] EstimateAvaiableTimeRange()
        {
            ulong[] timeRange = new ulong[2];
            timeRange[0] = ulong.MaxValue;
            timeRange[1] = ulong.MinValue;

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {

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
            }
            finally
            {
                // release the read lock
                bufferLock.ExitReadLock();
            }

            return timeRange;
        }

        public EventBuffer<T> ReadFromBuffer(ulong[] desiredSampleRange) 
        {
            EventBuffer<T> returnBuffer = new EventBuffer<T>(dataBuffer.sampleFrequencyHz);

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                // Collect all the data within the desired sample range and add to the returnBuffer
                // object
                int added = 0;
                for (int i = 0; i < dataBuffer.eventBuffer.Count;i++ )
                {
                    if (dataBuffer.eventBuffer[i].sampleIndex > desiredSampleRange[0] &&
                        dataBuffer.eventBuffer[i].sampleIndex <= desiredSampleRange[1])
                    {
                        returnBuffer.eventBuffer.Add((T)dataBuffer.eventBuffer[i].DeepClone());
                        added++;
                    }
                }
                Console.WriteLine("ReadFromBuffer: " + added + " /" + this.dataBuffer.eventBuffer.Count.ToString() + "read, range " + desiredSampleRange[0]+ "-" +desiredSampleRange[1]);
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
