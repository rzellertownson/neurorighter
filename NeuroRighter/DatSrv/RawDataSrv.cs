using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using System.Threading;

namespace NeuroRighter.DatSrv
{
    /// <summary>
    /// Server for raw data.
    /// </summary>
    public class RawDataSrv
    {
        // Locking object for thread-safe access to the internal data buffer
        protected static readonly object lockObj = new object();

        // Main storage buffer
        private RawMultiChannelBuffer dataBuffer;
        private int numSamplesPerWrite; // The number of samples to be writen for each channel during a write operations
        private int numTasks;
        private double sampleFrequencyHz;
        private int channelCount;

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
        /// <param name="numDataCollectionTasks"> The number of external processes that can asynchronously add data to the buffer</param>
        public RawDataSrv(double sampleFrequencyHz, int numChannels, double bufferSizeSec, int numSamplesPerWrite, int numDataCollectionTasks)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.dataBuffer = new RawMultiChannelBuffer(sampleFrequencyHz, numChannels, (int)Math.Ceiling(bufferSizeSec * sampleFrequencyHz), numDataCollectionTasks);
            this.numSamplesPerWrite = numSamplesPerWrite;
            this.numTasks = numDataCollectionTasks;
            this.channelCount = numChannels;
        }

        /// <summary>
        /// Write data to the Raw Data Server.
        /// </summary>
        /// <param name="newData"> Data block to be added to the server's data buffer.</param>
        /// <param name="task"> The NI task that created these new data</param>
        /// <param name="offset"> Sample offset incurred due to acausal filtering</param>
        internal void WriteToBuffer(double[][] newData, int task, int offset)
        {
            lock (lockObj)
            {
                // Overwrite expired samples.
                for (int i = 0; i < numSamplesPerWrite; ++i)
                {
                    for (int j = 0; j < dataBuffer.NumChannels; ++j)
                    {
                        dataBuffer.RawMultiChannelBuffer[offset * task + j][dataBuffer.leastCurrentCircularSample[task]] = newData[j][i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(task);
                }
            }
        }

        /// <summary>
        /// Write data to the Raw Data Server.
        /// </summary>
        /// <param name="newData"> Data block to be added to the server's data buffer.</param>
        /// <param name="task"> The NI task that created these new data</param>
        /// <param name="offset"> Sample offset incurred due to acausal filtering</param>
        internal void WriteToBuffer(double[,] newData, int task, int offset)
        {
            lock (lockObj)
            {
                // Overwrite expired samples.
                for (int i = 0; i < numSamplesPerWrite; ++i)
                {
                    for (int j = 0; j < dataBuffer.NumChannels; ++j)
                    {
                        dataBuffer.RawMultiChannelBuffer[offset * task + j][dataBuffer.leastCurrentCircularSample[task]] = newData[j, i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(task);
                }
            }
        }

        /// <summary>
        /// Write data to the Raw Data Server.
        /// </summary>
        /// <param name="newData"> Data block to be added to the server's data buffer.</param>
        internal void WriteToBuffer(double[][] newData)
        {
            lock (lockObj)
            {
                // Overwrite expired samples.
                for (int i = 0; i < numSamplesPerWrite; ++i)
                {
                    for (int j = 0; j < dataBuffer.NumChannels; ++j)
                    {
                        dataBuffer.RawMultiChannelBuffer[j][dataBuffer.leastCurrentCircularSample[0]] = newData[j][i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(0);
                }
            }
        }

        /// <summary>
        /// Write data to the Raw Data Server.
        /// </summary>
        /// <param name="newData"> Data block to be added to the server's data buffer.</param>
        internal void WriteToBuffer(double[,] newData)
        {
            lock (lockObj)
            {
                // Overwrite expired samples.
                for (int i = 0; i < numSamplesPerWrite; ++i)
                {
                    for (int j = 0; j < dataBuffer.NumChannels; ++j)
                    {
                        dataBuffer.RawMultiChannelBuffer[j][dataBuffer.leastCurrentCircularSample[0]] = newData[j, i];
                    }

                    // Increment start, end and write markers
                    dataBuffer.IncrementCurrentPosition(0);
                }
            }
        }

        /// <summary>
        /// Estimate the avialable samples in the buffer. This can be used to inform
        /// the user of good arguments for the ReadFromBuffer method.
        /// </summary>
        /// <returns>timeRange</returns>
        public ulong[] EstimateAvailableTimeRange()
        {
            lock (lockObj)
            {
                return dataBuffer.StartAndEndSample;
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
        public RawMultiChannelBuffer ReadFromBuffer(ulong desiredStartIndex, ulong desiredStopIndex)
        {


            // Enforce a read lock
            //bufferLock.EnterReadLock();
            //try
            //{
            lock (lockObj)
            {
                // Make sure the desiredSampleRange is correctly formatted
                if (desiredStartIndex > desiredStopIndex)
                {
                    throw new FormatException("The first element of desiredSampleRange must be smaller or equal to the last element.");
                }

                // Make space for the returnBuffer
                int startIndex; // Where to start taking samples
                int stopIndex; // where to stop taking samples
                RawMultiChannelBuffer returnBuffer = null;

                // First make sure that there are samples available within the desired range
                if (dataBuffer.StartAndEndSample[0] <= desiredStartIndex ||
                   dataBuffer.StartAndEndSample[1] >= desiredStopIndex)
                {
                    // Figure out what part of the buffer we want in reference to the leastCurrentSample
                    // Lower bound
                    ulong absStart;
                    ulong absStop;
                    if (desiredStartIndex <= dataBuffer.StartAndEndSample[0])
                    {
                        startIndex = dataBuffer.netLeastAndMostCurrentCircular[0];
                        absStart = dataBuffer.StartAndEndSample[0];
                    }
                    else
                    {
                        startIndex = dataBuffer.FindSampleIndex(desiredStartIndex);
                        absStart = desiredStartIndex;
                    }

                    // Upper bound
                    if (desiredStopIndex >= dataBuffer.StartAndEndSample[1])
                    {
                        stopIndex = dataBuffer.netLeastAndMostCurrentCircular[1];
                        absStop = dataBuffer.StartAndEndSample[1];
                    }
                    else
                    {
                        stopIndex = dataBuffer.FindSampleIndex(desiredStopIndex);
                        absStop = desiredStopIndex;
                    }

                    // Instantiate the returnBuffer
                    returnBuffer = new RawMultiChannelBuffer(dataBuffer.SampleFrequencyHz, dataBuffer.NumChannels, (int)(absStop - absStart + 1), numTasks);
                    returnBuffer.StartAndEndSample = new ulong[] { absStart, absStop };

                    for (ulong j = absStart; j < absStop + 1; ++j)
                    {
                        int circSample = dataBuffer.FindSampleIndex(j);
                        for (int i = 0; i < dataBuffer.NumChannels; ++i)
                        {
                            returnBuffer.RawMultiChannelBuffer[i][j - absStart] = dataBuffer.RawMultiChannelBuffer[i][circSample];
                        }
                    }
                }
                // Return the data
                return returnBuffer;
            }
            //}
            //finally
            //{
            //    // release the read lock
            //    bufferLock.ExitReadLock();
            //}

        }

        /// <summary>
        /// Sampling frequency for data collected for this server.
        /// </summary>
        public double SampleFrequencyHz
        {
            get
            {
                return sampleFrequencyHz;
            }
        }

        /// <summary>
        ///  Number of channels belonging to this server
        /// </summary>
        public int ChannelCount
        {
            get
            {
                return channelCount;
            }
        }

    }
}
