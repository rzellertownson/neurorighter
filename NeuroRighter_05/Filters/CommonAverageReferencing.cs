using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    internal sealed class CommonAverageReferencing
    {
        private double[] meanData;
        private int bufferLength;

        internal CommonAverageReferencing(int bufferLength)
        {
            this.bufferLength = bufferLength;
            meanData = new double[bufferLength];
        }

        internal void reference(double[][] data, int startChannel, int numChannels)
        {
            //Reset mean
            for (int i = 0; i < bufferLength; ++i) meanData[i] = 0.0;

            //Compute mean
            for (int i = startChannel; i < startChannel + numChannels; ++i)
                for (int j = 0; j < bufferLength; ++j)
                    meanData[j] += data[i][j];

            //Divide by num channels
            for (int i = 0; i < bufferLength; ++i) meanData[i] /= numChannels;

            //Subtract out mean
            for (int i = startChannel; i < startChannel + numChannels; ++i)
                for (int j = 0; j < bufferLength; ++j)
                    data[i][j] -= meanData[j];
        }
    }
}
