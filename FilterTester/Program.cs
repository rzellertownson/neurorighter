using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NeuroRighter
{
    class Program
    {
        static void Main(string[] args)
        {
            //params to fill
            int filterType;
            string inputNameRaw;
            string inputNameStim;
            string outputName;

            int samplingRate;
            string extension;//output extension


            //objects to create
            NeuroRighter.FileOutput outStream;
            FileStream inStreamRaw;
            FileStream inStreamStim;

            #region get params
            //no argument case
            if (args.Length == 0)
            {
                Console.WriteLine("welcome to Filter Tester, Part of NeuroRighter");
            }


            #endregion

            #region initialization/headers

            outStream = new NeuroRighter.FileOutput(outputName, samplingRate, extension);
            inStreamRaw = new FileStream(inputNameRaw, FileMode.Open);
            inStreamStim = new FileStream(inputNameStim, FileMode.Open);
            int buffersize = 500;//samples
            double[][] filtData;
            outStream.writeHeader(numChannels,samplingRate,fileType

            #endregion

            #region filter actual data
            bool finished = false;

            while(!finished)
            {
                inStreamRaw.Read(filtData, offset, count);
                outStream.read(filtData, numChannelsData, startChannelData, length);
            }
            #endregion


        }
    }
}
