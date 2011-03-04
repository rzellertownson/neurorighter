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
    class RMSThresholdFixed : SpikeDetector
    {

        private int numUpdatesForTrain; 
        private int[] numUpdates; //We want to converge on a good estimate of RMS based on the first few calls, then stop updating
        private double[,] RMSList;
        private double[] ChanThresh;
        private double[] ThreshSorted;

        public RMSThresholdFixed(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn,
            int numPostIn, int numPreIn, rawType threshMult, int detectionDeadTime, double deviceRefresh) :
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult, detectionDeadTime) 
        {
            numUpdatesForTrain = (int)Math.Round(10/deviceRefresh); // ten seconds worth of data used in training
            threshold = new rawType[1, numChannels];
            numUpdates = new int[numChannels];
            RMSList = new double[numChannels,numUpdatesForTrain];
            ChanThresh = new double[numUpdatesForTrain];
            ThreshSorted = new double[(int)Math.Floor((double)(numUpdatesForTrain - 9 * numUpdatesForTrain / 10))];
        }

        internal void calcThreshForOneBlock(rawType[] data, int channel, int idx)
        {
            rawType tempData = 0;
            for (int j = 0; j < spikeBufferLength / downsample; ++j)
                tempData += data[j * downsample] * data[j * downsample]; //Square data
            tempData /= (spikeBufferLength / downsample);
            rawType thresholdTemp = (rawType)(Math.Sqrt(tempData) * _thresholdMultiplier);
            RMSList[channel, idx] = thresholdTemp;
            threshold[0, channel] = (threshold[0, channel] * (numUpdates[channel])) / (numUpdates[channel] + 1) + (thresholdTemp / (numUpdates[channel] + 1));// Recursive RMS estimate
        }

        protected override void updateThreshold(rawType[] data, int channel)
        {
            if (numUpdates[channel] > numUpdatesForTrain) { /* do nothing */ }
            else if (numUpdates[channel] == numUpdatesForTrain)
            {
                // Estimate the threshold based on the lower 25% percentile of threshold estimates gathered duringthe updating process
                for (int j = 0; j < numUpdatesForTrain; ++j)
                    ChanThresh[j] = RMSList[channel, j];

                Array.Sort(ChanThresh);

                for (int j = 0; j < ThreshSorted.Length; ++j)
                    ThreshSorted[j] = RMSList[channel, j];

                threshold[0, channel] = ThreshSorted.Average();
                ++numUpdates[channel]; // prevent further updates
            }
            else
            {
                calcThreshForOneBlock(data, channel, numUpdates[channel]);
                ++numUpdates[channel];
            }

        }
     
    }
}
