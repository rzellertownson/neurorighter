// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NeuroRighter
{
    using rawType = System.Double;


    /// <author>John Rolston (rolston2@gmail.com) and Jon Newman</author>
    class AdaptiveRMSThreshold : SpikeDetector
    {

        private int numUpdatesForTrain; // This is the number of updates needed to form a buffer for the exponential filter (5 sec of data)
        private double updateBlockLengthSec = 0.1;
        private double filterHalfLifeSec = 5; 
        private double filterHalfLife;
        private double alpha;
        private int[] numUpdates;
        private double[,] RMSList;
        private double[] ChanThresh;
        private double[] ThreshSorted;
        bool[] chanStarted;
        double[,] warmupThreshold;
        bool[] warmedUp;
        int[] countWarmup;

        public AdaptiveRMSThreshold(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn,
            int numPostIn, int numPreIn, rawType threshMult, int detectionDeadTime, double deviceRefresh) :
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult, detectionDeadTime)
        {
            numUpdatesForTrain = (int)Math.Round(updateBlockLengthSec / deviceRefresh); // 1 second worth of data used for estimating each RMS point to be feed into the exp filter
            filterHalfLife = filterHalfLifeSec / updateBlockLengthSec; // seconds
            threshold = new rawType[1, numChannels];
            numUpdates = new int[numChannels];
            RMSList = new double[numChannels, numUpdatesForTrain];
            ChanThresh = new double[numUpdatesForTrain];
            ThreshSorted = new double[(int)Math.Floor((double)(numUpdatesForTrain - 9 * numUpdatesForTrain / 10))];
            alpha = 2 / (2.8854 * filterHalfLife + 1);
            chanStarted = new bool[numChannels];
            warmupThreshold = threshold;
            warmedUp = new bool[numChannels];
            countWarmup = new int[numChannels];
        }

        internal void calcThreshForOneBlock(rawType[] data, int channel, int idx, bool withinTrainBlock)
        {
            rawType tempData = 0;
            for (int j = 0; j < spikeBufferLength / downsample; ++j)
                tempData += data[j * downsample] * data[j * downsample]; //Square data
            tempData /= (spikeBufferLength / downsample);
            rawType thresholdTemp = (rawType)(Math.Sqrt(tempData) * _thresholdMultiplier);
            RMSList[channel, idx] = thresholdTemp;

            if (withinTrainBlock)
                threshold[0, channel] = (threshold[0, channel] * (numUpdates[channel])) / (numUpdates[channel] + 1) + (thresholdTemp / (numUpdates[channel] + 1));// Recursive RMS estimate
        }

        protected override void updateThreshold(rawType[] data, int channel)
        {
            lock (this)
            {
                if (numUpdates[channel] == numUpdatesForTrain) // Time to feed the exp. filter
                {
                    // Estimate the threshold based on the lower 10% percentile of threshold estimates gathered duringthe updating process
                    for (int j = 0; j < numUpdatesForTrain; ++j)
                        ChanThresh[j] = RMSList[channel, j];

                    Array.Sort(ChanThresh);

                    for (int j = 0; j < ThreshSorted.Length; ++j)
                        ThreshSorted[j] = ChanThresh[j];

                    if (!chanStarted[channel])
                    {
                        threshold[0, channel] = ThreshSorted.Average();
                        chanStarted[channel] = true;
                    }
                    else if (!warmedUp[channel])
                    {
                        countWarmup[channel]++;
                        warmupThreshold[0, channel] = ExpFilter(ThreshSorted.Average(), threshold[0, channel], alpha);
                        if (Convert.ToDouble(countWarmup[channel]) >= filterHalfLife) 
                        {
                            warmedUp[channel] = true;
                            threshold[0, channel] = warmupThreshold[0, channel];
                        }
                    }
                    else
                    {
                        threshold[0, channel] = ExpFilter(ThreshSorted.Average(), threshold[0, channel], alpha);
                    }

                    numUpdates[channel] = 0; // prevent further updates
                }
                else
                {
                    calcThreshForOneBlock(data, channel, numUpdates[channel], !warmedUp[channel]);
                    ++numUpdates[channel];
                }
            }
        }

        internal double ExpFilter(double threshIn, double oldThresh, double A)
        {
            double newThresh = A * threshIn + (1 - A) * oldThresh;
            return newThresh;
        }

    }
}
