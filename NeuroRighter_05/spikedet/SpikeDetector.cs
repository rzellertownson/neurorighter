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

namespace NeuroRighter.SpikeDetection
{
    using rawType = System.Double;
    /// <summary>
    /// Base clase for spike detection. All spike detectors should inherit this
    /// virtual class. 
    /// <author> Jon Newman</author>
    /// </summary>
    internal abstract class SpikeDetector
    {
        // Parameters for spike detection
        protected int spikeBufferLength; //Length of data dimension
        protected int numChannels;
        protected int downsample; //How much to downsample data when calculating threshold
        protected int spikeWaveformLength;
        protected int numPost;
        protected int numPre;
        protected rawType[][] detectionCarryOverBuffer;
        protected int carryOverLength;
        protected rawType[] _appendedData;
        protected rawType[,] threshold;
        protected rawType _thresholdMultiplier;
        protected float[][] returnThresh;
        protected int deadtime; //Num samples overlap between possible spike detections
        protected bool[] inASpike; // true when the waveform is over or under the current detection threshold for a given channel
        internal rawType thresholdMultiplier
        {
            get { return _thresholdMultiplier; }
            set { _thresholdMultiplier = value; }
        }

        // Parameters for spike validation
        protected int enterSpikeIndex;
        protected int exitSpikeIndex;
        protected int maxSpikeWidth;
        protected int minSpikeWidth;
        protected double maxSpikeAmp;
        protected double minSpikeSlope;
        protected int spikeIntegrationTime; // one millisecond's worth of samples at the current sampling rate
        protected int spikeDiffTime; // To validate spike slope

        // Data for whrite
        protected bool[] regularDetect;
        protected List<double> spikeDetectionBuffer; // buffer for single channe's worth of spike data
        protected bool posSpike; // Spike polarity
        protected int recIndexOffset;

        public SpikeDetector(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, 
            int spikeWaveformLength, int numPostIn, int numPreIn, rawType threshMult, int detectionDeadTime,
            int minSpikeWidth, int maxSpikeWidth, double maxSpikeAmp, double minSpikeSlope, int spikeIntegrationTime)
        {
            this.spikeBufferLength = spikeBufferLengthIn;
            this.numChannels = numChannelsIn;
            this.downsample = downsampleIn;
            this.spikeWaveformLength = spikeWaveformLength;
            this.numPost = numPostIn;
            this.numPre = numPreIn;
            this._thresholdMultiplier = threshMult;
            this.deadtime = detectionDeadTime;
            this.inASpike = new bool[numChannels];
            this.regularDetect = new bool[numChannels];
            this.maxSpikeWidth = maxSpikeWidth;
            this.minSpikeWidth = minSpikeWidth;
            this.maxSpikeAmp = maxSpikeAmp;
            this.minSpikeSlope = minSpikeSlope;
            this.carryOverLength = spikeWaveformLength + maxSpikeWidth;
            this.spikeIntegrationTime = spikeIntegrationTime;
            this.spikeDiffTime = (int)Math.Round((double)spikeIntegrationTime / 2.0);

            detectionCarryOverBuffer = new rawType[numChannels][];
            for (int i = 0; i < numChannels; ++i)
            {
                this.detectionCarryOverBuffer[i] = new rawType[carryOverLength];
            }
            _appendedData = new rawType[spikeBufferLength + spikeWaveformLength];
        }

        // The threshold update methods are the only things that change for
        // different spike detectors
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

