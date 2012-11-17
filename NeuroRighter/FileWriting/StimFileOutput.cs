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
    class StimFileOutput : FileOutput
    {
        internal StimFileOutput(string filenameBase, double samplingRate, string extension) :
            base(filenameBase, samplingRate, extension)
        {
            //Create output stream
            outStream = createStream(filenameBase + extension, 256 * 1024);
            writeHeader(samplingRate);
        }

        protected override void writeHeader(double samplingRate)
        {
            DateTime dt = DateTime.Now; //Get current time (local to computer)

            outStream.Write(BitConverter.GetBytes(Convert.ToDouble(samplingRate)), 0, 8); //Double: Sampling rate
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Year)), 0, 2); //Int: Year
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Month)), 0, 2); //Int: Month
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Day)), 0, 2); //Int: Day
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Hour)), 0, 2); //Int: Hour
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Minute)), 0, 2); //Int: Minute
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Second)), 0, 2); //Int: Second
            outStream.Write(BitConverter.GetBytes(Convert.ToInt16(dt.Millisecond)), 0, 2); //Int: Millisecond
        }

        internal override void write(int startTimeStim, double[] prependedData, double stimJump, int idx)
        {
            outStream.Write(BitConverter.GetBytes(startTimeStim + idx), 0, 4); //Write time (index number)
            outStream.Write(BitConverter.GetBytes((Convert.ToInt16((prependedData[idx + 1] + prependedData[idx + (int)stimJump]) / 2) - //average a couple values
                (short)1) * (short)8 +
                Convert.ToInt16((prependedData[idx + (int)(2 * stimJump) + 1] +
                prependedData[idx + (int)(3 * stimJump)]) / 2)), 0, 2); // Channel
            outStream.Write(BitConverter.GetBytes(prependedData[idx + (int)(5 * stimJump)]), 0, 8); //Stim voltage
            outStream.Write(BitConverter.GetBytes(prependedData[idx + (int)(7 * stimJump)]), 0, 8); //Stim pulse width (div by 100us)
        }

        internal override void flush()
        {
            outStream.Close();
        }
    }
}
