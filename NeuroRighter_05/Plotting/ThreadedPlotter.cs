/* 
 * ThreadedPlotter
 * Plots (currently LFP only) signals coming into circular buffer
 * (see Bufferization/DataBuffer)
 * 
 * Contributed by:
 * Alexandra Elbakyan
 * <mindwrapper@gmail.com>
 * August 2010
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NeuroRighter.Bufferization;

namespace NeuroRighter.Plotting
{
    class ThreadedPlotter
    {

        DataBuffer dBuf;
        List<int[]> myList;
        double[,] visBuf;
        int freeSpaceIndex;
        int numChannels, numPoints;
        RowGraph plot;

        public ThreadedPlotter(DataBuffer dBuf, int pointsPerPlot, RowGraph plot)
        {
            this.dBuf = dBuf;
            myList = dBuf.connectClient();
            numChannels = dBuf.numChannels;
            numPoints = pointsPerPlot;
            visBuf = new double[numChannels,numPoints * 2];
            freeSpaceIndex = 0;
            this.plot = plot;
            Thread plotThread = new Thread(new ThreadStart(plottingLoop));
            plotThread.Start();
        }

        void plottingLoop()
        {
            int i, c, k;
            while (true)
            {
                Thread.Sleep(100);
                if (myList.Count == 0)
                    continue;
                dBuf.rwLock.EnterReadLock();
                i = freeSpaceIndex;
                foreach (int[] newP in myList)
                {
                    for (k = newP[0]; k < newP[0] + newP[1]; k++, i++)
                        for (c = 0; c < numChannels; c++)
                            visBuf[c, i] = dBuf.data[k, c];
                }
                freeSpaceIndex = i;
                if (freeSpaceIndex > numPoints)
                {
                    int newPointsCnt = freeSpaceIndex - numPoints;
                    for (i = 0; i < numPoints; i++)
                        for (c = 0; c < numChannels; c++)
                            visBuf[c, i] = visBuf[c, i + newPointsCnt];
                    freeSpaceIndex = numPoints;
                }
                myList.Clear();
                dBuf.rwLock.ExitReadLock();
                float[] data = new float[numPoints];
                for (c = 0; c < numChannels; c++)
                {
                    for (i = 0; i < numPoints; i++)
                        data[i] = (float)visBuf[c, i]*1000 - c*200;
                    plot.plotY(data, 0F, 1F, Microsoft.Xna.Framework.Graphics.Color.Lime, c);
                }
                plot.Invalidate();
            }
        }

    }
}
