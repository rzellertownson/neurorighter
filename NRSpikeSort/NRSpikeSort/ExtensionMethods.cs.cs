//RapidSort - A fast, unsupervised spike sorting program
//Copyright (C) 2011  Jonathan Newman

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU Lesser General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU Lesser General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO.Compression;

namespace NRSpikeSort.Extensions
{
    /// <summary>
    /// Extension methods of NRSpikeSort
    /// </summary>
    /// <author> Jon Newman</author>
    public static class ExtensionMethods
    {

        private const double sqrt2 = 1.41421356;

        /// <summary>
        /// Error function
        /// </summary>
        /// <param name="x">Arguement to the error function</param>
        /// <returns>Error function value</returns>
        public static double ErrorFunction(this double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        /// <summary>
        /// Calculate standard deviation of elements in list
        /// </summary>
        /// <param name="values">Data vector</param>
        /// <returns>Standard deviation of data vector</returns>
        public static double StandardDeviation(this IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        /// <summary>
        /// Sqrt of 2
        /// </summary>
        public static double Sqrt2
        {
            get 
            {
                return sqrt2; 
            }
        }

        /// <summary>
        /// Get a deep clone to prevent manipuation of referenced objects in methods.
        /// </summary>
        /// <typeparam name="T"> Object type to clone</typeparam>
        /// <param name="obj">Object to clone</param>
        /// <returns>Cloned object</returns>
        public static T DeepCopy<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;

                return (T)formatter.Deserialize(stream);
            }
        }

    }
}
