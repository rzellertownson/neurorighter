using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// This class is the standard NR buffer class for raw analog data coming from
    /// mulitple input channels. It has a startAndEndSample property that specifies
    /// the start and end sample indicies, relative to the start of data collection
    /// contained in the data buffer corresponding to the first and last positions
    /// of the rawMultichannelBuffer array (one channel's worth of data along each row).
    /// The samplePeriodSecond property allows client classes to determine the absolute
    /// time stamp of each sample in seconds.
    /// </summary>
    class RawMultiChannelBuffer
    {
        internal double sampleFrequencyHz;
        internal ulong[] startAndEndSample;
        internal double[][] rawMultiChannelBuffer;
        internal int numChannels;
        internal int bufferLengthInSamples;
        internal int leastCurrentCircularSample;
        internal int mostCurrentCircularSample;
        internal ulong totalNumSamplesWritten;

        public RawMultiChannelBuffer(double sampleFrequencyHz, int numChannels, int bufferLengthInSamples)
        {
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.numChannels = numChannels;
            this.bufferLengthInSamples = bufferLengthInSamples;
            this.startAndEndSample = new ulong[2];
            this.startAndEndSample[0] = 0;
            this.startAndEndSample[1] = 0;
            this.rawMultiChannelBuffer = new double[numChannels][];
            this.leastCurrentCircularSample = 0;
            this.mostCurrentCircularSample = 0;
            this.totalNumSamplesWritten = 0;

            // Allocate space with zeros
            for (int i = 0; i < numChannels; ++i)
            {

                rawMultiChannelBuffer[i] = new double[bufferLengthInSamples];

                for (int j = 0; j < bufferLengthInSamples; ++j)
                {
                    rawMultiChannelBuffer[i][j] = 0;
                }
            }
        }

        internal void IncrementCurrentPosition()
        {
            totalNumSamplesWritten++;
            mostCurrentCircularSample = leastCurrentCircularSample % bufferLengthInSamples;
            leastCurrentCircularSample = (++leastCurrentCircularSample) % bufferLengthInSamples;
            startAndEndSample[1] = totalNumSamplesWritten-1;
            if (startAndEndSample[1] < (ulong)bufferLengthInSamples)
            {
                startAndEndSample[0] = 0; 
            }
            else
            {
                startAndEndSample[0] = startAndEndSample[1] - (ulong)bufferLengthInSamples; 
            }
        }

        internal int FindSampleIndex(int numberOfSamplesFromLeastCurrentOne)
        {
            return (leastCurrentCircularSample + numberOfSamplesFromLeastCurrentOne) % bufferLengthInSamples;
        }
    }
}
