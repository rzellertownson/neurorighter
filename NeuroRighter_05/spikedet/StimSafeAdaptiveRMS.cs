using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.SpikeDetection
{
    /// <author>John Rolston (rolston2@gmail.com)</author>
    sealed class StimSafeAdaptiveRMS : SpikeDetector
    {
        private double tempData;
        private const double WINDOW = 5.0; //in seconds, how much data to average over
        private const double VOLTAGE_EPSILON = 1E-6;
        private List<List<double>> RMSList;
        private readonly int numReadsPerWindow;

        public StimSafeAdaptiveRMS(int spikeBufferLengthIn, int numChannelsIn, int downsampleIn, int spike_buffer_sizeIn,
            int numPostIn, int numPreIn, double threshMult, double deviceRefresh) :
            base(spikeBufferLengthIn, numChannelsIn, downsampleIn, spike_buffer_sizeIn, numPostIn, numPreIn, threshMult)
        {
            threshold = new double[1, numChannels];
            numReadsPerWindow = (int)Math.Round(WINDOW / deviceRefresh);
            if (numReadsPerWindow < 1) numReadsPerWindow = 1;
            RMSList = new List<List<double>>(numChannelsIn);
            for (int i = 0; i < numChannelsIn; ++i) RMSList.Add(new List<double>(numReadsPerWindow));
        }

        protected override void updateThreshold(double[] data, int channel)
        {
            tempData = 0.0;
            int samplesAdded = 0;
            for (int j = 0; j < spikeBufferLength / downsample; ++j)
            {
                if (data[j * downsample] >= VOLTAGE_EPSILON || data[j * downsample] <= -VOLTAGE_EPSILON)
                {
                    tempData += data[j * downsample] * data[j * downsample]; //Square data
                    ++samplesAdded;
                }
            }
            if (samplesAdded > 0)
            {
                tempData /= samplesAdded;
                if (RMSList[channel].Count == numReadsPerWindow)
                    RMSList[channel].RemoveAt(0);
                RMSList[channel].Add(Math.Sqrt(tempData) * _thresholdMultiplier);
                //threshold[0, channel] = Math.Sqrt(tempData) * _thresholdMultiplier;
                double avg = 0.0;
                for (int i = 0; i < RMSList[channel].Count; ++i) avg += RMSList[channel][i];
                threshold[0, channel] = avg / RMSList[channel].Count;
            }
        }
    }
}
