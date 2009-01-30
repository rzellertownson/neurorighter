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

namespace NeuroRighter
{
    using rawType = System.Double;

    /// <author>John Rolston (rolston2@gmail.com)</author>
    sealed class RMSThreshold : SpikeDetector
    {
        private rawType tempData;
        private const double WINDOW = 0.25; //in seconds, how much data to average over
        private List<List<double>> RMSList;
        private readonly int numReadsPerWindow;

        public RMSThreshold(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn, 
            int numPostIn, int numPreIn, rawType threshMult, double deviceRefresh) :
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult) 
        {
            threshold = new rawType[1,numChannels];
            numReadsPerWindow = (int)Math.Round(WINDOW / deviceRefresh);
            if (numReadsPerWindow < 1) numReadsPerWindow = 1;
            RMSList = new List<List<double>>(numChannelsIn);
            for (int i = 0; i < numChannelsIn; ++i) RMSList.Add(new List<double>(numReadsPerWindow));
        }

        protected override void updateThreshold(rawType[] data, int channel)
        {
            tempData = 0;
            for (int j = 0; j < spikeBufferLength / downsample; ++j)
                tempData += data[j * downsample] * data[j * downsample]; //Square data
            tempData /= (spikeBufferLength / downsample);
            if (RMSList[channel].Count == numReadsPerWindow)
                RMSList[channel].RemoveAt(0);
            RMSList[channel].Add(Math.Sqrt(tempData) * _thresholdMultiplier);
            //threshold[0, channel] = Math.Sqrt(tempData) * _thresholdMultiplier;
            double avg = 0.0;
            for (int i = 0; i < RMSList[channel].Count; ++i) avg += RMSList[channel][i];
            threshold[0, channel] = avg / RMSList[channel].Count;
        }
    }
}
