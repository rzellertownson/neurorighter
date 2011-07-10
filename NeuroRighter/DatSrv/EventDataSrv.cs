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
    /// <summary>
    /// Data server for event-type data.
    /// </summary>
    public class EventDataSrv<T> where T : NREvent
    {
        /// <summary>
        ///  The mutex class for concurrent read and write access to data buffers
        /// </summary>
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        // Main storage buffer
        private EventBuffer<T> dataBuffer;
        
        private ulong[] currentSample;
        private ulong bufferSizeInSamples; // The maximum number of samples between  the
                                         // current sample and the last avaialbe mixed
                                         // event time before it expires and is removed.
        private int numSamplesPerWrite;  // The number of samples for each buffer that
                                         // mixed events could have been detected in
        private ulong mincurrentSample;

        // Internal variables
        internal int noTasks; // number of daq data colleciton tasks

        /// <summary>
        /// Sampling frequency for data collected for this server.
        /// </summary>
        public double sampleFrequencyHz;


        /// <summary>
        /// Generic event-type data server (e.g. spikes). The main data buffer that this class updates
        /// 'dataBuffer', is itself a EventBuffer object. The  method ReadFromBuffer accepts a time range (in seconds referenced to the start of the recording)
        /// as input and will copy the portion of the current data buffer that is within that range to the user as a 
        /// EventBuffer object. The EstimateAvailableTimeRange method can be used to get an estimate of a valide range
        /// to enter for a Read operation. If there is no data in the time range provided, the method returns a null object.
        /// </summary>
        /// <param name="sampleFrequencyHz"> Sampling frequency of the DAQ that is feeding this server</param>
        /// <param name="bufferSizeSec">The requested history of the buffer in seconds</param>
        /// <param name="numSamplesPerWrite"> How many samples will the DAQ provide when a Write is called?</param>
        /// <param name="numDataCollectionTasks"> The number of external processes that can asynchronously add data to the buffer</param>
        public EventDataSrv(double sampleFrequencyHz, double bufferSizeSec, int numSamplesPerWrite, int numDataCollectionTasks)
        {
            this.currentSample = new ulong[noTasks];
            this.mincurrentSample = 0;
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.dataBuffer = new EventBuffer<T>(sampleFrequencyHz);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.bufferSizeInSamples = (ulong)Math.Ceiling(bufferSizeSec * sampleFrequencyHz);
            this.noTasks = numDataCollectionTasks;
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

        /// <summary>
        /// Estimate the avialable samples in the buffer. This can be used to inform
        /// the user of good arguments for the ReadFromBuffer method.
        /// </summary>
        /// <returns>timeRange</returns>
        public ulong[] EstimateAvailableTimeRange()
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

        /// <summary>
        /// Read data from buffer. This method will attempt to retrieve samples within the range
        /// specified by the input arguements. The object that is returned
        /// will contain information on the true sample bounds. You can use the EstimateAvailableTimeRange
        /// method to get a (time-sensitive) estimate for good-arguments for this method.
        /// </summary>
        /// <param name="desiredStartIndex">earliest sample, referenced to 0, that should be returned</param>
        /// <param name="desiredStopIndex">latest sample, referenced to 0, that should be returned</param>
        /// <returns>EventBuffer<T></returns>
        public EventBuffer<T> ReadFromBuffer(ulong desiredStartIndex, ulong desiredStopIndex) 
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
                    if (dataBuffer.eventBuffer[i].sampleIndex > desiredStartIndex &&
                        dataBuffer.eventBuffer[i].sampleIndex <= desiredStopIndex)
                    {
                        returnBuffer.eventBuffer.Add((T)dataBuffer.eventBuffer[i].DeepClone());
                        added++;
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
