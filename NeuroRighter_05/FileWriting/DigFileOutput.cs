using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Timers;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System.ComponentModel;

namespace NeuroRighter.FileWriting
{
    ///<summary>Class for logging digital events to file.</summary>
    ///<author>Jon Newman</author>
    class DigFileOutput : FileOutput
    {
        internal DigFileOutput(string filenameBase, int samplingRate, string extension) :
            base(filenameBase, samplingRate, extension)
        {
            //Create output stream
            outStream = createStream(filenameBase + extension, 256 * 1024);
            writeHeader(samplingRate);
        }

        protected override void writeHeader(int samplingRate)
        {
            DateTime dt = DateTime.Now; //Get current time (local to computer)

            outStream.Write(BitConverter.GetBytes(samplingRate), 0, 4); //Int: Sampling rate
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond
        }

        internal override void write(int digChangeTime, uint digChangeState, int numReads, int buffSize)
        {
            outStream.Write(BitConverter.GetBytes(digChangeTime + ((numReads-1) * buffSize)), 0, 4); //Write port-change time (index number)
            outStream.Write(BitConverter.GetBytes(digChangeState),0,4); // Port state at change
        }

        internal override void flush()
        {
            outStream.Close();
        }
    }
}
