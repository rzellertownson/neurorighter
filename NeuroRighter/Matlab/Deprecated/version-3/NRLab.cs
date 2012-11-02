// NeuroRighter
// Copyright (c) 2008 John Rolston
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
using System.Runtime.InteropServices;
using System.IO;


namespace NeuroRighter
{
    /// <summary>
    /// 
    /// </summary>
    public interface LoadRawSignature
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="channels"></param>
        /// <param name="times"></param>
        /// <returns></returns>
        double[,] loadRaw(String filename, double[,] channels, double[,] times);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="times"></param>
        /// <returns></returns>
        double[] loadRawTimes(String filename, double[,] times);
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    class NRLab : LoadRawSignature
    {
        private const int RAW_HDR_LEN = 54;

        public double[,] loadRaw(String filename, double[,] channels, double[,] times)
        {
            //Open file
            BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read));
            int numChannelsActual = br.ReadInt16();
            int Fs = br.ReadInt32();
            int gain = br.ReadInt16();
            double[] scalingCoeffs = new double[4];
            for (int i = 0; i < 4; ++i) scalingCoeffs[i] = br.ReadDouble();
            short[] dt = new short[7];
            for (int i = 0; i < 7; ++i) dt[i] = br.ReadInt16();

            //Get number of samples per channel
            long numSamplesTotal = (br.BaseStream.Length - RAW_HDR_LEN) / numChannelsActual;
            long numSamplesExtract = Convert.ToInt64((times[0, 1] - times[0, 0]) * Fs);
            
            //channels should be [X], [X Y], or [X Y ... Z]
            int numChannelsTaken = channels.GetLength(1);

            //Create output variable
            double[,] data = new double[numChannelsTaken, numSamplesExtract];

            //Fast-forward to times of interest
            br.BaseStream.Seek(Convert.ToInt64(times[0, 0] * Fs * numChannelsActual + RAW_HDR_LEN), SeekOrigin.Begin);

            for (int s = 0; s < numSamplesExtract; ++s)
            {
                for (int c = 0; c < numChannelsTaken; ++c)
                {
                    short val = br.ReadInt16();
                    data[c,s] = scalingCoeffs[0] + scalingCoeffs[1] * (double)val +
                        scalingCoeffs[2] * scalingCoeffs[2] * (double)val +
                        scalingCoeffs[3] * scalingCoeffs[3] * scalingCoeffs[3] * (double)val;
                }
            }

            br.Close();
            return data;
        }

        public double[] loadRawTimes(String filename, double[,] times)
        {
            return new double[1] { 1 };
        }
    }
}
