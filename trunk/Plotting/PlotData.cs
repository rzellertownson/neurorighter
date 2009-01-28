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

    //PLOTDATA Stores a portion of data, posting to plots when appropriate
    internal class PlotData
    {
        internal readonly Int32 downsample;
        protected Int32 bufferLength;
        protected float[][] data;
        protected float[][] outputData;
        protected float gain;
        protected Int32[] writeHead; //Index of next sample to write
        protected Int32 readHead;  //Index of next sample to read
        protected Int32 numChannels;
        protected Int32[] numWrites; //Num samples written since last read
        protected Int32 samplingRate;
        protected Double refreshTime; //In Seconds
        protected float boxHeight; //Height (amplitude) of each graph box
        protected Int32 numRows;  //For display data
        protected Int32 numCols;
        protected String channelMapping;

        internal delegate void dataAcquiredHandler(object sender);
        internal event dataAcquiredHandler dataAcquired;

        internal PlotData(Int32 numChannels, Int32 downsample, Int32 bufferLength, Int32 samplingRate, float boxHeight,
            Int32 numRows, Int32 numCols, Double refreshTime, String channelMapping)
        {
            this.numChannels = numChannels;
            this.downsample = downsample;
            this.bufferLength = bufferLength;
            this.samplingRate = samplingRate;
            this.boxHeight = boxHeight;
            this.numRows = numRows;
            this.numCols = numCols;
            this.refreshTime = refreshTime;
            this.channelMapping = channelMapping;

            gain = 1F;

            int numSamplesPerPlot = (int)(refreshTime * (samplingRate / downsample));
            data = new float[numChannels][];
            outputData = new float[numRows][];
            for (int i = 0; i < numChannels; ++i) data[i] = new float[bufferLength];
            for (int i = 0; i < numRows; ++i) outputData[i] = new float[numCols * numSamplesPerPlot];
            writeHead = new Int32[numChannels];
            numWrites = new Int32[numChannels];
        }

        //************************************************************
        //WRITE
        //input is the data to write into PlotData's buffer
        //startChannel is which channel in buffer to start writing at
        //************************************************************
        internal void write(RawType[][] input, Int32 startChannel, Int32 numChannelsToWrite)
        {
            for (int c = 0; c < numChannelsToWrite; ++c) //For each channel of input
            {
                for (int s = 0; s < input[c].Length; s += downsample)
                {
                    data[startChannel + c][writeHead[startChannel + c]++] = (float)input[c + startChannel][s];
                    //Check to see if we've wrapped around
                    if (writeHead[startChannel + c] >= bufferLength) writeHead[startChannel + c] = 0;
                }
                numWrites[startChannel + c] += input[c].Length / downsample; //integer division is floor by default
            }

            lock (this)
            {
                //See if we've written enough to call on plotters
                Int32 minWrites = Int32.MaxValue;
                //Find minimum number of writes that have been made across all channels
                for (int i = 0; i < numChannels; ++i) minWrites = (numWrites[i] < minWrites ? numWrites[i] : minWrites);
                if (minWrites >= refreshTime * (samplingRate / downsample))
                {
                    //Check for missed buffer reads
                    Int32 maxWrites = 0;
                    for (int i = 0; i < numChannels; ++i) maxWrites = (numWrites[i] > maxWrites ? numWrites[i] : maxWrites);
                    if (maxWrites > bufferLength) //We've written too much, and aren't reading, so we'll have to clear some buffer
                    {
                        int numSamplesPerPlot = (int)(refreshTime * (samplingRate / downsample));
                        readHead += numSamplesPerPlot * (int)Math.Ceiling((double)((maxWrites - bufferLength) / numSamplesPerPlot));
                        readHead %= bufferLength;
                        //System.Windows.Forms.MessageBox.Show("Plot Data Buffer Overrun");
                    }
                    //Callback
                    if (dataAcquired != null) dataAcquired(this);
                    //for (int i = 0; i < numChannels; ++i) numWrites[i] = 0;
                }
            }
        }

        //******************
        //READ
        //******************
        internal virtual float[][] read()
        {
            float temp;
            int numSamplesPerPlot = (int)(refreshTime * (samplingRate / downsample));
            if (numChannels == 16 || numChannels == 64)
            {
                for (int i = 0; i < numRows; ++i) //row
                {
                    for (int j = 0; j < numCols; ++j) //col
                    {
                        int channel;
                        if (numChannels == 64 && channelMapping == "invitro")
                            channel = MEAChannelMappings.ch2rc[i * numRows + j, 0] * numRows + MEAChannelMappings.ch2rc[i * numRows + j, 1];
                        else channel = i * numRows + j;

                        for (int k = 0; k < numSamplesPerPlot; ++k) //sample
                        {
                            //Adjust for display gain and overshoots
                            temp = data[channel][(k + readHead) % bufferLength] * gain; //NB: Should check for wrapping once in advance, rather than modding every time
                            if (temp > boxHeight * 0.5F)
                                temp = boxHeight * 0.5F;
                            else if (temp < -boxHeight *0.5F)
                                temp = -boxHeight * 0.5F;
                            //Translate data down and into output buffer
                            outputData[i][numSamplesPerPlot * j + k] = temp - i * boxHeight;
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

        internal void skipRead()
        {
            int numSamplesPerPlot = (int)(refreshTime * (samplingRate / downsample));
            readHead += numSamplesPerPlot;
            readHead = readHead % bufferLength;
            for (int i = 0; i < numChannels; ++i) numWrites[i] -= numSamplesPerPlot;
        }


        internal void setGain(float gain) { this.gain = gain; }
        internal float getGain() { return gain; }
    }
}
