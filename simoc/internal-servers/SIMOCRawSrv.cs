using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using System.Threading;
using NeuroRighter.DatSrv;

namespace simoc.srv
{
    /// <summary>
    /// Server for raw data.
    /// </summary>
    public class SIMOCRawSrv
    {
        // The mutex class for concurrent read and write access to data buffers
        protected ReaderWriterLockSlim bufferLock = new ReaderWriterLockSlim();

        // Main storage buffer
        private RawSimocBuffer dataBuffer;
        private int numSamplesPerWrite; // The number of samples to be writen for each channel during a write operations
        private int numDataCollectionTasks;

        /// <summary>
        /// Sampling frequency for data collected for this server.
        /// </summary>
        public double sampleFrequencyHz;

        /// <summary>
        ///  Number of channels belonging to this server
        /// </summary>
        public int channelCount;

        /// <summary>
        /// I'm using this as a closed loop server for my stuff.
        /// </summary>
        /// <param name="numChannels"> Number of channels in the raw data stream</param>
        /// <param name="bufferSizeSec"> The history of the channels that should be kept, in seconds</param>
        /// <param name="sampleFrequencyHz"> The sampling frequency of an individual channel in the stream</param>
        /// <param name="numSamplesPerWrite"> The number of samples to be written each time the DAQ is polled and the dataBuffer is updated.</param>
        /// <param name="numDataCollectionTasks"> The number of external processes that can asynchronously add data to the buffer</param>
        public SIMOCRawSrv(double sampleFrequencyHz, int numChannels, double bufferSizeSec, int numSamplesPerWrite, int numDataCollectionTasks)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.dataBuffer = new RawSimocBuffer(sampleFrequencyHz, numChannels, (int)Math.Ceiling(bufferSizeSec * sampleFrequencyHz), numDataCollectionTasks);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.numDataCollectionTasks = numDataCollectionTasks;
            this.channelCount = numChannels;
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

        internal void WriteToBuffer(double[][] newData)
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
                        dataBuffer.rawMultiChannelBuffer[j][dataBuffer.leastCurrentCircularSample[0]] = newData[j][i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(0);
                }
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        internal void WriteToBuffer(double[,] newData)
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
                        dataBuffer.rawMultiChannelBuffer[j][dataBuffer.leastCurrentCircularSample[0]] = newData[j, i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(0);
                }
            }
            finally
            {
                // release the write lock
                bufferLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Estimate the avialable samples in the buffer. This can be used to inform
        /// the user of good arguments for the ReadFromBuffer method.
        /// </summary>
        /// <returns>timeRange</returns>
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

        /// <summary>
        /// Read data from buffer. This method will attempt to retrieve samples within the range
        /// specified by the input arguements. The object that is returned
        /// will contain information on the true sample bounds. You can use the EstimateAvailableTimeRange
        /// method to get a (time-sensitive) estimate for good-arguments for this method.
        /// </summary>
        /// <param name="desiredStartIndex">earliest sample, referenced to 0, that should be returned</param>
        /// <param name="desiredStopIndex">latest sample, referenced to 0, that should be returned</param>
        /// <returns>RawMultiChannelBuffer</returns>
        public RawSimocBuffer ReadFromBuffer(ulong desiredStartIndex, ulong desiredStopIndex) 
        {
            // Make sure the desiredSampleRange is correctly formatted
            if (desiredStartIndex > desiredStopIndex)
            {
                throw new FormatException("The first element of desiredSampleRange must be smaller or equal to the last element.");
            }

            // Make space for the returnBuffer
            int startIndex; // Where to start taking samples
            int stopIndex; // where to stop taking samples
            RawSimocBuffer returnBuffer = null;

            // Enforce a read lock
            bufferLock.EnterReadLock();
            try
            {
                // First make sure that there are samples available within the desired range
                if (dataBuffer.startAndEndSample[0] <= desiredStartIndex ||
                   dataBuffer.startAndEndSample[1] >= desiredStopIndex)
                {
                    // Figure out what part of the buffer we want in reference to the leastCurrentSample
                    // Lower bound
                    ulong absStart;
                    ulong absStop;
                    if (desiredStartIndex <= dataBuffer.startAndEndSample[0])
                    {
                        startIndex = dataBuffer.netLeastAndMostCurrentCircular[0];
                        absStart = dataBuffer.startAndEndSample[0];
                    }
                    else
                    {
                        startIndex = dataBuffer.FindSampleIndex(desiredStartIndex);
                        absStart = desiredStartIndex;
                    }

                    // Upper bound
                    if (desiredStopIndex >= dataBuffer.startAndEndSample[1])
                    {
                        stopIndex = dataBuffer.netLeastAndMostCurrentCircular[1];
                        absStop = dataBuffer.startAndEndSample[1];
                    }
                    else
                    {
                        stopIndex = dataBuffer.FindSampleIndex(desiredStopIndex);
                        absStop = desiredStopIndex;
                    }

                    // Instantiate the returnBuffer
                    returnBuffer = new RawSimocBuffer(dataBuffer.sampleFrequencyHz, dataBuffer.numChannels, (int)(absStop - absStart + 1), numDataCollectionTasks);
                    returnBuffer.startAndEndSample = new ulong[] {absStart,absStop};

                    for (ulong j = absStart; j < absStop + 1; ++j)
                    {
                        int circSample = dataBuffer.FindSampleIndex(j);
                        for (int i = 0; i < dataBuffer.numChannels; ++i)
                        {
                            returnBuffer.rawMultiChannelBuffer[i][j - absStart] = dataBuffer.rawMultiChannelBuffer[i][circSample];
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
