using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using System.Threading;

namespace NeuroRighter.DatSrv
{
    public class RawDataSrv
    {
        // The mutex class for concurrent read and write access to data buffers
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        // Main storage buffer
        private RawMultiChannelBuffer dataBuffer;
        private int numSamplesPerWrite; // The number of samples to be writen for each channel during a write operations
        private int numTasks;

        /// <summary>
        /// Generic raw data server for multichannel data streams. The main data buffer that this class updates i
        /// 'dataBuffer', itself a RawMultiChannelBuffer object. The class has one method that the user should worry about
        /// called, ReadFromDataBuffer. This method accepts a time range (in seconds referenced to the start of the recording)
        /// as input and will copy the portion of the current data buffer that is within that range to the user as a 
        /// RawMultiChannelBuffer object. If there is no data in the time range provided, the method returns a null object.
        /// </summary>
        /// <param name="numChannels"> Number of channels in the raw data stream</param>
        /// <param name="bufferSizeSec"> The history of the channels that should be kept, in seconds</param>
        /// <param name="sampleFrequencyHz"> The sampling frequency of an individual channel in the stream</param>
        /// <param name="numSamplesPerWrite"> The number of samples to be written each time the DAQ is polled and the dataBuffer is updated.</param>
        internal RawDataSrv(double sampleFrequencyHz, int numChannels, double bufferSizeSec, int numSamplesPerWrite,int numTasks)
        {
            this.dataBuffer = new RawMultiChannelBuffer(sampleFrequencyHz, numChannels, (int)Math.Ceiling(bufferSizeSec * sampleFrequencyHz), numTasks);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.numTasks = numTasks;
        }

        internal void WriteToBuffer(double[][] newData, int task, int offset) 
        { 
            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // Overwrite expired samples.
                for (int i = 0; i < numSamplesPerWrite; ++i)
                {
                    for (int j = 0; j < dataBuffer.numChannels; ++j)
                    {
                        dataBuffer.rawMultiChannelBuffer[offset*task+j][dataBuffer.leastCurrentCircularSample[task]] = newData[j][i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(task);
                }
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        internal void WriteToBuffer(double[,] newData, int task, int offset)
        {
            // Lock out other write operations
            bufferLock.EnterWriteLock();
            try
            {
                // Overwrite expired samples.
                for (int i = 0; i < numSamplesPerWrite; ++i)
                {
                    for (int j = 0; j < dataBuffer.numChannels; ++j)
                    {
                        dataBuffer.rawMultiChannelBuffer[offset * task + j][dataBuffer.leastCurrentCircularSample[task]] = newData[j,i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(task);
                }
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        public ulong[] EstimateAvailableTimeRange()
        { 
            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                return dataBuffer.startAndEndSample;
            }
            finally
            {
                // release the read lock
                bufferLock.ExitReadLock();
            }
        }

        public RawMultiChannelBuffer ReadFromBuffer(double[] desiredSampleRange) 
        {
            // Make sure the desiredSampleRange is correctly formatted
            if (desiredSampleRange[0] > desiredSampleRange[1])
            {
                throw new FormatException("The first element of desiredSampleRange must be smaller or equal to the last element.");
            }

            // Make space for the returnBuffer
            int startIndex; // Where to start taking samples
            int stopIndex; // where to stop taking samples
            RawMultiChannelBuffer returnBuffer = null;
            
            // Convert desired sample range to ulong's
            ulong[] desiredSampleRangeUlong  = new ulong[2];
            desiredSampleRangeUlong[0] = (ulong)Math.Floor(desiredSampleRange[0]);
            desiredSampleRangeUlong[1] = (ulong)Math.Ceiling(desiredSampleRange[1]);

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                // First make sure that there are samples available within the desired range
                if (dataBuffer.startAndEndSample[0] <= desiredSampleRange[0] &&
                   dataBuffer.startAndEndSample[1] >= desiredSampleRange[0])
                {
                    // Figure out what part of the buffer we want in reference to the leastCurrentSample
                    // Lower bound
                    if (desiredSampleRange[0] <= dataBuffer.startAndEndSample[0])
                    {
                        startIndex = dataBuffer.netLeastAndMostCurrentCircular[0];
                    }
                    else
                    {
                        startIndex = dataBuffer.FindSampleIndex((int)(desiredSampleRangeUlong[0] - dataBuffer.startAndEndSample[0]));
                    }

                    // Upper bound
                    if (desiredSampleRange[1] >= dataBuffer.startAndEndSample[1])
                    {
                        stopIndex = dataBuffer.netLeastAndMostCurrentCircular[1];
                    }
                    else
                    {
                        stopIndex = dataBuffer.FindSampleIndex((int)(desiredSampleRangeUlong[1] - dataBuffer.startAndEndSample[0]));
                    }

                    // Instantiate the returnBuffer
                    returnBuffer = new RawMultiChannelBuffer(dataBuffer.sampleFrequencyHz, dataBuffer.numChannels, stopIndex - startIndex, numTasks);

                    for (int j = startIndex; j <stopIndex; ++j)
                    {
                        for (int i = 0; i < dataBuffer.numChannels; ++i)
                        {
                            returnBuffer.rawMultiChannelBuffer[i][j - startIndex] = dataBuffer.rawMultiChannelBuffer[i][j];
                        }
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
