using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    internal sealed class CommonMedianReferencer : Referencer
    {
        private double[][] meanData;
        private int bufferLength;

        internal CommonMedianReferencer(int bufferLength, int numChannels)
        {
            this.bufferLength = bufferLength;
            meanData = new double[bufferLength][];
            for (int i = 0; i < bufferLength; ++i) meanData[i] = new double[numChannels];
        }

        unsafe internal override void reference(double[][] data, int startChannel, int numChannels)
        {
            //Reset mean
            //for (int i = 0; i < bufferLength; ++i) meanData[i] = 0.0;

            //Store entries into meanData array
            for (int i = startChannel; i < startChannel + numChannels; ++i)
                for (int j = 0; j < bufferLength; ++j)
                    meanData[j][i] = data[i][j]; //Note i/j inversion

            //Sort
            for (int j = 0; j < bufferLength; ++j) Array.Sort(meanData[j]);

            //Subtract out median
            if (numChannels % 2 == 0)
            {
                for (int j = 0; j < bufferLength; ++j)
                {
                    //double median = 0.5 * (meanData[j][startChannel + numChannels / 2] + meanData[j][startChannel + numChannels / 2 + 1]);
                    double median = 0.5 * (meanData[j][meanData[j].Length / 2] + meanData[j][meanData[j].Length / 2 + 1]);
                    for (int i = startChannel; i < startChannel + numChannels; ++i)
                        data[i][j] -= median;
                }
            }
            else
            {
                for (int i = startChannel; i < startChannel + numChannels; ++i)
                    for (int j = 0; j < bufferLength; ++j)
                        data[i][j] -= meanData[j][startChannel + numChannels / 2];
            }

        }
    }
}
