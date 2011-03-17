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
using System.Threading;
using ExtensionMethods;

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
        protected double currentThreshold;
        protected rawType[][] detectionCarryOverBuffer;
        protected int carryOverLength;
        protected rawType[,] threshold;
        protected rawType _thresholdMultiplier;
        protected float[][] returnThresh;
        protected int deadTime; //Num samples overlap between possible spike detections
        protected int[] initialSamplesToSkip;
        protected bool[] inASpike; // true when the waveform is over or under the current detection threshold for a given channel
        internal rawType thresholdMultiplier
        {
            get { return _thresholdMultiplier; }
            set { _thresholdMultiplier = value; }
        }

        // Parameters for spike validation
        protected int enterSpikeIndex;
        protected int exitSpikeIndex;
        protected int spikeWidth;
        protected double primarySpikeIntegral;
        protected int[] secondarySpikeIdx;
        protected double secondarySpikeIntegral;
        protected int maxSpikeWidth;
        protected int minSpikeWidth;
        protected double maxSpikeAmp;
        protected double minSpikeSlope;
        //protected int spikeIntegrationTime; // one millisecond's worth of samples at the current sampling rate
        //protected int spikeDiffTime; // To validate spike slope

        // Data for write
        protected bool[] regularDetect;
        protected List<double> spikeDetectionBuffer; // buffer for single channe's worth of spike data
        protected bool posCross; // polarity of inital threshold crossing
        protected bool secondaryCross; // Is there another crossing within the dead-time after the first?
        protected int recIndexOffset;

        public SpikeDetector(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, 
            int spikeWaveformLength, int numPostIn, int numPreIn, rawType threshMult, int detectionDeadTime,
            int minSpikeWidth, int maxSpikeWidth, double maxSpikeAmp, double minSpikeSlope)
        {
            this.spikeBufferLength = spikeBufferLengthIn;
            this.numChannels = numChannelsIn;
            this.downsample = downsampleIn;
            this.spikeWaveformLength = spikeWaveformLength;
            this.numPost = numPostIn;
            this.numPre = numPreIn;
            this._thresholdMultiplier = threshMult;
            this.deadTime = detectionDeadTime;
            this.inASpike = new bool[numChannels];
            this.regularDetect = new bool[numChannels];
            this.maxSpikeWidth = maxSpikeWidth;
            this.minSpikeWidth = minSpikeWidth;
            this.maxSpikeAmp = maxSpikeAmp;
            this.minSpikeSlope = minSpikeSlope;
            this.carryOverLength = spikeWaveformLength + 2*maxSpikeWidth;
            this.initialSamplesToSkip = new int[numChannels];

            detectionCarryOverBuffer = new rawType[numChannels][];
            for (int i = 0; i < numChannels; ++i)
            {
                this.detectionCarryOverBuffer[i] = new rawType[carryOverLength];
            }

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
                int i = numPre + initialSamplesToSkip[channel];

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
                int indiciesToSearchForCross = spikeDetectionBuffer.Count - carryOverLength + numPre;
                int indiciesToSearchForReturn = spikeDetectionBuffer.Count - carryOverLength + numPre + maxSpikeWidth;
                
                // For fixed and adaptive, the current threshold is not a function of i
                currentThreshold = threshold[0, channel];
                for (; i < indiciesToSearchForReturn; ++i)
                {

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
                            enterSpikeIndex = i;
                        }
                    }
                    else if (inASpike[channel] &&
                             spikeDetectionBuffer[i] < currentThreshold &&
                             spikeDetectionBuffer[i] > -currentThreshold)
                    {
                        //dbg
                        if (i == numPre)
                        {
                            Console.WriteLine("badness");
                        }

                        // We were in a spike and now we are exiting
                        inASpike[channel] = false;
                        exitSpikeIndex = i;
                        
                        // Positive or negative crossing
                        posCross = FindSpikePolarityBySlopeOfCrossing();


                        // Search dead-time for a second crossing on the opposite
                        // threshold of the on that was just crossed. If this has
                        // occured, integrate the area of the voltage over each
                        // crossing. The maximal area wins and determins the location
                        // and polarity of the spike.
                        if (deadTime > 0)
                        {
                            secondaryCross = SearchForSecondCrossing(posCross);

                            // If there is a second crossing, compare the integrals of the 
                            if (secondaryCross)
                            {
                                // find the enter anx exit points for the second porition
                                // of the spike
                                secondarySpikeIdx = GetSecondaryEnterExitPoints();

                                // compare integrals of the first and second portions of th
                                // spike
                                primarySpikeIntegral = GetSpikeIntegral(posCross, enterSpikeIndex, exitSpikeIndex);
                                secondarySpikeIntegral = GetSpikeIntegral(posCross, secondarySpikeIdx[0], secondarySpikeIdx[1]);

                                // If the secondary spike is the real one, then redefined the exit/enter points based
                                // on it
                                if (secondarySpikeIntegral > primarySpikeIntegral)
                                {
                                    posCross = !posCross; // Must be the opposite polarity
                                    enterSpikeIndex = secondarySpikeIdx[0];
                                    exitSpikeIndex = secondarySpikeIdx[1];
                                }
                            }
                        }

                        spikeWidth = exitSpikeIndex - enterSpikeIndex;
       
                        // Find the index of the spike maximum
                        int spikeMaxIndex = FindSpikeMax();

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

                            // Carry-over dead time if we are at the end of the buffer
                            if (i >= indiciesToSearchForCross)
                                initialSamplesToSkip[channel] = (exitSpikeIndex-indiciesToSearchForCross)+deadTime;
                            else
                                initialSamplesToSkip[channel] = 0;

                            // Advance through deadTime measured from the second threshold crossing
                            i = exitSpikeIndex + deadTime;
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
            bool spikeSlopeGood = GetSpikeSlope(absWave) > minSpikeSlope;
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

        protected int FindSpikeMax()
        {
            List<double> spkSnip = spikeDetectionBuffer.GetRange(enterSpikeIndex, spikeWidth);
            if (posCross)
                return spkSnip.MaxIndex() + enterSpikeIndex;
            else
                return spkSnip.MinIndex() + enterSpikeIndex;

        }

        protected double GetSpikeIntegral(bool posCross, int enterIdx, int exitIdx)
        {
            // Estimate the area of a spike (defined as the area from the
            // threshold crossing)
            double tempInt = spikeDetectionBuffer.GetRange(enterIdx, exitIdx - enterIdx).Sum();
            return Math.Abs(tempInt) - (exitIdx - enterIdx) * currentThreshold;
        }

        protected double GetSpikeSlope(double[] absWave)
        {
            double spikeSlopeEstimate = new double();
            int diffWidth;

            if (spikeWidth + 2 <= numPre)
                diffWidth = spikeWidth+2;
            else
                diffWidth = numPre;

            for (int i = numPre + 1 - diffWidth; i < numPre + diffWidth; ++i)
            {
                spikeSlopeEstimate += Math.Abs(absWave[i + 1] - absWave[i]);
            }
            
            return spikeSlopeEstimate / (double)(2 * diffWidth);

        }

        protected bool FindSpikePolarityBySlopeOfCrossing()
        {
            // Is the crossing through the bottom or top threshold?
            return spikeDetectionBuffer[enterSpikeIndex] > 0;
        }

        protected bool SearchForSecondCrossing(bool positiveCross)
        {
            if (positiveCross)
            {
                // Search for a negative crossing that occurs within the deadTime
                return spikeDetectionBuffer.GetRange(exitSpikeIndex, deadTime).Min() < -currentThreshold;
            }
            else
            {
                // Search for a postive crossing that occurs within the deadTime
                return spikeDetectionBuffer.GetRange(exitSpikeIndex, deadTime).Max() > currentThreshold;

            }
        }

        protected int[] GetSecondaryEnterExitPoints()
        {
            bool inASpike = false;
            int[] enterExit = new int[2];
            for (int i = 0; i < deadTime + maxSpikeWidth; i++)
            {
                if (!inASpike)
                {
                    if (spikeDetectionBuffer[exitSpikeIndex + i] < currentThreshold &&
                        spikeDetectionBuffer[exitSpikeIndex + i] > -currentThreshold)
                    {
                        continue;
                    }
                    else
                    {
                        enterExit[0] = exitSpikeIndex + i;
                        inASpike = true;
                    }
                }
                else
                {
                    if (spikeDetectionBuffer[exitSpikeIndex + i] > currentThreshold &&
                        spikeDetectionBuffer[exitSpikeIndex + i] < -currentThreshold)
                    {
                        if (i == deadTime + maxSpikeWidth - 1)
                        {
                            // secondary spike is too wide, return original indicies
                            enterExit[0] = enterSpikeIndex;
                            enterExit[1] = exitSpikeIndex;
                            break;
                        }
                        continue;
                    }
                    else
                    {
                        enterExit[1] = exitSpikeIndex + i;
                        break;
                    }
                }
            }

            return enterExit;
        }


    }
}