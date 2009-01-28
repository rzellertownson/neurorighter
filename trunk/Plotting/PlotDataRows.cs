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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter
{
    using RawType = System.Double;

    //******************************
    //PlotDataRows for data like LFPs
    //******************************
    internal sealed class PlotDataRows : PlotData
    {
        private Double plotLength; //Total length of plot, in seconds

        internal PlotDataRows(Int32 numChannels, Int32 downsample, Int32 bufferLength, Int32 samplingRate, float boxHeight,
            Double refresh, Double plotLength)
            : base(numChannels, downsample, bufferLength, samplingRate, boxHeight,
                numChannels, 1, refresh, "invivo") //Only 1 column
        {
            this.plotLength = plotLength;

            //Have to update outputData
            int numSamplesPerPlot = (int)(plotLength * (samplingRate / downsample));
            int numSamplesPerRefresh = (int)(refreshTime * (samplingRate / downsample));
            for (int i = 0; i < numRows; ++i) outputData[i] = new float[numCols * numSamplesPerPlot];

            //Readhead needs to be pushed back to account for difference between refreshtime and plotlength
            readHead = bufferLength - (numSamplesPerPlot - numSamplesPerRefresh);
        }

        //******************
        //READ
        //******************
        internal override float[][] read()
        {
            lock (this)
            {
                float temp;
                int numSamplesPerPlot = (int)(plotLength * (samplingRate / downsample));
                int numSamplesPerUpdate = (int)(refreshTime * (samplingRate / downsample));
                for (int i = 0; i < numChannels; ++i) //row
                {
                    for (int k = 0; k < numSamplesPerPlot; ++k) //sample
                    {
                        //Adjust for display gain and overshoots
                        temp = data[i][(k + readHead) % bufferLength] * gain; //NB: Should check for wrapping once in advance, rather than modding every time
                        if (temp > boxHeight * 0.5F)
                            temp = boxHeight * 0.5F;
                        else if (temp < -boxHeight * 0.5F)
                            temp = -boxHeight * 0.5F;
                        //Translate data down and into output buffer
                        outputData[i][k] = temp - i * boxHeight;
                    }
                }
                readHead += numSamplesPerUpdate;
                readHead %= bufferLength;
                for (int i = 0; i < numChannels; ++i) numWrites[i] -= numSamplesPerUpdate;

                return outputData;
            }
        }
    }
}
