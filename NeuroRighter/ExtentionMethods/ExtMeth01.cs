using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ExtensionMethods
{
    /// <summary>
    /// Extension Methods for NeuroRighter
    /// </summary>
    /// <author> Jon Newman</author>
    public static class NRExtensionMethods
    {
        /// <summary>
        /// Method for getting index of max value of 
        /// a generic collection
        /// </summary>
        /// <typeparam name="T">Generic collection</typeparam>
        /// <param name="sequence"></param>
        /// <returns>Max of genertic collection</returns>
        public static int MaxIndex<T>(this IEnumerable<T> sequence)
            where T : IComparable<T>
        {
            int maxIndex = -1;
            T maxValue = default(T); // Immediately overwritten anyway

            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxValue = value;
                }
                index++;
            }
            return maxIndex;
        }

        /// <summary>
        /// Method for getting index of min value of 
        /// a generic collection
        /// </summary>
        /// <typeparam name="T">Generic collection</typeparam>
        /// <param name="sequence"></param>
        /// <returns>Min of genertic collection</returns>
        public static int MinIndex<T>(this IEnumerable<T> sequence)
            where T : IComparable<T>
        {
            int minIndex = -1;
            T minValue = default(T); // Immediately overwritten anyway

            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(minValue) < 0 || minIndex == -1)
                {
                    minIndex = index;
                    minValue = value;
                }
                index++;
            }
            return minIndex;
        }

        /// <summary>
        /// Method for creating deep copies of objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Method for geneating the default color map for NR plotting
        /// </summary>
        /// <param name="numChannels">int number of channels</param>
        /// <returns>List of color objects</returns>
        public static List<Color> GenerateBrainbow(this int numChannels)
        {
            // This is generated code
            byte[,] cmap = new byte[64, 3]  {{255,0,0},
                                            {255,24,0},
                                            {255,48,0},
                                            {255,72,0},
                                            {255,96,0},
                                            {255,120,0},
                                            {255,144,0},
                                            {255,168,0},
                                            {255,192,0},
                                            {255,216,0},
                                            {255,240,0},
                                            {248,255,0},
                                            {224,255,0},
                                            {200,255,0},
                                            {176,255,0},
                                            {152,255,0},
                                            {128,255,0},
                                            {104,255,0},
                                            {80,255,0},
                                            {56,255,0},
                                            {32,255,0},
                                            {8,255,0},
                                            {0,255,16},
                                            {0,255,40},
                                            {0,255,64},
                                            {0,255,88},
                                            {0,255,112},
                                            {0,255,136},
                                            {0,255,160},
                                            {0,255,184},
                                            {0,255,208},
                                            {0,255,232},
                                            {0,255,255},
                                            {0,232,255},
                                            {0,208,255},
                                            {0,184,255},
                                            {0,160,255},
                                            {0,136,255},
                                            {0,112,255},
                                            {0,88,255},
                                            {0,64,255},
                                            {0,40,255},
                                            {0,16,255},
                                            {8,0,255},
                                            {32,0,255},
                                            {56,0,255},
                                            {80,0,255},
                                            {104,0,255},
                                            {128,0,255},
                                            {152,0,255},
                                            {176,0,255},
                                            {200,0,255},
                                            {224,0,255},
                                            {248,0,255},
                                            {255,0,240},
                                            {255,0,216},
                                            {255,0,192},
                                            {255,0,168},
                                            {255,0,144},
                                            {255,0,120},
                                            {255,0,96},
                                            {255,0,72},
                                            {255,0,48},
                                            {255,0,24}};

            // Create color list from RGB map above
            List<Color> NRColorMap = new List<Color>();
            for (int i = 0; i < numChannels; ++i)
            {
                Color colTemp = new Color(cmap[i, 0], cmap[i, 1], cmap[i, 2]);
                NRColorMap.Add(colTemp);
            }

            return NRColorMap;
        }

        /// <summary>
        /// Method for geneating the default color map for NR plotting for sorted units.
        /// </summary>
        /// <param name="numChannels">int number of channels</param>
        /// <returns>List of color objects</returns>
        public static List<Color> GenerateUnitBrainbow(this int numChannels)
        {
            // This is generated code
            byte[,] cmap = new byte[7, 3]  {{120,120,120},
                                            {255,24,0},
                                            {255,240,0},
                                            {0,208,255},
                                            {32,255,0},
                                            {255,0,240},
                                            {255,255,255}};

            // Create color list from RGB map above
            List<Color> NRColorMap = new List<Color>();
            for (int i = 0; i < cmap.GetLength(0); ++i)
            {
                Color colTemp = new Color(cmap[i, 0], cmap[i, 1], cmap[i, 2]);
                NRColorMap.Add(colTemp);
            }

            return NRColorMap;
        }

    }
}
