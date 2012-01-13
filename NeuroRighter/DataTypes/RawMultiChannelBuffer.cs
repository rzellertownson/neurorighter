using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// This class is the standard NR buffer class for raw analog data coming from
    /// multiple input channels. It has a startAndEndSample property that specifies
    /// the start and end sample indicies, relative to the start of data collection
    /// contained in the data buffer corresponding to the first and last positions
    /// of the rawMultichannelBuffer array (one channel's worth of data along each row).
    /// The samplePeriodSecond property allows client classes to determine the absolute
    /// time stamp of each sample in seconds.
    /// </summary>
    public class RawMultiChannelBuffer
    {
        // User accessible
        private double sampleFrequencyHz;
        private ulong[] startAndEndSample;
        private double[][] rawMultiChannelBuffer;
        private int numChannels;
        private int bufferLengthInSamples;

        // NeuroRighter accessible
        internal int[] leastCurrentCircularSample;//one for each task
        internal int[] mostCurrentCircularSample;
        internal ulong[] totalNumSamplesWritten;
        internal int[] netLeastAndMostCurrentCircular;

        /// <summary>
        /// Standard NR buffer class for raw analog data coming from
        /// multiple input channels. It has a startAndEndSample property that specifies
        /// the start and end sample indicies, relative to the start of data collection
        /// contained in the data buffer corresponding to the first and last positions
        /// of the rawMultichannelBuffer array (one channel's worth of data along each row).
        /// The samplePeriodSecond property allows client classes to determine the absolute
        /// time stamp of each sample in seconds.
        /// </summary>
        /// <param name="sampleFrequencyHz"> Sampling frequency of data in the buffer</param>
        /// <param name="numChannelsPerTask"> Number of channels data collection task (argument 4) </param>
        /// <param name="bufferLengthInSamples">The history of the channels that should be kept, in samples</param></param>
        /// <param name="numDataCollectionTasks"> The number of external processes that can asynchronously add data to the buffer</param>
        public RawMultiChannelBuffer(double sampleFrequencyHz, int numChannelsPerTask, int bufferLengthInSamples, int numDataCollectionTasks)
        {
            this.netLeastAndMostCurrentCircular = new int[2];
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.numChannels = numChannelsPerTask;
            this.bufferLengthInSamples = bufferLengthInSamples;
            this.startAndEndSample = new ulong[2];
            this.startAndEndSample[0] = 0;
            this.startAndEndSample[1] = 0;
            this.rawMultiChannelBuffer = new double[numChannels * numDataCollectionTasks][];
            this.leastCurrentCircularSample = new int[numDataCollectionTasks];
            this.mostCurrentCircularSample = new int[numDataCollectionTasks]; ;
            this.totalNumSamplesWritten = new ulong[numDataCollectionTasks]; ;

            // Allocate space with zeros
            for (int i = 0; i < numChannels * numDataCollectionTasks; ++i)
            {

                rawMultiChannelBuffer[i] = new double[bufferLengthInSamples];

                for (int j = 0; j < bufferLengthInSamples; ++j)
                {
                    rawMultiChannelBuffer[i][j] = 0;
                }
            }
        }

        internal void IncrementCurrentPosition(int task)
        {
            totalNumSamplesWritten[task]++;
            mostCurrentCircularSample[task] = leastCurrentCircularSample[task] % bufferLengthInSamples;
            leastCurrentCircularSample[task] = (++leastCurrentCircularSample[task]) % bufferLengthInSamples;
            ulong tmpa =totalNumSamplesWritten[0];
            ulong tmpi = totalNumSamplesWritten[0];

            //find the minimum and maximum no of samples written
            for (int i = 1;i<totalNumSamplesWritten.Length;i++)
            {
                if (tmpa>totalNumSamplesWritten[i])
                    tmpa = totalNumSamplesWritten[i];
                if (tmpi < totalNumSamplesWritten[i])
                    tmpi = totalNumSamplesWritten[i];
            }
            netLeastAndMostCurrentCircular[0] = (int)(tmpa % (ulong)bufferLengthInSamples);//least current circular index
            if (tmpi>0)
                netLeastAndMostCurrentCircular[1] = (int)((tmpi - 1) % (ulong)bufferLengthInSamples);//most current circular index

            startAndEndSample[1] = tmpi - 1;
            if (tmpa-1 < (ulong)bufferLengthInSamples)
            {
                startAndEndSample[0] = 0; 
            }
            else
            {
                startAndEndSample[0] = tmpa - 1 - (ulong)bufferLengthInSamples; 
            }

        }

        //used for reading only.  this means that we care about the 'youngest' of the two oldest samples and the 
        internal int FindSampleIndex(ulong absoluteSampleIndex)
        {
            return (int)(absoluteSampleIndex % (ulong)bufferLengthInSamples);
        }

        /// <summary>
        /// The sampling frequency of this data stream in Hz.
        /// </summary>
        public double SampleFrequencyHz
        {
            get
            {
                return sampleFrequencyHz;
            }
        }

        /// <summary>
        /// The oldest and newest sample that are current members of this data buffer.
        /// </summary>
        public ulong[] StartAndEndSample
        {
            get
            {
                return startAndEndSample;
            }
            set
            {
                value = startAndEndSample;
            }
        }

        /// <summary>
        /// The sample buffer itself.
        /// </summary>
        public double[][] RawMultiChannelBuffer
        {
            get
            {
                return rawMultiChannelBuffer;
            }
        }

        /// <summary>
        /// The number channels stored in this buffer.
        /// </summary>
        public int NumChannels
        {
            get
            {
                return NumChannels;
            }
        }

        /// <summary>
        /// The history of the this buffer, measured in the amount of past samples it stores for eachc channel.
        /// </summary>
        public int BufferLengthInSamples
        {
            get
            {
                return BufferLengthInSamples;
            }
        }
    }
}
