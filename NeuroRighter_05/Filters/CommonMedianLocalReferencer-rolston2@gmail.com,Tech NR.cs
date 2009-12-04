using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    internal sealed class CommonMedianLocalReferencer : Referencer
    {
        private double[][][] meanData; //group x sample x channel
        private int bufferLength;
        private int numChannelsPerGroup;

        internal CommonMedianLocalReferencer(int bufferLength, int numChannelsPerGroup, int numGroups)
        {
            this.bufferLength = bufferLength;
            this.numChannelsPerGroup = numChannelsPerGroup;
            meanData = new double[numGroups][][];
            for (int i = 0; i < numGroups; ++i)
            {
                meanData[i] = new double[bufferLength][];
                for (int j = 0; j < bufferLength; ++j)
                    meanData[i][j] = new double[numChannelsPerGroup];
            }
        }

        unsafe internal override void reference(double[][] data, int startChannel, int numChannels)
        {
            int group = startChannel / numChannelsPerGroup;

            //Store entries into meanData array
            for (int c = 0; c < numChannelsPerGroup; ++c)
                for (int s = 0; s < bufferLength; ++s)
                    meanData[group][s][c] = data[c + group * numChannelsPerGroup][s];

            //Sort
            for (int s = 0; s < bufferLength; ++s) Array.Sort(meanData[group][s]);

            //Subtract out median
            if (numChannels % 2 == 0)
            {
                for (int s = 0; s < bufferLength; ++s)
                {
                    double median = 0.5 * (meanData[group][s][(int)(numChannelsPerGroup * 0.5)] + meanData[group][s][(int)(numChannelsPerGroup * 0.5) + 1]);
                    for (int c = group * numChannelsPerGroup; c < (group + 1) * numChannelsPerGroup; ++c)
                        data[c][s] -= median;
                }
            }
            else
            {
                for (int c = group * numChannelsPerGroup; c < (group + 1) * numChannelsPerGroup; ++c)
                    for (int s = 0; s < bufferLength; ++s)
                        data[c][s] -= meanData[group][s][(int)(numChannelsPerGroup * 0.5)];
            }

        }
    }
}
