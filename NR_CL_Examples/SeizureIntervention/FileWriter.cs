using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Timers;
using System.ComponentModel;
using System.Text;

namespace NR_CL_Examples
{

    /// <summary>
    /// Writes data streams to file.
    /// </summary>
    public class FileWriter
    {

        // The data writer
        BinaryWriter fileWriter;

        // Parameters
        internal int numStreams; // The number of data streams to be recorded

        public FileWriter(string fileName, int numStreams, double samplingRate, string type)
        {
            this.numStreams = numStreams;

            //Create output file
            fileWriter = new BinaryWriter(File.Open(fileName, FileMode.Create));

            // Write the file header
            WriteHeader(numStreams, samplingRate, type);
        }

        /// <summary>
        /// Provide the time stamp of a datum and the datum to write to file
        /// </summary>
        /// <param name="timeStamp">the integer representing the sample of the datum</param>
        /// <param name="datum"> the NX1 data points for each stream at sample = timeStamp</param>
        internal void WriteData(double timeStampSec, double[] datum)
        {
            fileWriter.Write(timeStampSec);
            for (int i = 0; i < datum.Length; ++i)
                fileWriter.Write(datum[i]);
        }

        private void WriteHeader(int numStreams, double samplingRate, string type)
        {
            DateTime dt = DateTime.Now; //Get current time (local to computer)

            //Write header info: #chs, sampling rate, date/time
            fileWriter.Write(numStreams); //Int: Num channels
            fileWriter.Write(samplingRate); //Int: Sampling rate
            
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
            fileWriter.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond

            fileWriter.Write(type); // The statistic that was used to trigger stimulation
        }

        internal void Close()
        {
            fileWriter.Close();
        }
    }
}
