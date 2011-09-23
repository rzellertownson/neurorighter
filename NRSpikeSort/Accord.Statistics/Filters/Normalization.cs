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
    using System.Data;
    using System.ComponentModel;

    /// <summary>
    ///   Data normalization preprocessing filter.
    /// </summary>
    /// 
    [Serializable]
    public class Normalization : BaseFilter<Normalization.Options>, IAutoConfigurableFilter
    {

        /// <summary>
        ///   Creates a new data normalization filter.
        /// </summary>
        public Normalization()
        {
        }

        /// <summary>
        ///   Creates a new data normalization filter.
        /// </summary>
        public Normalization(params string[] columns)
        {
            foreach (String col in columns)
                Columns.Add(new Options(col));
        }

        /// <summary>
        ///   Processes the current filter.
        /// </summary>
        protected override DataTable ProcessFilter(DataTable data)
        {
            DataTable result = data.Copy();

            // Scale each value from the original ranges to destination ranges
            foreach (Options column in this.Columns)
            {
                string name = column.ColumnName;

                foreach (DataRow row in result.Rows)
                {
                    double value = (double)row[name];

                    // Center
                    value -= column.Mean;

                    if (column.Standardize)
                    {
                        // Normalize
                        value /= column.StandardDeviation;
                    }

                    row[name] = value;
                }
            }

            return result;
        }

        /// <summary>
        ///   Auto detects the filter options by analyzing a given <see cref="System.Data.DataTable"/>.
        /// </summary>     
        /// 
        public void Detect(DataTable data)
        {
            // For each column
            foreach (DataColumn column in data.Columns)
            {
                if (column.DataType == typeof(Double) || column.DataType == typeof(Decimal))
                {
                    string name = column.ColumnName;
                    double sdev = (double)data.Compute("STDEV(" + name + ")", String.Empty);
                    double mean = (double)data.Compute("AVG(" + name + ")", String.Empty);

                    if (!Columns.Contains(name))
                        Columns.Add(new Options(name));

                    Columns[name].Mean = mean;
                    Columns[name].StandardDeviation = sdev;
                }
            }
        }


        /// <summary>
        ///   Options for normalizing a column.
        /// </summary>
        ///
        [Serializable]
        public class Options : ColumnOptionsBase
        {
            /// <summary>
            ///   Gets or sets the mean of the data contained in the column.
            /// </summary>
            public double Mean { get; set; }

            /// <summary>
            ///   Gets or sets the standard deviation of the data contained in the column.
            /// </summary>
            public double StandardDeviation { get; set; }

            /// <summary>
            ///   Gets or sets if the column's data should be standardized to Z-Scores.
            /// </summary>
            public bool Standardize { get; set; }

            /// <summary>
            ///   Constructs a new Options object.
            /// </summary>
            public Options()
                : this("New column")
            {

            }

            /// <summary>
            ///   Constructs a new Options object for the given column.
            /// </summary>
            /// 
            /// <param name="name">
            ///   The name of the column to create this options for.
            /// </param>
            /// 
            public Options(String name)
                : this(name, 0, 1)
            {
            }

            /// <summary>
            ///   Constructs a new Options object for the given column.
            /// </summary>
            /// 
            /// <param name="name">
            ///   The name of the column to create this options for.
            /// </param>
            /// 
            /// <param name="mean">The mean value for normalization.</param>
            /// <param name="standardDeviation">The standard deviation value for standardization.</param>
            /// 
            public Options(String name, double mean, double standardDeviation)
                : base(name)
            {
                this.Mean = mean;
                this.StandardDeviation = standardDeviation;
                this.Standardize = true;
            }

        }

    }
}
