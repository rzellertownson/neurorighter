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
using NeuroRighter.DataTypes;

namespace NeuroRighter.SpikeDetection
{
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
        protected double[][] detectionCarryOverBuffer;
        internal protected int carryOverLength;
        protected double[,] threshold;
        protected double _thresholdMultiplier;
        protected float[][] returnThresh;
        protected int deadTime; //Num samples overlap between possible spike detections
        protected int[] initialSamplesToSkip;
        protected bool[] inASpike; // true when the waveform is over or under the current detection threshold for a given channel
        internal double thresholdMultiplier
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
        protected bool[] regularDetect;
        protected List<double> spikeDetectionBuffer; // buffer for single channe's worth of spike data
        protected bool posCross; // polarity of inital threshold crossing
        protected int recIndexOffset;
        protected int[] deadWidth;
        protected bool inBounds;

        public SpikeDetector(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, 
            int spikeWaveformLength, int numPostIn, int numPreIn, double threshMult, int detectionDeadTime,
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

            if (deadTime != 0)
                this.carryOverLength = numPre + 2*maxSpikeWidth + deadTime + numPost;
            else
                this.carryOverLength = numPre + maxSpikeWidth + numPost;

            this.initialSamplesToSkip = new int[numChannels];

            detectionCarryOverBuffer = new double[numChannels][];
            for (int i = 0; i < numChannels; ++i)
            {
                this.detectionCarryOverBuffer[i] = new double[carryOverLength];
            }

        }

        // The threshold update methods are the only things that change for
        // different spike detectors
        protected virtual void updateThreshold(double[] data, int channel) { }

        protected virtual void updateThreshold(double[] data, int channel, int idx) { }

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
        internal virtual List<SpikeEvent> DetectSpikes(double[] data, int channel, ulong bufferOffset)
        {
            if (bufferOffset==0)
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
                
                // Reset the skip value to 0
                initialSamplesToSkip[channel] = 0;

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
                    //peak detection- just requires one sample
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

                            // Positive or negative crossing
                            posCross = FindSpikePolarityBySlopeOfCrossing();
                        }
                    }
                        //exiting a spike- requires + maxspikewidth (to find peak), -pre and +post (to find waveform)
                    else if (inASpike[channel] && 
                            ((posCross && spikeDetectionBuffer[i] < currentThreshold) ||
                             (!posCross && spikeDetectionBuffer[i] > -currentThreshold))
                            )
                    {

                        // We were in a spike and now we are exiting
                        inASpike[channel] = false;
                        exitSpikeIndex = i;

                        // Calculate Spike width
                        spikeWidth = exitSpikeIndex - enterSpikeIndex;
       
                        // Find the index + value of the spike maximum
                        int spikeMaxIndex = FindMaxDeflection(posCross, enterSpikeIndex, spikeWidth);
                        double spikeMax = spikeDetectionBuffer[spikeMaxIndex];

                        // Define spike waveform
                        double[] waveform = CreateWaveform(spikeMaxIndex);

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
                                int deadMaxIndex = FindMaxDeflection(exitSpikeIndex, deadTime);

                                // Check that the maximal value in the deadtime is not the 
                                // exitSpikeIndex
                                if (deadMaxIndex == exitSpikeIndex)
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
                                    double[] deadWaveform = CreateWaveform(deadMaxIndex);

                                    if (deadWidth != null)
                                    {
                                        bool goodDeadSpike = CheckSpike(deadWidth[2], deadWaveform);

                                        if (goodDeadSpike && inflectionWithinDead)
                                        {
                                            waveform = deadWaveform;
                                            spikeMaxIndex = deadMaxIndex;
                                            spikeMax = deadMax;
                                            exitSpikeIndex = deadWidth[1];
                                        }
                                    }
                                }
                            }
                        
                        ProcessSpike:
                            // Record the waveform
                            ulong tmpindex = ((ulong)(spikeMaxIndex) + bufferOffset) - (ulong)(recIndexOffset);
                   
                            waveforms.Add(new SpikeEvent(channel,
                                tmpindex, currentThreshold, waveform));

                            // Calculate dead-time
                            int dt;
                            if (!inflectionWithinDead && deadWidth !=null)
                                dt = deadWidth[0] - 1;
                            else
                                dt = deadTime;

                            // Carry-over dead time if we are at the end of the buffer
                            if (i >= indiciesToSearchForCross)
                                initialSamplesToSkip[channel] = dt + exitSpikeIndex - indiciesToSearchForCross;
                            else
                                initialSamplesToSkip[channel] = 0;

                            // Advance through deadTime measured from the spike exit index
                            if (!inflectionWithinDead && deadWidth != null)
                                i = deadWidth[0] - 1;

                            else
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
        protected bool CheckSpike(int spikeWidth, double[] waveform)
        {
            // Check spike width
            bool spikeWidthGood = maxSpikeWidth >= spikeWidth && minSpikeWidth <= spikeWidth;
            if (!spikeWidthGood)
                return spikeWidthGood;

            // Check spike amplitude
            double[] absWave = new double[waveform.Length];
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

        protected int FindMaxDeflection(int startInd, int widthToSearch)
        {
            List<double> spkSnip = spikeDetectionBuffer.GetRange(startInd, widthToSearch);

            // Find absolute maximum
            for (int i = 0; i < spkSnip.Count; ++i)
                spkSnip[i] = Math.Abs(spkSnip[i]);
            return spkSnip.MaxIndex() + startInd;

        }

        protected int FindMaxDeflection(bool positiveCross, int startInd, int widthToSearch)
        {
            List<double> spkSnip = spikeDetectionBuffer.GetRange(startInd, widthToSearch);

            // Switch between max or min
            if (posCross)
            {
                return spkSnip.MaxIndex() + startInd;
            }
            else
            {
                return spkSnip.MinIndex() + startInd;
            }

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

        protected double[] CreateWaveform(int maxIdx)
        {
            double[] waveform = new double[numPost + numPre + 1];
            for (int j = maxIdx - numPre; j < maxIdx + numPost + 1; ++j)
                waveform[j - maxIdx + numPre] = spikeDetectionBuffer[j];
            return waveform;
        }

        protected int[] FindWidthFromMaxInd(int maxIdx)
        {
            int[] enterExitWidth = new int[3];
            int toSearch;
            if (maxSpikeWidth > maxIdx)
                toSearch = maxIdx;
            else
                toSearch = maxSpikeWidth;

            for (int i = 1; i < toSearch; ++i)
            {
                if (spikeDetectionBuffer[maxIdx - i] < currentThreshold &&
                    spikeDetectionBuffer[maxIdx - i] > -currentThreshold)
                {
                    enterExitWidth[0] = maxIdx - i;
                    break;
                }
                else if (i == maxSpikeWidth - 1)
                {
                    return null;
                }
            }

            for (int i = 1; i < maxSpikeWidth; ++i)
            {
                if (spikeDetectionBuffer[maxIdx + i] < currentThreshold &&
                    spikeDetectionBuffer[maxIdx + i] > -currentThreshold)
                {
                    enterExitWidth[1] = maxIdx + i;
                    enterExitWidth[2] = enterExitWidth[1] - enterExitWidth[0];
                    return enterExitWidth;
                }
                else if (i == maxSpikeWidth - 1)
                {
                    return null;
                }
            }

            return null;
        }
    }
}