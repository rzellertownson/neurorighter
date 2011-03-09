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

namespace NeuroRighter.SpkDet
{
    using rawType = System.Double;

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal abstract class SpikeDetector
    {
        protected int spikeBufferLength; //Length of data dimension
        protected int numChannels;
        protected int downsample; //How much to downsample data when calculating threshold
        protected int spikeBufferSize;
        protected int numPost;
        protected int numPre;
        protected rawType[][] spikeDetectionBuffer;
        protected rawType[] _appendedData;
        protected rawType[,] threshold;
        protected rawType _thresholdMultiplier;
        protected float[][] returnThresh;
        protected  int deadtime; //Num samples overlap between possible spike detections

        public rawType thresholdMultiplier
        {
            get { return _thresholdMultiplier; }
            set { _thresholdMultiplier = value; }
        }

        public rawType[] appendedData
        {
            get { return _appendedData; }
        }

        public SpikeDetector(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, 
            int spike_buffer_sizeIn, int numPostIn, int numPreIn, rawType threshMult, int detectionDeadTime)
        {
            spikeBufferLength = spikeBufferLengthIn;
            numChannels = numChannelsIn;
            downsample = downsampleIn;
            spikeBufferSize = spike_buffer_sizeIn;
            numPost = numPostIn;
            numPre = numPreIn;
            _thresholdMultiplier = threshMult;
            deadtime = detectionDeadTime;

            spikeDetectionBuffer = new rawType[numChannels][];
            for (int i = 0; i < numChannels; ++i)
            {
                spikeDetectionBuffer[i] = new rawType[spikeBufferSize];
            }
            _appendedData = new rawType[spikeBufferLength + spikeBufferSize];
        }

        protected virtual void updateThreshold(rawType[] data, int channel) { }

        protected virtual void updateThreshold(rawType[] data, int channel, int idx) { }

        internal virtual float[][] GetCurrentThresholds()
        {


                returnThresh = new float[2][];

                for (int i = 0; i < 2; ++i)
                {
                    returnThresh[i] = new float[numChannels];
                }

                for (int i = 0; i < numChannels; ++i)
                {
                    returnThresh[0][i] = (float)(threshold[0, i]);
                    returnThresh[1][i] = (float)(-threshold[0, i]);
                }
 

            return returnThresh;
        }

        public virtual void detectSpikes(rawType[] data, List<SpikeWaveform> waveforms, int channel)
        {
            updateThreshold(data, channel);

            //for (int j = 0; j < spikeBufferSize; ++j)
            //    _appendedData[j] = spikeDetectionBuffer[channel][j];
            //for (int j = 0; j < spikeBufferLength; ++j)
            //    _appendedData[j + spikeBufferSize] = data[j];

            int i;
            //Check carried-over samples for spikes
            for (i = spikeBufferSize - numPost; i < spikeBufferSize; ++i)
            {
                if (spikeDetectionBuffer[channel][i] < threshold[0, channel] &&
                    spikeDetectionBuffer[channel][i] > -threshold[0, channel]) { /*do nothing, pt. is within thresh*/ }
                else
                {
                    rawType[] waveform = new rawType[numPost + numPre + 1];
                    for (int j = i - numPre; j < spikeBufferSize; ++j)
                        waveform[j - i + numPre] = spikeDetectionBuffer[channel][j];
                    for (int j = 0; j < numPost - (spikeBufferSize - i) + 1; ++j)
                        waveform[j + numPre + (spikeBufferSize - i)] = data[j];
                    waveforms.Add(new SpikeWaveform(channel, i - spikeBufferSize, threshold[0, channel], waveform));
                    i += numPost - deadtime;
                }
            }
            for (i = 0; i < numPre; ++i)
            {
                if (data[i] < threshold[0, channel] && data[i] > -threshold[0, channel]) { }
                else
                {
                    rawType[] waveform = new rawType[numPost + numPre + 1];
                    for (int j = spikeBufferSize - (numPre - i); j < spikeBufferSize; ++j)
                        waveform[j - spikeBufferSize + (numPre - i)] = spikeDetectionBuffer[channel][j];
                    for (int j = 0; j < (numPost + 1) + i; ++j)
                        waveform[j + (numPre - i)] = data[j];
                    waveforms.Add(new SpikeWaveform(channel, i, threshold[0, channel], waveform));
                    i += numPost - deadtime;
                }
            }
            for ( ; i < spikeBufferLength - numPost; ++i)
            {
                if (data[i] < threshold[0, channel] && data[i] > -threshold[0, channel]) { }
                else
                {
                    rawType[] waveform = new rawType[numPost + numPre + 1];
                    for (int j = i - numPre; j < i + numPost + 1; ++j)
                        waveform[j - i + numPre] = data[j];
                    waveforms.Add(new SpikeWaveform(channel, i, threshold[0, channel], waveform));
                    i += numPost - deadtime;
                }
            }



            //for (int j = spikeBufferSize - numPost; j < spikeBufferLength + spikeBufferSize - numPost; ++j)
            //{
            //    if (_appendedData[j] < threshold[0,channel] && _appendedData[j] > -threshold[0,channel]) { /*If pt. is under threshold, do nothing */ }
            //    else
            //    {
            //        rawType[] waveform = new rawType[numPost + numPre + 1];
            //        for (int k = j - numPre; k < j + numPost; ++k)
            //            waveform[k - j + numPre] = _appendedData[k];
            //        waveforms.Add(new SpikeWaveform(j, waveform));
            //        j += numPost - 10; //Jump past rest of waveform
            //    }
            //}

            for (int j = 0; j < spikeBufferSize; ++j)
                spikeDetectionBuffer[channel][j] = data[j + spikeBufferLength - spikeBufferSize];
        }
    }
}