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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;
using NeuroRighter.Output;
using NeuroRighter.DataTypes;
using NeuroRighter.DatSrv;
using simoc.srv;

namespace simoc.plotting
{
    class ScatterGraphController
    {

        private ScatterGraph analogScatterGraph;
        private ulong lastSampleRead ;
        private ulong numSampToPlot = 1500; //(ulong)Math.Floor(Properties.Settings.Default.RawSampleFrequency * Properties.Settings.Default.ADCPollingPeriodSec);

        /// <summary>
        /// Generic controller for NI scatter graphs using datSrv as an input.
        /// <author> Jon Newman</author>
        /// </summary>
        /// <param name="analogScatterGraph"></param>
        public ScatterGraphController(ref ScatterGraph analogScatterGraph)
        {
            this.analogScatterGraph = analogScatterGraph;
            for (int i = 0; i < analogScatterGraph.Plots.Count; ++i)
                {
                     // Plot options
                     analogScatterGraph.Plots[i].CanScaleYAxis = false;
                     analogScatterGraph.Plots[i].CanScaleXAxis = false;
                     analogScatterGraph.Plots[i].AntiAliased = true;
                }

        }

        internal void updateScatterGraph(SIMOCRawSrv analogDataServer, double requestedHistorySec, double peakVoltage, double shift)
        {
     
            // One over samplefreq
            double oneOverSampleFreq = 1 / analogDataServer.sampleFrequencyHz;

            // First retrieve the data thats in the range of the users request
            ulong[] availableDataRange = analogDataServer.EstimateAvailableTimeRange();

            // Plot bound settings
            ulong historySamples = (ulong)(analogDataServer.sampleFrequencyHz * requestedHistorySec);
            double minUpdateTimeSec = 0.2; //seconds
            int downSampleFactor;
            if (historySamples <= 1500)
            {
                numSampToPlot = historySamples;
                downSampleFactor = 1;
            }
            else
            {
                numSampToPlot = 1500;
                downSampleFactor = (int)(historySamples / numSampToPlot);
                
            }
            int newDataLength;

            if ((availableDataRange[1]-lastSampleRead)*oneOverSampleFreq > minUpdateTimeSec)
            {
                // x-data storage
                double[] xDat;
                int k = 0;
                // Get data in requested history
                RawSimocBuffer analogData;
                if (historySamples > availableDataRange[1])
                {
                    analogData = analogDataServer.ReadFromBuffer(0, availableDataRange[1]);
                    newDataLength = analogData.rawMultiChannelBuffer[0].Length / downSampleFactor;

                    // Make X data, always 0 to whatever the plot width is
                    xDat = new double[newDataLength];
                    for (int j = 0; j < xDat.Length; ++j)
                    {
                        xDat[j] = k * oneOverSampleFreq;
                        k = k + downSampleFactor;
                    }
                }
                else
                {
                    analogData = analogDataServer.ReadFromBuffer(availableDataRange[1] - historySamples, availableDataRange[1]);
                    newDataLength = analogData.rawMultiChannelBuffer[0].Length / downSampleFactor;

                    // Make X data, always 0 to whatever the plot width is
                    xDat = new double[numSampToPlot];
                    for (int j = 0; j < xDat.Length; ++j)
                    {
                        xDat[j] = k * oneOverSampleFreq;
                        k = k + downSampleFactor;
                    }
                }

                // Update last sample read
                lastSampleRead = analogData.startAndEndSample[1];

                // Get plot range
                Range plotYRange = new Range(-peakVoltage + shift, peakVoltage + shift);
                Range plotXRange = new Range(0, xDat[xDat.Length-1]);

                // Update the scatter plot
                for (int i = 0; i < analogScatterGraph.Plots.Count; ++i)
                {
                    // Make y data
                    k = 0;
                    double[] yDat = new double[xDat.Length];
                    for (int j = 0; j < xDat.Length; ++j)
                    {
                        yDat[j] = analogData.rawMultiChannelBuffer[i][k];
                        k = k + downSampleFactor;
                    }

                    // Set capacity
                    analogScatterGraph.Plots[i].HistoryCapacity = (int)numSampToPlot;

                    // set plot range
                    analogScatterGraph.Plots[i].YAxis.Range = plotYRange;
                    analogScatterGraph.Plots[i].XAxis.Range = plotXRange;
                    analogScatterGraph.Plots[i].PlotXY(xDat,yDat);
                }
            }

        }
    }
}