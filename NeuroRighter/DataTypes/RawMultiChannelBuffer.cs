// NeuroRighter
// Copyright (c) 2008-2012 Potter Lab
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

namespace NeuroRighter.DataTypes
{
    /// <summary>
    /// This class is the standard NR buffer class for raw analog data coming from
    /// multiple input channels. It contains various properties and methods for accessing 
    /// multichannel stream data and metadata.
    /// </summary>
    public class RawMultiChannelBuffer
    {
        // User accessible
        private double sampleFrequencyHz;
        private ulong[] startAndEndSample;
        private double[][] buffer;
        private int numChannels;
        private int bufferLengthInSamples;

        // NeuroRighter accessible
        internal int[] leastCurrentCircularSample;//one for each task
        internal int[] mostCurrentCircularSample;
        internal ulong[] totalNumSamplesWritten;
        internal int[] netLeastAndMostCurrentCircular;

        /// <summary>
        /// This class is the standard NR buffer class for raw analog data coming from
        /// multiple input channels. It contains various properties and methods for accessing 
        /// multichannel stream data and metadata.
        /// </summary>
        /// <param name="sampleFrequencyHz"> Sampling frequency of data in the buffer</param>
        /// <param name="numChannelsPerTask"> Number of channels data collection task (argument 4) </param>
        /// <param name="bufferLengthInSamples">The history of the channels that should be kept, in samples</param>
        /// <param name="numDataCollectionTasks"> The number of external processes that can asynchronously add data to the buffer</param>
        public RawMultiChannelBuffer(double sampleFrequencyHz, int numChannelsPerTask, int bufferLengthInSamples, int numDataCollectionTasks)
        {
            this.netLeastAndMostCurrentCircular = new int[2];
            this.sampleFrequencyHz = sampleFrequencyHz;
            this.numChannels = numChannelsPerTask;
            this.bufferLengthInSamples = bufferLengthInSamples;
            this.startAndEndSample = new ulong[2];
            this.startAndEndSample[0] = 0;
            this.startAndEndSample[1] = 0;
            this.buffer = new double[numChannels * numDataCollectionTasks][];
            this.leastCurrentCircularSample = new int[numDataCollectionTasks];
            this.mostCurrentCircularSample = new int[numDataCollectionTasks]; ;
            this.totalNumSamplesWritten = new ulong[numDataCollectionTasks]; ;

            // Allocate space with zeros
            for (int i = 0; i < numChannels * numDataCollectionTasks; ++i)
            {

                buffer[i] = new double[bufferLengthInSamples];

                for (int j = 0; j < bufferLengthInSamples; ++j)
                {
                    buffer[i][j] = 0;
                }
            }
        }

        /// <summary>
        /// Icrement the current-most sample for each task that is providing data to the buffer.
        /// </summary>
        /// <param name="task"></param>
        internal void IncrementCurrentPosition(int task)
        {
            totalNumSamplesWritten[task]++;
            mostCurrentCircularSample[task] = leastCurrentCircularSample[task] % bufferLengthInSamples;
            leastCurrentCircularSample[task] = (++leastCurrentCircularSample[task]) % bufferLengthInSamples;
            ulong tmpa =totalNumSamplesWritten[0];
            ulong tmpi = totalNumSamplesWritten[0];

            //find the minimum and maximum no of samples written
            for (int i = 1;i<totalNumSamplesWritten.Length;i++)
            {
                if (tmpa>totalNumSamplesWritten[i])
                    tmpa = totalNumSamplesWritten[i];
                if (tmpi < totalNumSamplesWritten[i])
                    tmpi = totalNumSamplesWritten[i];
            }

            netLeastAndMostCurrentCircular[0] = (int)(tmpa % (ulong)bufferLengthInSamples);//least current circular index

            if (tmpi > 0)
                netLeastAndMostCurrentCircular[1] = (int)((tmpi - 1) % (ulong)bufferLengthInSamples);//most current circular index

            startAndEndSample[1] = tmpi - 1;
            if (tmpa < 1 || tmpa-1 < (ulong)bufferLengthInSamples)
            {
                startAndEndSample[0] = 0; 
            }
            else
            {
                startAndEndSample[0] = tmpa - 1 - (ulong)bufferLengthInSamples; 
            }

        }

        /// <summary>
        /// Used for reading only.  this means that we care about the 'youngest' of the two oldest samples and the 
        /// </summary>
        /// <param name="absoluteSampleIndex"> The absolute sample index</param>
        /// <returns>Relative sample index, based relative to the start of the last buffer load.</returns>
        internal int FindSampleIndex(ulong absoluteSampleIndex)
        {
            return (int)(absoluteSampleIndex % (ulong)bufferLengthInSamples);
        }

        /// <summary>
        /// The sampling frequency of this data stream in Hz.
        /// </summary>
        public double SampleFrequencyHz
        {
            get
            {
                return sampleFrequencyHz;
            }
        }

        /// <summary>
        /// The oldest and newest sample that currently members of the buffer.
        /// </summary>
        public ulong[] StartAndEndSample
        {
            get
            {
                return startAndEndSample;
            }
            set
            {
                startAndEndSample = value;
            }
        }

        /// <summary>
        /// Multichannle data buffer array that contains one channel's worth of data along each row.
        /// </summary>
        public double[][] Buffer
        {
            get
            {
                return buffer;
            }
        }

        /// <summary>
        /// The number channels stored in this buffer.
        /// </summary>
        public int NumChannels
        {
            get
            {
                return numChannels;
            }
        }

        /// <summary>
        /// The history of the this buffer, measured in the amount of past samples it stores for each channel.
        /// </summary>
        public int BufferLengthInSamples
        {
            get
            {
                return bufferLengthInSamples;
            }
        }
    }
}
