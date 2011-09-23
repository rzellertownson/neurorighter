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
    using System.ComponentModel;

    /// <summary>
    ///   Codification Filter class.
    /// </summary>
    /// <remarks>
    ///   The codification filter performs an integer codification of classes in
    ///   given in a string form. An unique integer identifier will be assigned
    ///   for each of the string classes.
    /// </remarks>
    /// 
    [Serializable]
    public class Codification : BaseFilter<Codification.Options>, IAutoConfigurableFilter
    {

        /// <summary>
        ///   Creates a new Codification Filter.
        /// </summary>
        /// 
        public Codification()
        {
        }

        /// <summary>
        ///   Processes the current filter.
        /// </summary>
        /// 
        protected override DataTable ProcessFilter(DataTable data)
        {
            // Copy only the schema (Clone)
            DataTable result = data.Clone();

            // For each column having a mapping
            foreach (Options options in Columns)
            {
                // Change its type from string to integer
                result.Columns[options.ColumnName].DataType = typeof(int);
            }


            // Now for each row on the original table
            foreach (DataRow inputRow in data.Rows)
            {
                // We'll import to the result table
                DataRow resultRow = result.NewRow();

                // For each column in original table
                foreach (DataColumn column in data.Columns)
                {
                    string name = column.ColumnName;

                    // If the column has a mapping
                    if (Columns.Contains(name))
                    {
                        var map = Columns[name].Mapping;

                        // Retrieve string value
                        string label = inputRow[name] as string;

                        // Get its corresponding integer
                        int value = map[label];

                        // Set the row to the integer
                        resultRow[name] = value;
                    }
                    else
                    {
                        // The column does not have a mapping
                        //  so we'll just copy the value over
                        resultRow[name] = inputRow[name];
                    }
                }

                // Finally, add the row into the result table
                result.Rows.Add(resultRow);
            }

            return result;
        }

        /// <summary>
        ///   Auto detects the filter options by analyzing a given <see cref="System.Data.DataTable"/>.
        /// </summary> 
        ///  
        public void Detect(DataTable data)
        {
            foreach (DataColumn column in data.Columns)
            {
                // If the column has string type
                if (column.DataType == typeof(String))
                {
                    // We'll create a mapping
                    string name = column.ColumnName;
                    var map = new Dictionary<string, int>();
                    Columns.Add(new Options(name, map));

                    // Do a select distinct to get distinct values
                    DataTable d = data.DefaultView.ToTable(true, name);

                    // For each distinct value, create a corresponding integer
                    for (int i = 0; i < d.Rows.Count; i++)
                    {
                        // And register the String->Integer mapping
                        map.Add(d.Rows[i][0] as string, i);
                    }
                }
            }
        }

        /// <summary>
        ///   Options for processing a column.
        /// </summary>
        /// 
        [Serializable]
        public class Options : ColumnOptionsBase
        {
            /// <summary>
            ///   Gets or sets the label mapping for translating
            ///   integer labels to the original string labels.
            /// </summary>
            public Dictionary<string, int> Mapping { get; private set; }

            /// <summary>
            ///   Constructs a new Options object for the given column.
            /// </summary>
            /// <param name="name">
            ///   The name of the column to create this options for.
            /// </param>
            public Options(String name)
                : base(name)
            {
                this.Mapping = new Dictionary<string, int>();
            }

            /// <summary>
            ///   Constructs a new Options object for the given column.
            /// </summary>
            /// <param name="name">
            ///   The name of the column to create this options for.
            /// </param>
            /// <param name="map">The initial mapping for this column.</param>
            public Options(String name, Dictionary<string, int> map)
                : base(name)
            {
                this.Mapping = map;
            }

            /// <summary>
            ///   Constructs a new Options object.
            /// </summary>
            public Options()
                : this("New column")
            {

            }
        }
    }
}
