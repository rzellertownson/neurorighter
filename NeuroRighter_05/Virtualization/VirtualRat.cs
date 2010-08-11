/* 
 * VirtualRat
 * Simulation of signals coming from hardware. For easier development.
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
using System.IO;

namespace NeuroRighter.Virtualization
{
    class VirtualRat
    {

        public double[,] buf;
        public int numChannels;
        public int samplingRate;
        public short[] recordTime;
        public long duration;

        public delegate void dataAcquiredHandler(object sender);
        public event dataAcquiredHandler dataAcquired;

        FileStream dataFile;
        int gain;
        double[] scalingCoeffs;

        int refreshInterval;
        bool isRunning;

        public VirtualRat()
        {
            scalingCoeffs = new double[4];
            recordTime = new short[7];
            refreshInterval = 100;
            isRunning = false;
        }

        public void loadRecordedData(string filename)
        {
            dataFile = File.OpenRead(filename);
            byte[] header = new byte[54];
            dataFile.Read(header, 0, 54);
            numChannels = BitConverter.ToInt16(header, 0);
            samplingRate = BitConverter.ToInt32(header, 2);
            duration = (dataFile.Length - header.Length) / (numChannels * samplingRate * sizeof(short));
            gain = BitConverter.ToInt16(header, 6);
            int i;
            for (i = 0; i < scalingCoeffs.Length; i++)
                scalingCoeffs[i] = BitConverter.ToDouble(header, 8 + i * 8);
            for (i = 0; i < recordTime.Length; i++)
                recordTime[i] = BitConverter.ToInt16(header, 40 + i * 2);
        }

        public void startPlayBack(int speedToReal)
        {
            buf = new double[numChannels, samplingRate * refreshInterval * speedToReal / 1000];
            isRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(playBack));            
        }

        public void stopPlayBack()
        {
            isRunning = false;
        }

        void playBack(object x)
        {
            int numSamples = buf.GetLength(1);
            byte[] currentSamplesBuf = new byte[numSamples * numChannels * sizeof(short)];
            short temp;
            while (isRunning && dataFile.Position < dataFile.Length)
            {
                dataFile.Read(currentSamplesBuf, 0, currentSamplesBuf.Length);
                int si = 0;
                for (int i = 0; i < numSamples; i++)
                    for (int c = 0; c < numChannels; c++, si += 2)
                    {
                        temp = BitConverter.ToInt16(currentSamplesBuf, si);
                        buf[c, i] = scalingCoeffs[0] + scalingCoeffs[1] * (double)temp +
                                    scalingCoeffs[2] * scalingCoeffs[2] * (double)temp +
                                    scalingCoeffs[3] * scalingCoeffs[3] * scalingCoeffs[3] * (double)temp;
                    }
                dataAcquired(this);
                Thread.Sleep(refreshInterval);
            }
        }

    }
}
