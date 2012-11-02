// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
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
using System.Text;
using System.IO;
using System.Threading;
using System.Timers;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System.ComponentModel;

namespace NeuroRighter
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal class FileOutput
    {
        protected short[,] _buffer; //To store data to be written
        protected int[] _currentLocationRead;  //...in buffer for each channel
        protected int[] _currentLocationWrite;
        protected const int BUFFER_LENGTH = 25000; //Num pts to store for each channel
        protected int numChannels;
        protected double preampgain;
        protected Stream outStream;
        protected BackgroundWorker _bgWorker;

        internal FileOutput(string filenameBase, int numChannels, double samplingRate, int fileType, Task recordingTask, string extension, double preampgain)
        {
            this.numChannels = numChannels;
            this.preampgain = preampgain;
            _bgWorker = new BackgroundWorker();
            _bgWorker.WorkerSupportsCancellation = true;
            _bgWorker.DoWork += new DoWorkEventHandler(_bgWorker_doWork);
            _bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_bgWorker_runWorkerCompleted);

            _buffer = new short[numChannels, BUFFER_LENGTH];
            _currentLocationRead = new int[numChannels]; //Automatically set to 0
            _currentLocationWrite = new int[numChannels];

            //Create output file
            outStream = createStream(filenameBase + extension, 256 * 1024);

            writeHeader(numChannels, samplingRate, fileType, recordingTask, preampgain);
            _bgWorker.RunWorkerAsync();
        }

        internal FileOutput(string filenameBase, double samplingRate, string extension) { }

        protected virtual Stream createStream(String filename, Int32 bufferSize)
        {
            return new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, false);
        }

        protected virtual void writeHeader(double samplingRate){ }

        protected virtual void writeHeader(int numChannels, double samplingRate, int fileType, Task recordingTask, double preampgain)
        {
            DateTime dt = DateTime.Now; //Get current time (local to computer)
            double[] scalingCoeffs = recordingTask.AIChannels[0].DeviceScalingCoefficients;

            //Write header info: #chs, sampling rate, gain, date/time
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(numChannels)), 0, 2); //Int: Num channels
            outStream.Write(BitConverter.GetBytes(samplingRate), 0, 8); //Double: Sampling rate
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(10.0 / recordingTask.AIChannels.All.RangeHigh)), 0, 2); //Double: Gain
            switch (fileType)
            {
                case 0: //Raw or LFP
                    outStream.Write(BitConverter.GetBytes((1 / preampgain) * scalingCoeffs[0]), 0, 8); //Double: Scaling coefficients
                    outStream.Write(BitConverter.GetBytes((1 / preampgain) * scalingCoeffs[1]), 0, 8);
                    outStream.Write(BitConverter.GetBytes((1 / preampgain) * scalingCoeffs[2]), 0, 8);
                    outStream.Write(BitConverter.GetBytes((1 / preampgain) * scalingCoeffs[3]), 0, 8);
                    break;
                case 1: //Derived
                    outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                    outStream.Write(BitConverter.GetBytes((1 / preampgain) * recordingTask.AIChannels.All.RangeHigh / 32768.0), 0, 8);
                    outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                    outStream.Write(BitConverter.GetBytes(0.0), 0, 8);
                    break;
            }
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond
        }

        internal virtual void read(short[,] data, int numChannelsData, int startChannelData, int length)
        {
            unsafe
            {
                fixed (short* pdata = &data[0, 0], pbuffer = &_buffer[0, 0])
                {
                    for (int c = startChannelData; c < startChannelData + numChannelsData; ++c)
                    {
                        int baseOfDimData = (c - startChannelData) * length; //BaseOfDim is in ref to input data, which has no channel offset
                        int baseOfDimBuffer = c * BUFFER_LENGTH;

                        //Check to see if we'll loop back to front of buffer here, rather than wasting an if statement in the loop
                        if (_currentLocationRead[c] + length < BUFFER_LENGTH)
                        {
                            //Check for buffer overrun
                            if (_currentLocationWrite[c] > _currentLocationRead[c])
                            {
                                if (_currentLocationWrite[c] - _currentLocationRead[c] < length)
                                    System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }

                            //We can copy blithely, without worry of looping back around
                            for (int i = 0; i < length; ++i)
                            {
                                pbuffer[baseOfDimBuffer + _currentLocationRead[c]++] = pdata[baseOfDimData + i];
                            }
                        }
                        else
                        {
                            //Check for buffer overruns
                            if (_currentLocationWrite[c] > _currentLocationRead[c])
                            {
                                //Since we're guaranteed to go to end of buffer, if write head is higher, we will overrun
                                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }
                            int firstDistance = BUFFER_LENGTH - _currentLocationRead[c];
                            for (int i = 0; i < firstDistance; ++i)
                            {
                                pbuffer[baseOfDimBuffer + _currentLocationRead[c]++] = pdata[baseOfDimData + i];
                            }
                            _currentLocationRead[c] = 0; //Reset read head

                            if (_currentLocationWrite[c] < length - firstDistance)
                            {
                                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }
                            for (int i = firstDistance; i < length; ++i)
                            {
                                pbuffer[baseOfDimBuffer + _currentLocationRead[c]++] = pdata[baseOfDimData + i];
                            }

                        }
                    }
                }
            }
        }

        internal virtual void read(short data, int channel)
        {
            _buffer[channel, _currentLocationRead[channel]] = data;
            if (++_currentLocationRead[channel] < BUFFER_LENGTH) { /* do nothing */ }
            else { _currentLocationRead[channel] = 0; }
            if (_currentLocationRead[channel] != _currentLocationWrite[channel]) { /*do nothing*/}
            else
                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
        }

        internal virtual void write()
        {
            //First, compute how much data is ready to be written
            int[] distances = new int[numChannels];
            lock (_currentLocationRead)
            {
                for (int i = 0; i < numChannels; ++i)
                {
                    distances[i] = _currentLocationRead[i] - _currentLocationWrite[0];
                    if (_currentLocationRead[i] < _currentLocationWrite[0])
                        distances[i] += BUFFER_LENGTH;
                }
            }
            int shortestChannel = 0;
            for (int i = 1; i < numChannels; ++i)
                if (distances[i] < distances[shortestChannel])
                    shortestChannel = i;

            //Write data to file
            if (_currentLocationWrite[0] + distances[shortestChannel] < BUFFER_LENGTH)
            {
                unsafe
                {
                    fixed (short* pbuffer = &_buffer[0, 0])
                    {
                        for (int i = 0; i < distances[shortestChannel]; ++i)
                            for (int c = 0; c < numChannels; ++c)
                                outStream.Write(BitConverter.GetBytes(pbuffer[c * BUFFER_LENGTH + _currentLocationWrite[c]++]), 0, 2);
                    }
                }
            }
            else //Going to wrap around buffer
            {
                unsafe
                {
                    fixed (short* pbuffer = &_buffer[0, 0])
                    {
                        for (int i = 0; i < distances[shortestChannel]; ++i)
                        {
                            for (int c = 0; c < numChannels; ++c)
                            {
                                outStream.Write(BitConverter.GetBytes(pbuffer[c * BUFFER_LENGTH + _currentLocationWrite[c]]), 0, 2);
                                if (++_currentLocationWrite[c] < BUFFER_LENGTH) { /* do nothing */ }
                                else { _currentLocationWrite[c] = 0; }
                            }
                        }
                    }
                }
            }
        }

        internal virtual void write(int startTimeStim, double[] prependedData, double stimJump, int idx) { }

        internal virtual void write(int digChangeTime, uint digChangeState, int numReads, int buffSize) { }

        protected virtual void _bgWorker_doWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            bool isRunning = true;
            while (isRunning)
            {
                if (_bgWorker.CancellationPending)
                {
                    isRunning = false;
                    e.Cancel = true;
                    break;
                }
                else
                {
                    write();
                    Thread.Sleep(50);
                }
            }
        }

        protected virtual void _bgWorker_runWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            Thread.Sleep(50); //Give other threads a chance to write data to file
            write();
            outStream.Close();
        }

        internal virtual void flush() 
        { 
            _bgWorker.CancelAsync(); 
        }
    }
}
