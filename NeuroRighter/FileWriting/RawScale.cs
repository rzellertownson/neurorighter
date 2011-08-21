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
        private double scaleResolution;
        private double[][] convertedDataDouble;

        public RawScale()
        {
            
        }

        internal Int16[] ConvertHardRawRowToInt16(ref double[] doubleData)
        {
            lock (this)
            {
                short [] convertedData = new short[doubleData.Length];

                for (int i = 0; i < doubleData.Length; ++i)
                {
                    convertedData[i] = (short)Math.Round(doubleData[i] * oneOverResolution);
                }

                return convertedData;
            }
        }

        internal Int16[,] ConvertHardRawMatrixToInt16(ref double[,] doubleData)
        {
            lock (this)
            {
                short[,] convertedData = new short[doubleData.GetLength(0), doubleData.GetLength(1)];

                for (int i = 0; i < doubleData.GetLength(0); ++i)
                {
                    for (int j = 0; j < doubleData.GetLength(1); ++j)
                    {
                        convertedData[i,j] = (short)Math.Round(doubleData[i,j] * oneOverResolution);
                    }
                }

                return convertedData;
            }
        }

        internal Int16[] ConvertSoftRawRowToInt16(ref double[] doubleData)
        {
            lock (this)
            {
                //This method deals with the fact that NI's range is soft--
                //i.e., values can exceed the max and min values of the range 
                // (but trying to convert these to shorts would crash the program)
                short[] convertedData = new short[doubleData.Length];

                for (int i = 0; i < doubleData.Length; ++i)
                {
                    convertedData[i] = (short)Math.Round(doubleData[i] * oneOverResolution);
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

        internal Int16[,] ConvertSoftRawMatixToInt16(ref double[,] doubleData)
        {
            lock (this)
            {
                short[,] convertedData = new short[doubleData.GetLength(0), doubleData.GetLength(1)];

                for (int i = 0; i < doubleData.GetLength(0); ++i)
                {

                    for (int j = 0; j < doubleData.GetLength(1); ++j)
                    {
                        convertedData[i,j] = (short)Math.Round(doubleData[i,j] * oneOverResolution);
                        if (convertedData[i, j] <= Int16.MaxValue && convertedData[i, j] >= Int16.MinValue)
                        {
                            //do nothing, most common case 
                        }
                        else if (convertedData[i, j] > Int16.MaxValue)
                        {
                            convertedData[i, j] = Int16.MaxValue;
                        }
                        else
                        {
                            convertedData[i, j] = Int16.MinValue;
                        }
                    }
                }

                return convertedData;
            }
            
        }

        internal double[][] ConvertInt16ToSoftRaw(ref short[,] analogData)
        {
            lock (this)
            {
                convertedDataDouble = new double[analogData.GetLength(0)][];
                for (int j = 0; j < analogData.GetLength(0); ++j)
                {
                    convertedDataDouble[j] = new double[analogData.GetLength(1)];

                    for (int i = 0; i < analogData.GetLength(1); ++i)
                    {
                        convertedDataDouble[j][i] = (double)(analogData[j,i] * scaleResolution);
                    }
                }

                return convertedDataDouble;
            }

        }

        internal void Set16BitResolution(double resolution)
        {
            // Set the double value corresonding to one 16-bit increment of your analog input
            oneOverResolution = 1 / resolution;
            scaleResolution = resolution;
        }

    }
}
