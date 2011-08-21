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

namespace simoc.filewriting
{

    /// <summary>
    /// Writes simoc's data double data streams to file.
    /// </summary>
    internal class FileWriter
    {

        // The data writer
        BinaryWriter simocFileWriter; 

        // Parameters
        internal int numStreams; // The number of data streams to be recorded

        public FileWriter(string fileName, int numStreams, double samplingRate)
        {
            this.numStreams = numStreams;

            //Create output file
            simocFileWriter = new BinaryWriter(File.Open(fileName, FileMode.Create));

            // Write the file header
            WriteHeader(numStreams, samplingRate);
        }

        /// <summary>
        /// Provide the time stamp of a datum and the datum to write to file
        /// </summary>
        /// <param name="timeStamp">the integer representing the sample of the datum</param>
        /// <param name="simocDatum"> the NX1 data points for each stream at sample = timeStamp</param>
        internal void WriteData(double timeStampSec, double[] simocDatum)
        {
            simocFileWriter.Write(timeStampSec);
            for (int i = 0; i < simocDatum.Length; ++i)
                simocFileWriter.Write(simocDatum[i]);
        }

        private void WriteHeader(int numStreams, double samplingRate)
        {
            DateTime dt = DateTime.Now; //Get current time (local to computer)

            //Write header info: #chs, sampling rate, date/time
            simocFileWriter.Write(numStreams); //Int: Num channels
            simocFileWriter.Write(samplingRate); //Int: Sampling rate

            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
            simocFileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond
        }

        internal void Close()
        {
            simocFileWriter.Close();
        }


        
    }
}
