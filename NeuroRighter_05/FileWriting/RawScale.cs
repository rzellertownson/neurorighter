using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.FileWriting
{
    /// <summary>
    /// Class for manipulation of analog raw input in order to make it fit for file writing
    /// <author> Jon Newman</author>
    /// </summary>
    /// 
    internal class RawScale
    {
        private double oneOverResolution;
        private Int16[] convertedData;

        public RawScale()
        {
            
        }

        internal Int16[] ConvertHardRawToInt16(ref double[] spikeData)
        {
            lock (this)
            {
                convertedData = new short[spikeData.Length];

                for (int i = 0; i < spikeData.Length; ++i)
                {
                    convertedData[i] = (short)Math.Round(spikeData[i] * oneOverResolution);
                }

                return convertedData;
            }
        }

        internal Int16[] ConvertSoftRawToInt16(ref double[] spikeData)
        {
            lock (this)
            {
                //This method deals with the fact that NI's range is soft--
                //i.e., values can exceed the max and min values of the range 
                // (but trying to convert these to shorts would crash the program)
                convertedData = new short[spikeData.Length];

                for (int i = 0; i < spikeData.Length; ++i)
                {
                    convertedData[i] = (short)Math.Round(spikeData[i] * oneOverResolution);
                    if (convertedData[i] <= Int16.MaxValue && convertedData[i] >= Int16.MinValue)
                    {
                        //do nothing, most common case 
                    }
                    else if (convertedData[i] > Int16.MaxValue)
                    {
                        convertedData[i] = Int16.MaxValue;
                    }
                    else
                    {
                        convertedData[i] = Int16.MinValue;
                    }
                }
            

            return convertedData;
            }
        }

        internal void Set16BitResolution(double resolution)
        {
            // Set the double value corresonding to one 16-bit increment of your analog input
            oneOverResolution = 1 / resolution;
        }

    }
}
