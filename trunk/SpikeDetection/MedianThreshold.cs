// NeuroRighter v0.04
// Copyright (c) 2008 John Rolston
//
// This file is part of NeuroRighter v0.04.
//
// NeuroRighter v0.04 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter v0.04 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter v0.04.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NationalInstruments.Analysis.Math;

namespace NeuroRighter
{
    using rawType = System.Double;

    /// <author>John Rolston (rolston2@gmail.com)</author>
    sealed class MedianThreshold : SpikeDetector
    {
        rawType[] stData; //Scaled and translated data
        private const double WINDOW = 0.25; //in seconds, how much data to average over
        private List<List<double>> MedianList;
        private readonly int numReadsPerWindow;

        public MedianThreshold(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn, 
            int numPostIn, int numPreIn, rawType threshMult, double deviceRefresh) : 
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult)
        {
            threshold = new rawType[1, numChannels];
            stData = new rawType[spikeBufferLength / downsample]; //Scaled and translated data
            numReadsPerWindow = (int)Math.Round(WINDOW / deviceRefresh);
            if (numReadsPerWindow < 1) numReadsPerWindow = 1;
            MedianList = new List<List<double>>(numChannelsIn);
            for (int i = 0; i < numChannelsIn; ++i) MedianList.Add(new List<double>(numReadsPerWindow));
        }

        protected override void updateThreshold(rawType[] data, int channel)
        {
            //We're going to assume that the data has zero mean (or that it's been filtered)
            //Divide by 0.6745 (per Quiroga et al. 2004), get median
            for (int j = 0; j < spikeBufferLength / downsample; ++j)
            {
                //stData[j] = data[i][j * downsample] / 0.6745;
                stData[j] = data[j * downsample];
                if (stData[j] < 0)
                    stData[j] *= -1; //Invert sign if negative
            }
            //thr[i] = Statistics.Median(stData) * thrMult;
            Array.Sort(stData);

            if (numReadsPerWindow == MedianList[channel].Count) MedianList[channel].RemoveAt(0);
            MedianList[channel].Add(stData[(int)(stData.Length / 2)] * thresholdMultiplier * 1.4826);
            double avg = 0.0;
            for (int i = 0; i < MedianList[channel].Count; ++i) avg += MedianList[channel][i];

            threshold[0, channel] = avg / MedianList[channel].Count;
        }

    }
}