        // Spike detection method that all data goes through at some point
        internal virtual List<SpikeWaveform> DetectSpikes(rawType[] data, int channel)
        {
            List<SpikeWaveform> waveforms = new List<SpikeWaveform>();

            lock (this)
            {
                // Update threshold
                updateThreshold(data, channel);

                // Define position in current data buffer
                int i = numPre;

                // Create the current data buffer
                if (!regularDetect[channel])
                {
                    // First fill, cannot get the first samples because
                    // the number of "pre" samples will be too low
                    regularDetect[channel] = true; // no longer the first detection
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

                    // Need to account for the fact that we our new spike detection buffer will have
                    // a starting index that does not start with new data
                    recIndexOffset = carryOverLength;
                }

                //Detect spikes, append to waveforms list
                int indiciesToSearch = spikeDetectionBuffer.Count - carryOverLength + numPre;
                for (; i < indiciesToSearch; ++i)
                {
                    if (!inASpike[channel])
                    {
                        if (spikeDetectionBuffer[i] < threshold[0, channel] &&
                            spikeDetectionBuffer[i] > -threshold[0, channel])
                        {
                            continue; // not above threshold, next point please
                        }
                        else
                        {
                            inASpike[channel] = true;
                            enterSpikeIndex = i;
                        }
                    }
                    else if (spikeDetectionBuffer[i] < threshold[0, channel] &&
                             spikeDetectionBuffer[i] > -threshold[0, channel])
                    {
                        // We were in a spike and now we are exiting
                        inASpike[channel] = false;
                        exitSpikeIndex = i;
                        int spikeWidth = exitSpikeIndex - enterSpikeIndex;
       
                        // Is this a positive or negative spike?
                        posSpike = FindSpikePolarityByIntegral();

                        // Find the index of the spike maximum
                        int spikeMaxIndex = FindSpikeMax(spikeDetectionBuffer,
                            enterSpikeIndex, spikeWidth, posSpike);

                        // Define spike waveform
                        rawType[] waveform = new rawType[numPost + numPre + 1];
                        for (int j = spikeMaxIndex - numPre; j < spikeMaxIndex + numPost + 1; ++j)
                            waveform[j - spikeMaxIndex + numPre] = spikeDetectionBuffer[j];

                        bool goodSpike = CheckSpike(spikeWidth, waveform);
                        if (goodSpike)
                        {
                            // Record the waveform
                            waveforms.Add(new SpikeWaveform(channel, 
                                spikeMaxIndex - recIndexOffset, threshold[0, channel], waveform));

                            // Advance through deadtime measured from the second threshold crossing
                            i = exitSpikeIndex + deadtime;
                        }
                    }
                    else if (inASpike[channel] && i == indiciesToSearch - 1)
                        // Spike is taking to long to come back through the threshold, its no good
                        inASpike[channel] = false;

                }

                // Create detection carry over buffer from last samples of this buffer that 
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

        // Check spike based on spike detection settings
        protected bool CheckSpike(int spikeWidth, rawType[] waveform)
        {
            // Check spike width
            bool spikeWidthGood = maxSpikeWidth >= spikeWidth && minSpikeWidth <= spikeWidth;
            if (!spikeWidthGood)
                return spikeWidthGood;

            // Check spike amplitude
            rawType[] absWave = new rawType[waveform.Length];
            for (int i = 0; i < waveform.Length; ++i)
                absWave[i] = Math.Abs(waveform[i]);
 
            bool spikeMaxGood = absWave.Max() < maxSpikeAmp;
            if (!spikeWidthGood)
                return spikeMaxGood;

            // Check spike slope
            double spikeSlopeEstimate = new double();
            for (int i = numPre+1-spikeDiffTime; i < numPre + spikeDiffTime; ++i)
            {
                spikeSlopeEstimate += Math.Abs(absWave[i + 1] - absWave[i]);
            }
            spikeSlopeEstimate = spikeSlopeEstimate/(double)spikeDiffTime;
            bool spikeSlopeGood = spikeSlopeEstimate > minSpikeSlope;
            if (!spikeSlopeGood)
                return spikeSlopeGood;
                

            //Ensure that part of the spike is not blanked
            double VOLTAGE_EPSILON = 0.0000005; // 1 uV
            double numBlanked = 0;
            for (int i = 0; i < absWave.Length; ++i)
            {
                if (absWave[i] < VOLTAGE_EPSILON)
                {
                    numBlanked++;
                }
                else
                {
                    numBlanked = 0;
                }

                if (numBlanked > 5)
                    return false;
            }

            // Made it through validation, return true
            return true;
        }

        protected int FindSpikeMax(List<double> spikeDetBuff, int enterSpikeInd, int spikeWidth,
            bool posSpike)
        {
            int indexOfSpikeMax = enterSpikeInd;

            if (posSpike)
            {
                for (int i = 1; i < spikeWidth; ++i)
                {
                    if (spikeDetBuff[enterSpikeInd + i] >
                        spikeDetectionBuffer[indexOfSpikeMax])
                    {
                        indexOfSpikeMax = enterSpikeInd + i;
                    }
                }
            }
            else
            {
                for (int i = 1; i < spikeWidth; ++i)
                {
                    if (spikeDetBuff[enterSpikeInd + i] <
                        spikeDetectionBuffer[indexOfSpikeMax])
                    {
                        indexOfSpikeMax = enterSpikeInd + i;
                    }
                }
            }

            return indexOfSpikeMax;
        }

        protected bool FindSpikePolarityByIntegral()
        {
            // Estimate the area of one millisecond following detection
            // about V(t) = 0 volts.
            double spikeIntegral;
            double preSpikeAvg;
            spikeIntegral = spikeDetectionBuffer.GetRange(enterSpikeIndex, 
                spikeIntegrationTime).Sum();
            preSpikeAvg = spikeDetectionBuffer.GetRange(enterSpikeIndex - numPre,
                numPre).Average();

            // Is the integral of the spike positive?
            return (spikeIntegral - numPre*preSpikeAvg) > 0;
        }

        protected bool FindSpikePolarityBySlopeOfCrossing()
        {
            // Is the crossing through the bottom or top threshold?
            return spikeDetectionBuffer[enterSpikeIndex] > 0;
        }

    }
}