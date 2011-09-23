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
    using Accord.Statistics.Analysis;

    /// <summary>
    ///   Principal component projection filter.
    /// </summary>
    /// 
    public class PrincipalComponentProjection : BaseFilter<PrincipalComponentProjection.Options>, IAutoConfigurableFilter
    {

        PrincipalComponentAnalysis pca;
        string[] componentNames;

        /// <summary>
        ///   Gets or sets the analysis associated with the filter.
        /// </summary>
        /// 
        public PrincipalComponentAnalysis Analysis
        {
            get { return pca; }
            set
            {
                pca = value;
                componentNames = generateNames(pca.Components.Count);
            }
        }

        /// <summary>
        ///   Creates a new Principal Component Projection filter.
        /// </summary>
        /// 
        public PrincipalComponentProjection()
        {

        }

        /// <summary>
        ///   Creates a new data normalization filter.
        /// </summary>
        /// 
        public PrincipalComponentProjection(params string[] columns)
        {
            foreach (String col in columns)
                Columns.Add(new Options(col));
        }


        /// <summary>
        ///   Processes the filter.
        /// </summary>
        /// 
        /// <param name="data">The data.</param>
        /// 
        protected override DataTable ProcessFilter(DataTable data)
        {
            // Step 1. Select columns and transform to double[,]
            int rows = data.Rows.Count;
            int cols = this.Columns.Count;

            double[,] inputMatrix = new double[rows, cols];

            // Scale each value from the original ranges to destination ranges
            for (int i = 0; i < Columns.Count; i++)
            {
                String columnName = Columns[i].ColumnName;

                DataColumn column = data.Columns[columnName];

                for (int j = 0; j < data.Rows.Count; j++)
                    inputMatrix[i, j] = (double)data.Rows[j][column];
            }

            double[,] outputMatrix = pca.Transform(inputMatrix);


            // Generate component column names
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Auto detects the filter options by analyzing a given <see cref="System.Data.DataTable"/>.
        /// </summary>
        /// 
        public void Detect(DataTable data)
        {
            throw new NotImplementedException();
        }

        private static string[] generateNames(int components)
        {
            String[] componentNames = new String[components];
            for (int i = 0; i < componentNames.Length; i++)
                componentNames[i] = "Principal Component 1";
            return componentNames;
        }

        /// <summary>
        ///   Options for normalizing a column.
        /// </summary>
        ///
        [Serializable]
        public class Options : ColumnOptionsBase
        {
            /// <summary>
            ///   Initializes a new instance of the <see cref="Options"/> class.
            /// </summary>
            /// 
            /// <param name="columnName">Name of the column.</param>
            /// 
            public Options(String columnName)
                : base(columnName)
            {
            }
        }



    }
}
