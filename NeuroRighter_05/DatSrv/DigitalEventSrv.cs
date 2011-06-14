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

        internal DigitalEventSrv(double sampleFrequencyHz, double bufferSizeSec, int numSamplesPerWrite)
        {
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

        internal DigitalEventBuffer ReadFromBuffer(double[] desiredSampleRange) 
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
                    if (dataBuffer.sampleBuffer.ElementAt(i) > desiredSampleRange[0] &&
                        dataBuffer.sampleBuffer.ElementAt(i) <= desiredSampleRange[1])
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