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
using System.Collections.Generic;
using System.Text;

namespace NeuroRighter.Filters
{
    /// <summary>
    /// Filters data by clipping out stimulation artifacts and interpolating
    /// </summary>
    /// <author>John Rolston (rolston2@gmail.com)</author>
    sealed internal class ArtiFilt
    {
        readonly int samplesPre; //Samples to blank/interp pre
        readonly int samplesPost; //Samples to blank/interp post

        private double[][] buffer;
        private double[][] tempBuffer;
        private ulong[] numReadsPerChannel;
        private bool[] fitUnfinished;
        private int[] unfinishedRemainder;


        /// <summary>
        /// Constructor for ArtiFilt filter
        /// </summary>
        /// <param name="timeBefore">Time (in seconds) before stim pulse to blank/interpolate</param>
        /// <param name="timeAfter">Time (in seconds) after stim pulse to blank/interpolate</param>
        /// <param name="samplingRate">Sampling frequency (in Hz)</param>
        /// <param name="numChannels">Number of channels</param>
        /// <param name="useInterpolation">True if linear interpolation, false if clipped samples are set to zero</param>
        internal ArtiFilt(double timeBefore, double timeAfter, double samplingRate, int numChannels, bool useInterpolation)
        {
            //Compute number of samples to blank pre/post stimulus pulse
            samplesPre = (int)Math.Round(timeBefore * samplingRate);
            samplesPost = (int)Math.Round(timeAfter * samplingRate);

            //Create buffer to store data
            buffer = new double[numChannels][];
            tempBuffer = new double[numChannels][];
            for (int c = 0; c < numChannels; ++c) { buffer[c] = new double[samplesPre]; tempBuffer[c] = new double[samplesPre]; }
            numReadsPerChannel = new ulong[numChannels];
            fitUnfinished = new bool[numChannels];
            unfinishedRemainder = new int[numChannels];
        }


        internal void filter(double[][] data, List<NeuroRighter.StimTick> stimIndices, int startChannel, int numChannels)
        {
            #region Shift data
            int offset = data[startChannel].Length - samplesPre;
            int length = data[startChannel].Length;

            //Copy last samplesPre to tempBuffer
            for (int c = startChannel; c < startChannel + numChannels; ++c)
                for (int s = 0; s < samplesPre; ++s) tempBuffer[c][s] = data[c][offset + s];

            //Shift samples rightward in array
            for (int c = startChannel; c < startChannel + numChannels; ++c)
                for (int s = 1; s <= offset; ++s) data[c][length - s] = data[c][length - s - samplesPre];

            //Add last samples from previous read to left
            for (int c = startChannel; c < startChannel + numChannels; ++c)
                for (int s = 0; s < samplesPre; ++s) data[c][s] = buffer[c][s];

            //Copy tempBuffer to buffer
            for (int c = startChannel; c < startChannel + numChannels; ++c)
                for (int s = 0; s < samplesPre; ++s) buffer[c][s] = tempBuffer[c][s];
            #endregion

            #region Handle Unfinished Fit
            for (int c = startChannel; c < startChannel + numChannels; ++c)
            {
                if (fitUnfinished[c])
                {
                    //Blank data
                    for (int s = 0; s < unfinishedRemainder[c]; ++s) data[c][s] = 0.0;
                    
                    //Reset unfinished markers
                    fitUnfinished[c] = false;
                    unfinishedRemainder[c] = 0;
                }
            }
            #endregion

            #region Normal Filtering
            //Check for stimIndices in this buffer
            for (int i = 0; i < stimIndices.Count; ++i)
            {
                //Check to see if it happened in this buffer or is 'stale'
                if (stimIndices[i].numStimReads == (int)numReadsPerChannel[startChannel])
                {
                    //Reconcile index with buffer lag
                    int index = stimIndices[i].index - samplesPre;

                    //Check to see if it can be handled solely here
                    if (index < length)
                    {
                        for (int c = startChannel; c < startChannel + numChannels; ++c)
                        {
                            for (int s = index - samplesPre; s < index + samplesPost; ++s)
                            {
                                data[c][s] = 0.0;
                            }
                        }
                    }
                    //We can't finish it all now, but we'll do as much as we can
                    else
                    {
                        int remainder = 0;
                        int end = index + samplesPost;
                        if (end > length) { remainder = end - length; end = length; }
                        for (int c = startChannel; c < startChannel + numChannels; ++c)
                        {
                            for (int s = index - samplesPre; s < index + samplesPost; ++s)
                                data[c][s] = 0.0;
                        }
                        //Deal with buffer
                        end = remainder;
                        if (end > samplesPost) { remainder = end - samplesPost; end = samplesPost; }
                        for (int c = startChannel; c < startChannel + numChannels; ++c)
                        {
                            for (int s = 0; s < end; ++s) buffer[c][s] = 0.0;
                        }
                        //Deal with unfinished
                        if (remainder > 0) for (int c = startChannel; c < startChannel + numChannels; ++c)
                        {
                            fitUnfinished[c] = true; 
                            unfinishedRemainder[c] = remainder;
                        }
                    }
                }
            }
            for (int c = startChannel; c < startChannel + numChannels; ++c) ++numReadsPerChannel[c];
            #endregion
        }
    }
}
