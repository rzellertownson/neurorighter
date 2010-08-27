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
using System.ComponentModel;
using NeuroRighter.Bufferization;

namespace NeuroRighter.Virtualization
{

    class VirtualRat
    {

        public DataType dataType;

        public double[,] buf;
        public int numChannels;
        public int samplingRate;
        public short[] recordTime;
        public long duration;

        public event DoWorkEventHandler dataAcquired;
        public delegate void playbackStatusHandler(VirtualRat sender);
        public event playbackStatusHandler playbackStatus;

        Thread ratThread;
        FileStream dataFile;
        int gain;
        double[] scalingCoeffs;
        int refreshInterval;
        int chunkLen_msec;
        
        public bool isRunning;
        public int currentTime;

        public VirtualRat()
        {
            scalingCoeffs = new double[4];
            recordTime = new short[7];
            refreshInterval = 50;
            currentTime = 0;
            isRunning = false;
        }

        public void loadRecordedData(string filename)
        {
            switch ((new FileInfo(filename)).Extension)
            {
                case ".raw": dataType = DataType.raw;
                    break;
                case ".lfp": dataType = DataType.lfp;
                    break;
                default:
                    return;
            }
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

        public void startPlayBack(int startTime_msec, int speedToReal, double refreshRate)
        {
            refreshInterval = (int)(refreshRate * 1000);
            chunkLen_msec = refreshInterval * speedToReal;
            buf = new double[(int)((double)samplingRate * chunkLen_msec / 1000), numChannels];
            currentTime = startTime_msec;
            dataFile.Seek(54 + (int)((double)samplingRate * startTime_msec / 1000) * numChannels * sizeof(short), SeekOrigin.Begin);
            isRunning = true;
            ratThread = new Thread(new ThreadStart(playBack));
            ratThread.Start();
        }

        public void stopPlayBack()
        {
            isRunning = false;
        }

        void playBack()
        {
            int numSamples = buf.GetLength(0);
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
                        buf[i, c] = scalingCoeffs[0] + scalingCoeffs[1] * (double)temp +
                                    scalingCoeffs[2] * scalingCoeffs[2] * (double)temp +
                                    scalingCoeffs[3] * scalingCoeffs[3] * scalingCoeffs[3] * (double)temp;
                    }
                dataAcquired(this, null);
                currentTime += chunkLen_msec;
                playbackStatus(this);
                Thread.Sleep(refreshInterval);
            }
            isRunning = false;
            playbackStatus(this);
        }

    }
}
