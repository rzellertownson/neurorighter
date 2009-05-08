using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.SpikeDetection
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>John Rolston (rolston2@gmail.com)</author>
    sealed class StimSafeMedian : SpikeDetector
    {
        double[] stData; //Scaled and translated data
        private const double WINDOW = 0.1; //in seconds, how much data to average over (has trouble w/ >100 ms)
        private const double VOLTAGE_EPSILON = 1E-6; //in volts, what we consider 0 (blanked)
        private double[][] buffer;
        private double[] sortedBuffer;
        private int[] writeIndex;
        private readonly int numReadsPerWindow;
        private int[] numReads;

        public StimSafeMedian(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn,
            int numPostIn, int numPreIn, double threshMult, double deviceRefresh, double samplingRate) :
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult)
        {
            threshold = new double[1, numChannels];
            stData = new double[spikeBufferLength / downsample]; //Scaled and translated data
            buffer = new double[numChannels][];
            for (int i = 0; i < numChannels; ++i)
                buffer[i] = new double[(int)(WINDOW * samplingRate / downsample)];
            sortedBuffer = new double[(int)(WINDOW * samplingRate / downsample)];
            writeIndex = new int[numChannels];
            numReadsPerWindow = (int)Math.Round(WINDOW / deviceRefresh);
            if (numReadsPerWindow < 1) numReadsPerWindow = 1;

            numReads = new int[numChannels];
        }

        protected override void updateThreshold(double[] data, int channel)
        {
            //We're going to assume that the data has zero mean (or that it's been filtered)
            //Divide by 0.6745 (per Quiroga et al. 2004), get median

            //First, collate data, take abs val, ignore blanked samples
            for (int j = 0; j < spikeBufferLength; j += downsample)
            {
                buffer[channel][writeIndex[channel]] = data[j];
                //Take absolute value
                if (buffer[channel][writeIndex[channel]] < 0.0)
                    buffer[channel][writeIndex[channel]] *= -1.0;

                //Verify that sample wasn't blanked (abs val less than 10 nV)
                if (buffer[channel][writeIndex[channel]] >= VOLTAGE_EPSILON)
                    ++writeIndex[channel]; //If less than voltage epsilon, overwrite on next pass

                if (writeIndex[channel] >= buffer[channel].Length) writeIndex[channel] = 0;
            }

            //Now, set threshold with 1.4826 multiplier
            if (numReads[channel] >= numReadsPerWindow)
            {
                //Copy into sorting array
                for (int i = 0; i < sortedBuffer.Length; ++i)
                    sortedBuffer[i] = buffer[channel][i];

                //Sort buffer
                Array.Sort(sortedBuffer);

                //Set threshold
                threshold[0, channel] = sortedBuffer[(int)(0.5 * sortedBuffer.Length)] * thresholdMultiplier * 1.4826;
            }
            else //We haven't written the full buffer yet, so we need to watch out for those zeros
            {
                ++numReads[channel];

                double[] tempSortedBuffer = new double[numReads[channel] * spikeBufferLength / downsample];
                //Copy into sorting array
                for (int i = 0; i < tempSortedBuffer.Length; ++i)
                    tempSortedBuffer[i] = buffer[channel][i];

                //Sort buffer
                Array.Sort(tempSortedBuffer);

                threshold[0, channel] = tempSortedBuffer[(int)(0.5 * tempSortedBuffer.Length)] * thresholdMultiplier * 1.4826;
            }
        }
    }
}
