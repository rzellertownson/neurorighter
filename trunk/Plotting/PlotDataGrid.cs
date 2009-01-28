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

    //****************************************
    //PlotDataGrid is for grid displayed data
    //****************************************
    internal sealed class PlotDataGrid : PlotData
    {
        internal PlotDataGrid(Int32 numChannels, Int32 downsample, Int32 bufferLength, Int32 samplingRate, float boxHeight,
            Int32 numRows, Int32 numCols, Double plotLength, String channelMapping)
            : base(numChannels, downsample, bufferLength, samplingRate, boxHeight, numRows, numCols, plotLength, channelMapping)
        {
            //Write NaN values for some graphs
            int numSamplesPerPlot = (int)(plotLength * (samplingRate / downsample));
            if (numChannels == 32)
            {
                for (int i = 0; i < 2 * numSamplesPerPlot; ++i)
                {
                    outputData[5][i] = Single.NaN;
                    outputData[5][i + numSamplesPerPlot * 4] = Single.NaN;
                }
            }
        }

        //******************
        //READ
        //******************
        internal override float[][] read()
        {
            float temp;
            int numSamplesPerPlot = (int)(refreshTime * (samplingRate / downsample));
            if (numChannels == 16 || numChannels == 64)
            {
                for (int i = 0; i < numRows; ++i) //row
                {
                    for (int j = 0; j < numCols; ++j) //col
                    {
                        int outRow = i;
                        int outCol = j;
                        if (numChannels == 64 && channelMapping == "invitro")
                        {
                            outRow = MEAChannelMappings.ch2rc[i * numRows + j, 0] - 1;
                            outCol = MEAChannelMappings.ch2rc[i * numRows + j, 1] - 1;
                        }

                        for (int k = 0; k < numSamplesPerPlot; ++k) //sample
                        {
                            //Adjust for display gain and overshoots
                            temp = data[i * numRows + j][(k + readHead) % bufferLength] * gain; //NB: Should check for wrapping once in advance, rather than modding every time
                            if (temp > boxHeight * 0.5F)
                                temp = boxHeight * 0.5F;
                            else if (temp < -boxHeight * 0.5F)
                                temp = -boxHeight * 0.5F;
                            //Translate data down and into output buffer
                            outputData[outRow][numSamplesPerPlot * outCol + k] = temp - outRow * boxHeight;
                        }
                    }
                }
            }
            if (numChannels == 32)
            {
                for (int i = 0; i < numRows - 1; ++i) //row - 1, since last row only has two channels
                {
                    for (int j = 0; j < numCols; ++j) //col
                    {
                        for (int k = 0; k < numSamplesPerPlot; ++k) //sample
                        {
                            //Adjust for display gain and overshoots
                            temp = data[i * numRows + j][(k + readHead) % bufferLength] * gain; //NB: Should check for wrapping once in advance, rather than modding every time
                            if (temp > boxHeight * 0.5F)
                                temp = boxHeight * 0.5F;
                            else if (temp < -boxHeight * 0.5F)
                                temp = -boxHeight * 0.5F;
                            //Translate data down and into output buffer
                            outputData[i][numSamplesPerPlot * j + k] = temp - i * boxHeight;
                        }
                    }
                }
                //Last row
                for (int i = 0; i < 2; ++i)
                {
                    for (int k = 0; k < numSamplesPerPlot; ++k) //sample
                    {
                        //Adjust for display gain and overshoots
                        temp = data[i + 30][(k + readHead) % bufferLength] * gain; //NB: Should check for wrapping once in advance, rather than modding every time
                        if (temp > boxHeight * 0.5F)
                            temp = boxHeight * 0.5F;
                        else if (temp < -boxHeight * 0.5F)
                            temp = -boxHeight * 0.5F;
                        //Translate data down and into output buffer
                        outputData[5][numSamplesPerPlot * (i + 2) + k] = temp - 5F * boxHeight;
                    }
                }
            }
            readHead += numSamplesPerPlot;
            readHead %= bufferLength;
            for (int i = 0; i < numChannels; ++i) numWrites[i] -= numSamplesPerPlot;

            return outputData;
        }
    }
}
