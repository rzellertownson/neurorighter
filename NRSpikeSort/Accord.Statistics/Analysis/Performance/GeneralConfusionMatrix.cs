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

namespace Accord.Statistics.Analysis
{
    using System;
    using Accord.Math;

    /// <summary>
    ///   General confusion matrix for 
    ///   multi-class decision problems.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       <a href="http://uwf.edu/zhu/evr6930/2.pdf">
    ///       R.  G.  Congalton. A Review  of Assessing  the Accuracy  of Classifications 
    ///       of Remotely  Sensed  Data. Available on: http://uwf.edu/zhu/evr6930/2.pdf </a></description></item>
    ///     <item><description>
    ///       <a href="http://www.iiasa.ac.at/Admin/PUB/Documents/IR-98-081.pdf">
    ///       G. Banko. A Review of Assessing the Accuracy of Classiﬁcations of Remotely Sensed Data and
    ///       of Methods Including Remote Sensing Data in Forest Inventory. Interim report. Available on:
    ///       http://www.iiasa.ac.at/Admin/PUB/Documents/IR-98-081.pdf </a></description></item>
    ///     </list></para>  
    /// </remarks>
    /// 
    public class GeneralConfusionMatrix
    {

        int[,] matrix;
        int samples;
        int classes;

        /// <summary>
        ///   Gets the confusion matrix, in which each element e_ij 
        ///   represents the number of elements from class i classified
        ///   as belonging to class j.
        /// </summary>
        /// 
        public int[,] Matrix
        {
            get { return matrix; }
        }

        /// <summary>
        ///   Gets the number of samples.
        /// </summary>
        /// 
        public int Samples
        {
            get { return samples; }
        }

        /// <summary>
        ///   Gets the number of classes.
        /// </summary>
        /// 
        public int Classes
        {
            get { return classes; }
        }

        /// <summary>
        ///   Creates a new Confusion Matrix.
        /// </summary>
        /// 
        public GeneralConfusionMatrix(int[,] matrix)
        {
            this.matrix = matrix;
            this.classes = matrix.GetLength(0);
            this.samples = matrix.Sum().Sum();
        }

        /// <summary>
        ///   Creates a new Confusion Matrix.
        /// </summary>
        /// 
        public GeneralConfusionMatrix(int classes, int[] expected, int[] predicted)
        {
            if (expected.Length != predicted.Length)
                throw new DimensionMismatchException("predicted",
                    "The number of expected and predicted observations must match.");

            this.samples = expected.Length;
            this.classes = classes;
            this.matrix = new int[classes, classes];

            // Each element ij represents the number of elements
            // from class i classified as belonging to class j.

            // For each classification,
            for (int k = 0; k < expected.Length; k++)
            {
                // Make sure the expected and predicted
                // values are from valid classes.

                int i = expected[k];
                int j = predicted[k];

                if (i < 0 || i >= classes)
                    throw new ArgumentOutOfRangeException("expected");

                if (j < 0 || j >= classes)
                    throw new ArgumentOutOfRangeException("predicted");


                matrix[i, j]++;
            }
        }

        /// <summary>
        ///   Gets the Kappa coefficient of performance.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///       CONGALTON, R.G.A., Review of assessing the accuracy of classifications of
        ///      remotely data, Remote sensing of the Environment, 37:35-46, 1991. </description></item>
        ///     </list></para>  
        /// </remarks>
        /// 
        /// 
        public double Kappa
        {
            get
            {
                int N = samples;
                int C = classes;

                int diagonalSum = 0;
                for (int i = 0; i < classes; i++)
                    diagonalSum += matrix[i, i];

                int directionSum = 0;
                for (int i = 0; i < classes; i++)
                {
                    int rowSum = 0;
                    int colSum = 0;

                    // Compute the sum of elements in row i
                    for (int j = 0; j < classes; j++)
                        rowSum += matrix[i, j];

                    // Compute the sum of elements in column i
                    for (int j = 0; j < classes; j++)
                        colSum += matrix[j, i];

                    directionSum += rowSum * colSum;
                }


                return (double)(N * diagonalSum - directionSum) / ((N * N) - directionSum);
            }
        }

        /// <summary>
        ///   Gets the Tau coefficient of performance.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///       MA, Z.; REDMOND, R. L. Tau coefficients for accuracy assessment of
        ///       classification of remote sensing data. </description></item>
        ///     </list></para>  
        /// </remarks>
        /// 
        public double Tau
        {
            get
            {
                int N = samples;

                int diagonalSum = 0;
                for (int i = 0; i < classes; i++)
                    diagonalSum += matrix[i, i];
                

                int directionSum = 0;
                for (int i = 0; i < classes; i++)
                {
                    // Compute the row sum for the class
                    int rowSum = 0;
                    for (int j = 0; j < classes; j++)
                        rowSum += matrix[i, j];

                    directionSum += matrix[i, i] * rowSum;
                }

                double p0 = (1.0 / N) * diagonalSum;
                double pr = (1.0 / (N * N)) * directionSum;

                return (double)(p0 - pr) / (1 - pr);
            }
        }
    }
}
