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
using NeuroRighter.DataTypes;

namespace NeuroRighter.SpikeDetection
{
    using rawType = System.Double;

    /// <author>Jon Newman and John Rolston</author>
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
        private double[,] thresholdCarryOverBuffer;
        private bool[] firstPass;


        public LimAda(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spikeWaveformLength, int numPostIn,
            int numPreIn, rawType threshMult, int detectionDeadTime, int minSpikeWidth, int maxSpikeWidth, double maxSpikeAmp
            , double minSpikeSlope, int spikeIntegrationTime, int spikeSamplingRateIn) : 
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spikeWaveformLength, numPostIn, numPreIn, threshMult, detectionDeadTime,
            minSpikeWidth, maxSpikeWidth, maxSpikeAmp, minSpikeSlope)
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

            thresholdCarryOverBuffer = new double[numChannels,carryOverLength];
            firstPass = new bool[numChannels];
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

        // Spike detection method that all data goes through at some point
        internal override List<SpikeEvent> DetectSpikes(rawType[] data, int channel, ulong bufferOffset)
        {
            if (bufferOffset == 0)
                for (int i = 0; i < numChannels; ++i)
                {
                    this.detectionCarryOverBuffer[i] = new double[carryOverLength];
                }
            List<SpikeEvent> waveforms = new List<SpikeEvent>();

            lock (this)
            {
                // Update threshold
                updateThreshold(data, channel);

                // Define position in current data buffer
                int i = numPre + initialSamplesToSkip[channel];

                // Create the current data buffer
                if (!regularDetect[channel])
                {
                    // First fill, cannot get the first samples because
                    // the number of "pre" samples will be too low
                    regularDetect[channel] = true; // no longer the first detection
                    firstPass[channel] = true;
                    spikeDetectionBuffer = new List<double>();
                    spikeDetectionBuffer.AddRange(data);
                    recIndexOffset = 0;
                }
                else
                {
                    // Create buffer that is used for spike detection
                    spikeDetectionBuffer = new List<double>();

                    // Data from last buffer that we could not detect on because of edge effects
                    spikeDetectionBuffer.AddRange(detectionCarryOverBuffer[channel]);

                    // Data from this buffer
                    spikeDetectionBuffer.AddRange(data);

                    // Need to account for the fact that our new spike detection buffer will have
                    // a starting index that does not start with new data's 0 index
                    recIndexOffset = carryOverLength;

                    // We are done with the first pass
                    firstPass[channel] = false;
                }

                //Detect spikes, append to waveforms list
                int indiciesToSearchForCross = spikeDetectionBuffer.Count - carryOverLength + numPre;
                int indiciesToSearchForReturn = spikeDetectionBuffer.Count - carryOverLength + numPre + maxSpikeWidth;

                for (; i < indiciesToSearchForReturn; ++i)
                {
                    // For limada, the current threshold is a function of i
                    if (i < carryOverLength && !firstPass[channel])
                    {
                        currentThreshold = thresholdCarryOverBuffer[channel, i];
                    }
                    else if (firstPass[channel])
                    {
                        currentThreshold = threshold[channel, i];
                    }
                    else
                    {
                        currentThreshold = threshold[channel, i - carryOverLength];
                    }

                    if (!inASpike[channel] && i < indiciesToSearchForCross)
                    {
                        if (spikeDetectionBuffer[i] < currentThreshold &&
                            spikeDetectionBuffer[i] > -currentThreshold)
                        {
                            continue; // not above threshold, next point please
                        }
                        else
                        {
                            // We are entering a spike
                            inASpike[channel] = true;
                            enterSpikeIndex[channel] = i;

                            // Positive or negative crossing
                            posCross = FindSpikePolarityBySlopeOfCrossing(channel);
                        }
                    }
                    else if (inASpike[channel] &&
                            ((posCross && spikeDetectionBuffer[i] < currentThreshold) ||
                             (!posCross && spikeDetectionBuffer[i] > -currentThreshold))
                            )
                    {

                        // We were in a spike and now we are exiting
                        inASpike[channel] = false;
                        exitSpikeIndex[channel] = i;

                        // Calculate Spike width
                        spikeWidth = exitSpikeIndex[channel] - enterSpikeIndex[channel];

                        // Find the index + value of the spike maximum
                        int spikeMaxIndex = FindMaxDeflection(posCross, enterSpikeIndex[channel], spikeWidth);
                        double spikeMax = spikeDetectionBuffer[spikeMaxIndex];

                        // Define spike waveform
                        rawType[] waveform = CreateWaveform(spikeMaxIndex);

                        // Check if the spike is any good
                        bool goodSpike = CheckSpike(spikeWidth, waveform);


                        if (!goodSpike)
                        {
                            // If the spike is no good
                            continue;
                        }
                        else
                        {
                            // Infection point within dead time?
                            bool inflectionWithinDead = true;
                            deadWidth = null;

                            if (deadTime > 0)
                            {
                                // Check the dead time for higher amplitdude waveform
                                int deadMaxIndex = FindMaxDeflection(exitSpikeIndex[channel], deadTime);

                                // Check that the maximal value in the deadtime is not the 
                                // exitSpikeIndex[channel]
                                if (deadMaxIndex == exitSpikeIndex[channel])
                                {
                                    inflectionWithinDead = false;
                                    deadWidth = null;
                                    goto ProcessSpike;
                                }

                                // Is this actually an infection point?
                                int deadMaxIndex1 = deadMaxIndex + 1;
                                int deadMaxIndex2 = deadMaxIndex - 1;

                                // If its not the infection point
                                if (Math.Abs(spikeDetectionBuffer[deadMaxIndex1]) > Math.Abs(spikeDetectionBuffer[deadMaxIndex]))
                                    // Forget it, we will catch this spike after the dead time
                                    inflectionWithinDead = false;

                                // Get the maximal value in the dead time
                                double deadMax = spikeDetectionBuffer[deadMaxIndex];

                                // Is it larger than the peak of the detected spike?
                                bool lookAtDeadWave = Math.Abs(deadMax) > Math.Abs(spikeMax);

                                // get the spike width around this max point
                                deadWidth = FindWidthFromMaxInd(deadMaxIndex);

                                if (lookAtDeadWave && inflectionWithinDead)
                                {
                                    // If the deadMax is actually larger than the original 
                                    // detection's max point
                                    rawType[] deadWaveform = CreateWaveform(deadMaxIndex);

                                    if (deadWidth != null)
                                    {
                                        bool goodDeadSpike = CheckSpike(deadWidth[2], deadWaveform);

                                        if (goodDeadSpike && inflectionWithinDead)
                                        {
                                            waveform = deadWaveform;
                                            spikeMaxIndex = deadMaxIndex;
                                            spikeMax = deadMax;
                                            exitSpikeIndex[channel] = deadWidth[1];
                                        }
                                    }
                                }
                            }

                        ProcessSpike:
                            // Record the waveform
                            waveforms.Add(new SpikeEvent(channel,
                                (ulong)(spikeMaxIndex)+(ulong)(bufferOffset) - (ulong)(recIndexOffset), currentThreshold, waveform));

                            // Calculate dead-time
                            int dt;
                            if (!inflectionWithinDead && deadWidth != null)
                                dt = deadWidth[0] - 1;
                            else
                                dt = deadTime;

                            // Carry-over dead time if we are at the end of the buffer
                            if (i >= indiciesToSearchForCross)
                                initialSamplesToSkip[channel] = dt + exitSpikeIndex[channel] - indiciesToSearchForCross;
                            else
                                initialSamplesToSkip[channel] = 0;

                            // Advance through deadTime measured from the spike exit index
                            if (!inflectionWithinDead && deadWidth != null)
                                i = deadWidth[0] - 1;

                            else
                                i = exitSpikeIndex[channel] + deadTime;

                        }
                    }
                    else if (inASpike[channel] && i == indiciesToSearchForReturn - 1)
                    {
                        // Spike is taking to long to come back through the threshold, its no good
                        inASpike[channel] = false;
                        break;
                    }
                    else if (!inASpike[channel] && i >= indiciesToSearchForCross)
                    {
                        break;
                    }

                }

                // Create carry-over buffer from last samples of this buffer
                int idx = 0;
                for (i = spikeDetectionBuffer.Count - carryOverLength; i < spikeDetectionBuffer.Count; ++i)
                {
                    detectionCarryOverBuffer[channel][idx] = spikeDetectionBuffer[i];
                    idx++;
                }

                // pass the waveforms to further processes
                return waveforms;
            }
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
