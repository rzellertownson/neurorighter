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
        private ulong lastSampleRead = 0;
        private int maxNumSampToPlot = 500;
        private double downSampleFactor;
        private bool zeroBase;
        private double minUpdateTimeSec;

        /// <summary>
        /// Generic controller for NI scatter graphs using a RawDataSrv object as an input.
        /// <author> Jon Newman</author>
        /// </summary>
        /// <param name="analogScatterGraph"> A reference to an NI measurement studio ScatterGraph object</param>
        public ScatterGraphController(ref ScatterGraph analogScatterGraph, bool zeroBase)
        {
            this.analogScatterGraph = analogScatterGraph;
            for (int i = 0; i < analogScatterGraph.Plots.Count; ++i)
                {
                     // Plot options
                     analogScatterGraph.Plots[i].CanScaleYAxis = false;
                     analogScatterGraph.Plots[i].CanScaleXAxis = false;

                     // Set capacity
                     analogScatterGraph.Plots[i].HistoryCapacity = maxNumSampToPlot;

                     // The plot update speed
                     minUpdateTimeSec = 0.1; 

                }
            this.zeroBase = zeroBase;

        }

        internal void updateScatterGraph(SIMOCRawSrv dataSrv, double requestedHistorySec, double peakYAxis, double yShift)
        {
            // Plot bound settings
            ulong historySamples = (ulong)(dataSrv.sampleFrequencyHz * requestedHistorySec);

            // How many samples to plot
            if ((int)historySamples < maxNumSampToPlot)
            {
                // account for when the user wants to look at less points than 500
                maxNumSampToPlot = (int)historySamples;
                downSampleFactor = 1;
            }
            else
            {
                // All other cases, this is the max no. of points to plot
                maxNumSampToPlot = 500;

                downSampleFactor = ((double)historySamples / (double)maxNumSampToPlot);
                if (downSampleFactor < 1)
                    downSampleFactor = 1;
            }
     
            // Get the sampling period for the data coming in
            double oneOverSampleFreq = 1 / dataSrv.sampleFrequencyHz;

            // First retrieve the data thats in the range of the users request
            ulong[] availableDataRange = dataSrv.EstimateAvailableTimeRange();

            int newDataLength;

            if ((availableDataRange[1] - lastSampleRead) * oneOverSampleFreq > minUpdateTimeSec)
            {
                // x-data storage
                double[] xDat;
                
                // Get data in requested history
                RawSimocBuffer analogData;
                double k = 0; // Index in x-axis vector

                if (historySamples > availableDataRange[1])
                {
                    analogData = dataSrv.ReadFromBuffer(0, availableDataRange[1]);
                    newDataLength = (int)(analogData.rawMultiChannelBuffer[0].Length / downSampleFactor);

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
                    analogData = dataSrv.ReadFromBuffer(availableDataRange[1] - historySamples, availableDataRange[1]);
                    newDataLength = (int)(analogData.rawMultiChannelBuffer[0].Length / downSampleFactor);

                    // Make X data, always 0 to whatever the plot width is
                    xDat = new double[newDataLength];
                    for (int j = 0; j < xDat.Length; ++j)
                    {
                        xDat[j] = k * oneOverSampleFreq;
                        k = k + downSampleFactor;
                    }


                }

                // Update last sample read
                lastSampleRead = analogData.startAndEndSample[1];

                // Get plot range
                Range plotYRange;
                Range plotXRange;     
                if (!zeroBase)
                {
                    plotYRange = new Range(-peakYAxis + yShift, peakYAxis + yShift);
                    plotXRange = new Range(0, requestedHistorySec);
                }
                else
                {
                    plotYRange = new Range(-0.1 + yShift, peakYAxis + yShift);
                    plotXRange = new Range(0, requestedHistorySec);
                }

                // Update the scatter plot
                for (int i = 0; i < analogScatterGraph.Plots.Count; ++i)
                {
                    //double[] currentXdata = analogScatterGraph.Plots[i].GetXData();
                    //double[] currentYdata = analogScatterGraph.Plots[i].GetYData();

                    double[] yDatTmp = analogData.rawMultiChannelBuffer[i];
                    double[] yDatDS = new double[newDataLength];

                    // Make y data
                    k = 0;
                    for (int j = 0; j < newDataLength; ++j)
                    {
                        yDatDS[j] = yDatTmp[(int)Math.Floor(k)];
                        k = k + downSampleFactor;
                    }

                    // Append new data to old data as needed
                    double[] yDat = new double[xDat.Length];
                    double[] oldYDat = analogScatterGraph.Plots[i].GetYData();
                    int l = xDat.Length;
                    for (int j = 1; j <= xDat.Length; ++j)
                    {
                        --l;
                        if (j <= yDatDS.Length)
                            yDat[l] = yDatDS[yDatDS.Length - j];
                        else
                        {
                            try
                            {
                                yDat[l] = oldYDat[oldYDat.Length + yDatDS.Length - j];
                            }
                            catch
                            {
                                break;
                            }
                        }


                    }


                    analogScatterGraph.Plots[i].ClearData();
                    analogScatterGraph.Plots[i].YAxis.Range = plotYRange;
                    analogScatterGraph.Plots[i].XAxis.Range = plotXRange;
                    analogScatterGraph.Plots[i].PlotXYAppend(xDat, yDat);
                }
            }
        }
    }
}
