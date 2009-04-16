using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    internal class ArtiFilt_Interpolation : ArtiFilt
    {
        private double[] interpSlope;
        private double[] interpCurrentOffset;

        internal ArtiFilt_Interpolation(double timeBefore, double timeAfter, double samplingRate, int numChannels) :
            base(timeBefore + 1/samplingRate, timeAfter + 1/samplingRate, samplingRate, numChannels)
        {
            interpSlope = new double[numChannels];
            interpCurrentOffset = new double[numChannels];
        }

        internal override void filter(ref double[][] data, List<NeuroRighter.StimTick> stimIndices, int startChannel, int numChannels, int numReadsPerChannel)
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
                    for (int s = 0; s < unfinishedRemainder[c]; ++s) data[c][s] = (interpCurrentOffset[c] += interpSlope[c]);

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
                if (stimIndices[i].numStimReads == numReadsPerChannel)
                {
                    //Reconcile index with buffer lag
                    int index = stimIndices[i].index + samplesPre;

                    //Check to see if it can be handled solely here
                    if (index < length - samplesPost)
                    {
                        for (int c = startChannel; c < startChannel + numChannels; ++c)
                        {
                            //Calculate slope
                            interpSlope[c] = (data[c][index + samplesPost - 1] - data[c][index - samplesPre]) / (samplesPost + samplesPre);
                            interpCurrentOffset[c] = data[c][index - samplesPre];

                            //Perform fit
                            for (int s = index - samplesPre + 1; s < index + samplesPost - 1; ++s) //1's to account for slope measurement
                            {
                                data[c][s] = (interpCurrentOffset[c] += interpSlope[c]);
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
                            //Calculate slope
                            interpSlope[c] = 0.0; //Can't do it, since next pt. is in next buffer read
                            interpCurrentOffset[c] = data[c][index - samplesPre];

                            //Perform fit
                            for (int s = index - samplesPre + 1; s < end; ++s)
                                data[c][s] = (interpCurrentOffset[c] += interpSlope[c]);
                        }
                        //Deal with buffer
                        end = remainder;
                        if (end > samplesPre) { remainder = end - samplesPre; end = samplesPre; }
                        for (int c = startChannel; c < startChannel + numChannels; ++c)
                        {
                            for (int s = 0; s < end; ++s) buffer[c][s] = (interpCurrentOffset[c] += interpSlope[c]);
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
            //for (int c = startChannel; c < startChannel + numChannels; ++c) ++numReadsPerChannel[c];
            #endregion
        }
    }
}
