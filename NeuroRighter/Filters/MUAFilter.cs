// Copyright (c) 2008-2012 Potter Lab
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    class MUAFilter
    {
        private int DownsampleFactor;
        private int BufferLength;
        private int NumChannels;
        private List<NationalInstruments.Analysis.Dsp.Filters.ButterworthLowpassFilter> LowFilters;
        private double samplingRate;

        private const double WINDOW = 5.0; //in seconds, how much data to average over for RMS
        private const double VOLTAGE_EPSILON = 1E-6;
        private const double THRESHOLD_MULTIPLIER = 2.0 * 2.0; //Squared
        private List<List<double>> RMSList;
        private readonly int numReadsPerWindow;


        internal MUAFilter(int NumChannels, double InputSamplingRate, int BufferLength, double highCut,  int filterOrder,
            int DownsampleFactor, double DeviceRefresh)
        {
            this.NumChannels = NumChannels;
            this.BufferLength = BufferLength;
            this.DownsampleFactor = DownsampleFactor;
            this.samplingRate = InputSamplingRate;

            numReadsPerWindow = (int)Math.Round(WINDOW / DeviceRefresh);
            if (numReadsPerWindow < 1) numReadsPerWindow = 1;
            RMSList = new List<List<double>>(NumChannels);
            for (int c = 0; c < NumChannels; ++c) RMSList.Add(new List<double>(numReadsPerWindow));
            
            //Create filters

            //LowFilters = new ButterworthFilter[NumChannels];
            //for (int c = 0; c < NumChannels; ++c)
            //    LowFilters[c] = new ButterworthFilter(2, InputSamplingRate, Low_LowCut, Low_HighCut, BufferLength);
            LowFilters = new List<NationalInstruments.Analysis.Dsp.Filters.ButterworthLowpassFilter>();
            for (int c = 0; c < NumChannels; ++c)
                LowFilters.Add(new NationalInstruments.Analysis.Dsp.Filters.ButterworthLowpassFilter(filterOrder, InputSamplingRate, highCut));

            
        }

        internal void Filter(double[][] data, int startChannel, int numChannels, ref double[][] OutputData, bool applyFilter)
        {
            for (int c = startChannel; c < startChannel + numChannels; ++c)
            {
                //Square (rectify) data, and find RMS
                double threshold = 0.0;
                int samplesAdded = 0;
                for (int j = 0; j < BufferLength; ++j)
                {
                    if (data[c][j] >= VOLTAGE_EPSILON || data[c][j] <= -VOLTAGE_EPSILON) //Check for data suspiciously close to 0 (as if it were blanked by SALPA)
                    {
                        data[c][j] *= data[c][j];
                        threshold += data[c][j];
                        ++samplesAdded;
                    }
                    else
                        data[c][j] = 0.0;
                }
                if (samplesAdded > 0)
                {
                    threshold /= samplesAdded;
                    if (RMSList[c].Count == numReadsPerWindow)
                        RMSList[c].RemoveAt(0);
                    RMSList[c].Add(threshold * THRESHOLD_MULTIPLIER); //Note, we're not taking square root
                    double avg = 0.0;
                    for (int i = 0; i < RMSList[c].Count; ++i) avg += RMSList[c][i];
                    threshold = avg / RMSList[c].Count;
                }

                //Clip outliers
                for (int s = 0; s < BufferLength; ++s)
                {
                    if (data[c][s] <= threshold && data[c][s] >= -threshold) { /* do nothing */ }
                    else if (data[c][s] < -threshold) data[c][s] = -threshold;
                    else data[c][s] = threshold;
                }

                //Low-pass filter
                if(applyFilter)
                    LowFilters[c].FilterData(data[c]);

                //Downsample and take square root
                for (int s = 0, sout = 0; s < BufferLength; s += DownsampleFactor)
                    OutputData[c][sout++] = Math.Sqrt((data[c][s] > 0 ? data[c][s] : 0.0));
            }
        }

        internal void Reset( double highCutHz, int filterOrder)
        {
            for (int c = 0; c < NumChannels; ++c)
                LowFilters[c].Reset(filterOrder, samplingRate, highCutHz);
        }
    }
}
