// Accord Statistics Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Statistics.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    ///   Class equalization filter.
    /// </summary>
    /// <remarks>
    ///   Currently this class does only work for a single
    ///   column and only for the binary case (two classes).
    /// </remarks>
    /// 
    [Serializable]
    public class Equalization : BaseFilter<Equalization.Options>
    {

        /// <summary>
        ///   Creates a new class equalization filter.
        /// </summary>
        /// 
        public Equalization()
        {
        }

        /// <summary>
        ///   Creates a new classes equalization filter.
        /// </summary>
        /// 
        public Equalization(string column)
        {
            Columns.Add(new Options(column));
        }

        /// <summary>
        ///   Processes the current filter.
        /// </summary>
        /// 
        protected override DataTable ProcessFilter(DataTable data)
        {
            // Currently works with only one column and for the binary case

            int[] classes = Columns[0].Classes;
            string column = Columns[0].ColumnName;

            // Get subsets with 0 and 1
            List<DataRow>[] subsets = new List<DataRow>[classes.Length];

            for (int i = 0; i < subsets.Length; i++)
            {
                subsets[i] = new List<DataRow>(data.Select("[" + column + "] = " + classes[i]));
            }

            while (subsets[0].Count != subsets[1].Count)
            {
                if (subsets[0].Count > subsets[1].Count)
                {
                    int diff = subsets[0].Count - subsets[1].Count;
                    for (int i = 0; i < diff && i < subsets[1].Count; i++)
                    {
                        subsets[1].Add(subsets[1][i]);
                    }
                }
                else
                {
                    int diff = subsets[1].Count - subsets[0].Count;
                    for (int i = 0; i < diff && i < subsets[0].Count; i++)
                    {
                        subsets[0].Add(subsets[0][i]);
                    }
                }
            }

            DataTable result = data.Clone();

            for (int i = 0; i < subsets.Length; i++)
            {
                foreach (DataRow row in subsets[i])
                    result.ImportRow(row);
            }

            return result;
        }

        /// <summary>
        ///   Options for the equalization filter.
        /// </summary>
        /// 
        [Serializable]
        public class Options : ColumnOptionsBase
        {
            /// <summary>
            ///   Gets or sets the labels used for each class contained in the column.
            /// </summary>
            /// 
            public int[] Classes { get; set; }

            /// <summary>
            ///   Constructs a new Options object for the given column.
            /// </summary>
            /// 
            /// <param name="name">
            ///   The name of the column to create this options for.
            /// </param>
            /// 
            public Options(String name)
                : base(name)
            {
            }

            /// <summary>
            ///   Constructs a new Options object.
            /// </summary>
            /// 
            public Options()
                : this("New column")
            {

            }
        }

    }
}
