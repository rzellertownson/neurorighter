// NeuroRighter
// Copyright (c) 2008 John Rolston
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
    sealed class LimAda : SpikeDetector
    {
        private int chunkSize;
        private int numChunks;
        private int VLoIdx;
        private int VHiIdx;
        private rawType VLo;
        private rawType VHi;
        private rawType[] tempData;
        private const rawType alpha = 0.01 / (1.0 + 0.01);
        private rawType tempThreshold;
        private rawType finalThreshold;
        private rawType[] limAdaPrevious;


        public LimAda(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn, int numPostIn,
            int numPreIn, rawType threshMult, int detectionDeadTime, int spikeSamplingRateIn) : 
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult, detectionDeadTime)
        {
            chunkSize = (int)(0.01 * (rawType)spikeSamplingRateIn); //Big enough for 10ms of data
            numChunks = spikeBufferLength / chunkSize;
            VLoIdx = (int)(0.02 * chunkSize);
            VHiIdx = (int)(0.3 * chunkSize);
            tempData = new rawType[chunkSize]; //To hold 10ms window

            threshold = new rawType[numChannels, spikeBufferLength];
            limAdaPrevious = new rawType[numChannels];
            for (int i = 0; i < numChannels; ++i)
                limAdaPrevious[i] = 0.0001;
        }

        protected override void updateThreshold(rawType[] data, int channel)
        {
            /*  After band pass filtering as for BandFlt, LimAda splits the data stream into 10 ms windows,
                and determines the 2nd and 30th percentiles of the distribution of voltages found in each
                such window. Call these voltages V.02 and V.30. (Note that both are usually negative because
                of the filtering, which sets V.50 ~ <V> ~ 0.) It then performs two tests:
                • Is the ratio of V.02 over V.30 less than 5?
                • Is the absolute value of V.30 (significantly) non-zero?
                The first test makes sure that there was no actual spike in the window; the second test
                makes sure that the data in the window was not blanked out (e.g. by Rawsrv or Salpa).
                If both tests are passed, the windows is considered ‘clean’, and V.02 is used to update the
                current noise threshold estimate. Spikes are detected whenever the absolute value of the
                voltage exceeds the current threshold, which is the output of passing the absolute values of
                V.02 from all ‘clean’ windows through a low-pass filter with a time constant of 100 windows
                (1 second if all are clean). This algorithm adapts rapidly to changing noise situations, while
                not desensitizing during bursts. 
             */
            for (int j = 0; j < numChunks; ++j)
            {
                //Copy chunk into tempData, sort
                for (int k = 0; k < chunkSize; ++k)
                {
                    tempData[k] = data[j * chunkSize + k];
                }
                Array.Sort(tempData);
                VLo = tempData[VLoIdx];
                VHi = tempData[VHiIdx];
                if (VLo / VHi < 5.0 && VHi != 0.0)
                {
                    //Low pass filter threshold
                    if (VLo < 0)
                        tempThreshold = -alpha * VLo + (1 - alpha) * limAdaPrevious[channel];
                    else
                        //tempThreshold = alpha * VLo + (1 - alpha) * limAdaPrevious[channel];
                        tempThreshold = limAdaPrevious[channel] + alpha * (VLo - limAdaPrevious[channel]);
                }
                else
                {
                    tempThreshold = limAdaPrevious[channel];
                }
                finalThreshold = tempThreshold * thresholdMultiplier * 0.494F; //The 0.494 is to make it commensurate with RMS noise

                //Copy threshold to thrAda
                for (int k = 0; k < chunkSize; ++k)
                {
                    threshold[channel, j * chunkSize + k] = finalThreshold;
                }
                limAdaPrevious[channel] = tempThreshold;
           }
        }

        public override void detectSpikes(rawType[] data, List<SpikeWaveform> waveforms, int channel)
        {

            int i;
            //Check carried-over samples for spikes
            for (i = spikeBufferSize - numPost; i < spikeBufferSize; ++i)
            {
                if (spikeDetectionBuffer[channel][i] < threshold[channel, spikeBufferLength - spikeBufferSize + i] &&
                    spikeDetectionBuffer[channel][i] > -threshold[channel, spikeBufferLength - spikeBufferSize + i]) { /*do nothing, pt. is within thresh*/ }
                else
                {
                    rawType[] waveform = new rawType[numPost + numPre + 1];
                    for (int j = i - numPre; j < spikeBufferSize; ++j)
                        waveform[j - i + numPre] = spikeDetectionBuffer[channel][j];
                    for (int j = 0; j < numPost - (spikeBufferSize - i) + 1; ++j)
                        waveform[j + numPre + (spikeBufferSize - i)] = data[j];
                    waveforms.Add(new SpikeWaveform(channel, i - spikeBufferSize, threshold[channel, spikeBufferLength - spikeBufferSize + i], waveform));
                    i += numPost - 10;
                }
            }

            updateThreshold(data, channel);
            
            for (i = 0; i < numPre; ++i)
            {
                if (data[i] < threshold[channel, i] && data[i] > -threshold[channel, i]) { }
                else
                {
                    rawType[] waveform = new rawType[numPost + numPre + 1];
                    for (int j = spikeBufferSize - (numPre - i); j < spikeBufferSize; ++j)
                        waveform[j - spikeBufferSize + (numPre - i)] = spikeDetectionBuffer[channel][j];
                    for (int j = 0; j < numPost + 1; ++j)
                        waveform[j + (numPre - i)] = data[j];
                    waveforms.Add(new SpikeWaveform(channel, i, threshold[channel, i], waveform));
                    i += numPost - 10;
                }
            }
            for (; i < spikeBufferLength - numPost; ++i)
            {
                if (data[i] < threshold[channel, i] && data[i] > -threshold[channel, i]) { }
                else
                {
                    rawType[] waveform = new rawType[numPost + numPre + 1];
                    for (int j = i - numPre; j < i + numPost + 1; ++j)
                        waveform[j - i + numPre] = data[j];
                    waveforms.Add(new SpikeWaveform(channel, i, threshold[channel, i], waveform));
                    i += numPost - 10;
                }
            }


            //for (int j = 0; j < spikeBufferSize; ++j)
            //    _appendedData[j] = spikeDetectionBuffer[channel][j];
            //for (int j = 0; j < spikeBufferLength; ++j)
            //    _appendedData[j + spikeBufferSize] = data[j];

            //for (int j = spikeBufferSize - numPost; j < spikeBufferLength + spikeBufferSize - numPost; ++j)
            //{
            //    if (_appendedData[j] < threshold[channel, j - spikeBufferSize + numPost] && _appendedData[j] > -threshold[channel, j - spikeBufferSize + numPost]) { /*If pt. is under threshold, do nothing */ }
            //    else
            //    {
            //        rawType[] waveform = new rawType[numPost + numPre + 1];
            //        for (int k = j - numPre; k < j + numPost; ++k)
            //            waveform[k - j - numPre] = _appendedData[k];
            //        waveforms.Add(new SpikeWaveform(j, waveform));
            //        j += numPost - 10; //Jump past rest of waveform
            //    }
            //}

            for (int j = 0; j < spikeBufferSize; ++j)
                spikeDetectionBuffer[channel][j] = data[j + spikeBufferLength - spikeBufferSize];
        }

        internal override float[][] GetCurrentThresholds()
        {
            returnThresh = new float[2][];

            for (int i = 0; i < 2; ++i)
            {
                returnThresh[i] = new float[numChannels];
            }

            for (int i = 0; i < numChannels; ++i)
            {
                returnThresh[0][i] = (float)(threshold[i, 0]);
                returnThresh[1][i] = (float)(-threshold[i, 0]);
            }


            return returnThresh;
        }


    }
}
