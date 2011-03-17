using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtensionMethods
{
    /// <summary>
    /// Extension Methods for NeuroRighter
    /// </summary>
    /// <author> Jon Newman</author>
    public static class NRExtensionMethods
    {

        // Method for getting index of max value of 
        // a generic collection
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
    }
}
