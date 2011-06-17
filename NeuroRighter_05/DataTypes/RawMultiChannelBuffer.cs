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
        internal double sampleFrequencyHz;
        internal ulong[] startAndEndSample;
        internal double[][] rawMultiChannelBuffer;
        internal int numChannels;
        internal int bufferLengthInSamples;
        internal int[] leastCurrentCircularSample;//one for each task
        internal int[] mostCurrentCircularSample;
        internal ulong[] totalNumSamplesWritten;
        internal int[] netLeastAndMostCurrentCircular;

        public RawMultiChannelBuffer(double sampleFrequencyHz, int numChannelsPerTask, int bufferLengthInSamples, int noTasks)
        {
            this.netLeastAndMostCurrentCircular = new int[2];
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.numChannels = numChannelsPerTask;
            this.bufferLengthInSamples = bufferLengthInSamples;
            this.startAndEndSample = new ulong[2];
            this.startAndEndSample[0] = 0;
            this.startAndEndSample[1] = 0;
            this.rawMultiChannelBuffer = new double[numChannels*noTasks][];
            this.leastCurrentCircularSample = new int[noTasks];
            this.mostCurrentCircularSample = new int[noTasks]; ;
            this.totalNumSamplesWritten = new ulong[noTasks]; ;

            // Allocate space with zeros
            for (int i = 0; i < numChannels * noTasks; ++i)
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
    }
}
