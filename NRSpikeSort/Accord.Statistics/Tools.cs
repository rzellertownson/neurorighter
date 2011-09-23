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

namespace Accord.Statistics
{
    using System;
    using System.Collections.Generic;
    using Accord.Math;
    using Accord.Math.Decompositions;


    /// <summary>
    ///   Set of statistics functions.
    /// </summary>
    /// 
    /// <remarks>
    ///   This class represents collection of common functions used in statistics.
    ///   Every Matrix function assumes data is organized in a table-like model,
    ///   where Columns represents variables and Rows represents a observation of
    ///   each variable.
    /// </remarks>
    /// 
    public static class Tools
    {


        #region Arrays Measures

        /// <summary>
        ///   Computes the Mean of the given values.
        /// </summary>
        /// <param name="values">A double array containing the vector members.</param>
        /// <returns>The mean of the given data.</returns>
        public static double Mean(this double[] values)
        {
            double sum = 0.0;

            for (int i = 0; i < values.Length; i++)
                sum += values[i];

            return sum / values.Length;
        }

        /// <summary>
        ///   Computes the Mean of the given values.
        /// </summary>
        /// <param name="values">A float array containing the vector members.</param>
        /// <returns>The mean of the given data.</returns>
        public static float Mean(this float[] values)
        {
            float sum = 0f;

            for (int i = 0; i < values.Length; i++)
                sum += values[i];

            return sum / values.Length;
        }

        /// <summary>
        ///   Computes the Standard Deviation of the given values.
        /// </summary>
        /// <param name="values">A double array containing the vector members.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(this double[] values)
        {
            return StandardDeviation(values, Mean(values));
        }

        /// <summary>
        ///   Computes the Standard Deviation of the given values.
        /// </summary>
        /// <param name="values">A double array containing the vector members.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(this float[] values)
        {
            return StandardDeviation(values, Mean(values));
        }

        /// <summary>
        ///   Computes the Standard Deviation of the given values.
        /// </summary>
        /// <param name="values">A double array containing the vector members.</param>
        /// <param name="mean">The mean of the vector, if already known.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static double StandardDeviation(this double[] values, double mean)
        {
            return System.Math.Sqrt(Variance(values, mean));
        }

        /// <summary>
        ///   Computes the Standard Deviation of the given values.
        /// </summary>
        /// <param name="values">A float array containing the vector members.</param>
        /// <param name="mean">The mean of the vector, if already known.</param>
        /// <returns>The standard deviation of the given data.</returns>
        public static float StandardDeviation(this float[] values, float mean)
        {
            return (float)System.Math.Sqrt(Variance(values, mean));
        }

        /// <summary>
        ///   Computes the Standard Error for a sample size, which estimates the
        ///   standard deviation of the sample mean based on the population mean.
        /// </summary>
        /// <param name="samples">The sample size.</param>
        /// <param name="standardDeviation">The sample standard deviation.</param>
        /// <returns>The standard error for the sample.</returns>
        public static double StandardError(int samples, double standardDeviation)
        {
            return standardDeviation / System.Math.Sqrt(samples);
        }

        /// <summary>
        ///   Computes the Standard Error for a sample size, which estimates the
        ///   standard deviation of the sample mean based on the population mean.
        /// </summary>
        /// <param name="values">A double array containing the samples.</param>
        /// <returns>The standard error for the sample.</returns>
        public static double StandardError(double[] values)
        {
            return StandardError(values.Length, StandardDeviation(values));
        }

        /// <summary>
        ///   Computes the Median of the given values.
        /// </summary>
        /// <param name="values">A double array containing the vector members.</param>
        /// <returns>The median of the given data.</returns>
        public static double Median(double[] values)
        {
            return Median(values, false);
        }

        /// <summary>
        ///   Computes the Median of the given values.
        /// </summary>
        /// 
        /// <param name="values">An integer array containing the vector members.</param>
        /// <param name="alreadySorted">A boolean parameter informing if the given values have already been sorted.</param>
        /// <returns>The median of the given data.</returns>
        /// 
        public static double Median(double[] values, bool alreadySorted)
        {
            double[] data = new double[values.Length];
            values.CopyTo(data, 0); // Creates a copy of the given values,

            if (!alreadySorted) // So we can sort it without modifying the original array.
                Array.Sort(data);

            int N = data.Length;

            if (N % 2 == 0)
                return (data[N / 2] + data[(N / 2) - 1]) * 0.5; // N is even 
            else return data[N / 2];                            // N is odd
        }

        /// <summary>
        ///   Computes the Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A double precision number array containing the vector members.</param>
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double Variance(double[] values)
        {
            return Variance(values, Mean(values));
        }

        /// <summary>
        ///   Computes the Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A double precision number array containing the vector members.</param>
        /// <param name="unbiased">
        ///   True to compute the unbiased estimate of the population
        ///   variance, false for the sample variance.</param>
        ///
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double Variance(double[] values, bool unbiased)
        {
            return Variance(values, Mean(values), unbiased);
        }

        /// <summary>
        ///   Computes the Variance of the given values.
        /// </summary>
        /// <param name="values">A single precision number array containing the vector members.</param>
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double Variance(float[] values)
        {
            return Variance(values, Mean(values));
        }

        /// <summary>
        ///   Computes the Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A number array containing the vector members.</param>
        /// <param name="mean">The mean of the array, if already known.</param>
        ///   
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double Variance(double[] values, double mean)
        {
            return Variance(values, mean, true);
        }

        /// <summary>
        ///   Computes the Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A number array containing the vector members.</param>
        /// <param name="mean">The mean of the array, if already known.</param>
        /// <param name="unbiased">
        ///   True to compute the unbiased estimate of the population
        ///   variance, false for the sample variance.</param>
        ///   
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double Variance(double[] values, double mean, bool unbiased)
        {
            double variance = 0.0;

            for (int i = 0; i < values.Length; i++)
            {
                double x = values[i] - mean;
                variance += x * x;
            }

            if (unbiased)
            {
                // Sample variance
                return variance / (values.Length - 1);
            }
            else
            {
                // Population variance
                return variance / values.Length;
            }
        }

        /// <summary>
        ///   Computes the Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A number array containing the vector members.</param>
        /// <param name="mean">The mean of the array, if already known.</param>
        /// <returns>The variance of the given data.</returns>
        /// 
        public static float Variance(float[] values, float mean)
        {
            float variance = 0f;

            for (int i = 0; i < values.Length; i++)
            {
                float x = values[i] - mean;
                variance += x * x;
            }

            // Sample variance
            return variance / (values.Length - 1);
        }

        /// <summary>
        ///   Computes the pooled standard deviation of the given values.
        /// </summary>
        /// 
        /// <param name="samples">The grouped samples.</param>
        /// 
        public static double PooledStandardDeviation(params double[][] samples)
        {
            return Math.Sqrt(PooledVariance(samples));
        }

        /// <summary>
        ///   Computes the pooled variance of the given values.
        /// </summary>
        /// 
        /// <param name="samples">The grouped samples.</param>
        /// 
        public static double PooledVariance(params double[][] samples)
        {
            double sum = 0;
            int length = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                double[] values = samples[i];
                double var = Variance(values);

                sum += (values.Length - 1) * var;
                length += (values.Length - 1);
            }

            return sum / length;
        }

        /// <summary>
        ///   Computes the Mode of the given values.
        /// </summary>
        /// 
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double Mode(double[] values)
        {
            int[] itemCount = new int[values.Length];
            double[] itemArray = new double[values.Length];
            int count = 0;

            for (int i = 0; i < values.Length; i++)
            {
                int index = Array.IndexOf<double>(itemArray, values[i], 0, count);

                if (index >= 0)
                {
                    itemCount[index]++;
                }
                else
                {
                    itemArray[count] = values[i];
                    itemCount[count] = 1;
                    count++;
                }
            }

            int maxValue = 0;
            int maxIndex = 0;

            for (int i = 0; i < count; i++)
            {
                if (itemCount[i] > maxValue)
                {
                    maxValue = itemCount[i];
                    maxIndex = i;
                }
            }

            return itemArray[maxIndex];
        }

        /// <summary>
        ///   Computes the Covariance between two values arrays.
        /// </summary>
        /// 
        /// <param name="vector1">A number array containing the first vector elements.</param>
        /// <param name="vector2">A number array containing the second vector elements.</param>
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double[,] Covariance(double[] vector1, double[] vector2)
        {
            double[][] vectors = new double[][] { vector1, vector2 };
            return Scatter(vectors, Mean(vectors, 1), vectors.Length, 1);
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// 
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// 
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The skewness of the given data.</returns>
        /// 
        public static double Skewness(double[] values)
        {
            double mean = Mean(values);
            return Skewness(values, mean, StandardDeviation(values, mean));
        }

        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// 
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// 
        /// <param name="values">A number array containing the vector values.</param>
        /// <param name="mean">The values' mean, if already known.</param>
        /// <param name="standardDeviation">The values' standard deviations, if already known.</param>
        /// 
        /// <returns>The skewness of the given data.</returns>
        /// 
        public static double Skewness(double[] values, double mean, double standardDeviation)
        {
            int n = values.Length;
            double sum = 0.0;
            for (int i = 0; i < n; i++)
            {
                // Sum of third moment deviations
                sum += System.Math.Pow(values[i] - mean, 3);
            }

            return sum / ((double)n * System.Math.Pow(standardDeviation, 3));
        }

        /// <summary>
        ///   Computes the Kurtosis for the given values.
        /// </summary>
        /// <param name="values">A number array containing the vector values.</param>
        /// <returns>The kurtosis of the given data.</returns>
        public static double Kurtosis(double[] values)
        {
            double mean = Mean(values);
            return Kurtosis(values, mean, StandardDeviation(values, mean));
        }

        /// <summary>
        ///   Computes the Kurtosis for the given values.
        /// </summary>
        /// <param name="values">A number array containing the vector values.</param>
        /// <param name="mean">The values' mean, if already known.</param>
        /// <param name="standardDeviation">The values' variance, if already known.</param>
        /// <returns>The kurtosis of the given data.</returns>
        public static double Kurtosis(double[] values, double mean, double standardDeviation)
        {
            int n = values.Length;

            double sum = 0.0, deviation;
            for (int i = 0; i < n; i++)
            {
                // Sum of fourth moment deviations
                deviation = (values[i] - mean);
                sum += System.Math.Pow(deviation, 4);
            }

            return sum / ((double)n * System.Math.Pow(standardDeviation, 4)) - 3.0;
        }

        #endregion

        #region Weighted Array Measures

        /// <summary>
        ///   Computes the Weighted Mean of the given values.
        /// </summary>
        /// 
        /// <param name="values">A double array containing the vector members.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// 
        /// <returns>The mean of the given data.</returns>
        /// 
        public static double WeightedMean(this double[] values, double[] weights)
        {
            double sum = 0.0;

            for (int i = 0; i < values.Length; i++)
                sum += values[i] * weights[i];

            /*
                // For non-unit weights
                double w = 0.0;
                for (int i = 0; i < weights.Length; i++)
                    w += weights[i];
                return sum / w;
            */

            return sum;
        }

        /// <summary>
        ///   Computes the Standard Deviation of the given values.
        /// </summary>
        /// 
        /// <param name="values">A double array containing the vector members.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// 
        /// <returns>The standard deviation of the given data.</returns>
        /// 
        public static double WeightedStandardDeviation(this double[] values, double[] weights)
        {
            return Math.Sqrt(WeightedVariance(values, weights));
        }

        /// <summary>
        ///   Computes the Standard Deviation of the given values.
        /// </summary>
        /// 
        /// <param name="values">A double array containing the vector members.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// <param name="mean">The mean of the vector, if already known.</param>
        /// 
        /// <returns>The standard deviation of the given data.</returns>
        /// 
        public static double WeightedStandardDeviation(this double[] values, double[] weights, double mean)
        {
            return Math.Sqrt(WeightedVariance(values, weights, mean));
        }

        /// <summary>
        ///   Computes the weighted Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A number array containing the vector members.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// 
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double WeightedVariance(double[] values, double[] weights)
        {
            return WeightedVariance(values, weights, WeightedMean(values, weights));
        }

        /// <summary>
        ///   Computes the weighted Variance of the given values.
        /// </summary>
        /// 
        /// <param name="values">A number array containing the vector members.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// <param name="mean">The mean of the array, if already known.</param>
        /// 
        /// <returns>The variance of the given data.</returns>
        /// 
        public static double WeightedVariance(double[] values, double[] weights, double mean)
        {
            // http://en.wikipedia.org/wiki/Weighted_variance#Weighted_sample_variance
            // http://www.gnu.org/software/gsl/manual/html_node/Weighted-Samples.html

            double variance = 0.0;
            double a = 0.0, b = 0.0;

            for (int i = 0; i < values.Length; i++)
            {
                double z = values[i] - mean;
                double w = weights[i];

                variance += w * (z * z);

                b += w;
                a += w * w;
            }

            return variance * (b / (b * b - a));
        }
        #endregion


        // ------------------------------------------------------------



        #region Matrix Measures


        /// <summary>
        ///   Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <returns>Returns a row vector containing the column means of the given matrix.</returns>
        /// <example>
        ///   <code>
        ///   double[,] matrix = 
        ///   {
        ///      { 2, -1.0, 5 },
        ///      { 7,  0.5, 9 },
        ///   };
        ///    
        ///   // column means are equal to (4.5, -0.25, 7.0)
        ///   double[] means = Accord.Statistics.Tools.Mean(matrix);
        ///   </code>
        /// </example>
        public static double[] Mean(double[,] matrix)
        {
            return Mean(matrix, 0);
        }

        /// <summary>
        ///   Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="dimension">
        ///   The dimension along which the means will be calculated. Pass
        ///   0 to compute a row vector containing the mean of each column,
        ///   or 1 to compute a column vector containing the mean of each row.
        ///   Default value is 0.
        /// </param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        /// <example>
        ///   <code>
        ///   double[,] matrix = 
        ///   {
        ///      { 2, -1.0, 5 },
        ///      { 7,  0.5, 9 },
        ///   };
        ///   
        ///   // column means are equal to (4.5, -0.25, 7.0)
        ///   double[] colMeans = Accord.Statistics.Tools.Mean(matrix, 0);
        ///     
        ///   // row means are equal to (2.0, 5.5)
        ///   double[] rowMeans = Accord.Statistics.Tools.Mean(matrix, 1);
        ///   </code>
        /// </example>
        public static double[] Mean(double[,] matrix, int dimension)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[] mean;

            if (dimension == 0)
            {
                mean = new double[cols];
                double N = rows;

                // for each column
                for (int j = 0; j < cols; j++)
                {
                    // for each row
                    for (int i = 0; i < rows; i++)
                        mean[j] += matrix[i, j];

                    mean[j] /= N;
                }
            }
            else if (dimension == 1)
            {
                mean = new double[rows];
                double N = cols;

                // for each row
                for (int j = 0; j < rows; j++)
                {
                    // for each column
                    for (int i = 0; i < cols; i++)
                        mean[j] += matrix[j, i];

                    mean[j] /= N;
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return mean;
        }

        /// <summary>
        ///   Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <returns>Returns a row vector containing the column means of the given matrix.</returns>
        /// <example>
        ///   <code>
        ///   double[][] matrix = 
        ///   {
        ///       new double[] { 2, -1.0, 5 },
        ///       new double[] { 7,  0.5, 9 },
        ///   };
        ///    
        ///   // column means are equal to (4.5, -0.25, 7.0)
        ///   double[] means = Accord.Statistics.Tools.Mean(matrix);
        ///   </code>
        /// </example>
        public static double[] Mean(double[][] matrix)
        {
            return Mean(matrix, 0);
        }

        /// <summary>
        ///   Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="dimension">
        ///   The dimension along which the means will be calculated. Pass
        ///   0 to compute a row vector containing the mean of each column,
        ///   or 1 to compute a column vector containing the mean of each row.
        ///   Default value is 0.
        /// </param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        /// <example>
        ///   <code>
        ///   double[][] matrix = 
        ///   {
        ///       new double[] { 2, -1.0, 5 },
        ///       new double[] { 7,  0.5, 9 },
        ///   };
        ///   
        ///   // column means are equal to (4.5, -0.25, 7.0)
        ///   double[] colMeans = Accord.Statistics.Tools.Mean(matrix, 0);
        ///     
        ///   // row means are equal to (2.0, 5.5)
        ///   double[] rowMeans = Accord.Statistics.Tools.Mean(matrix, 1);
        ///   </code>
        /// </example>
        public static double[] Mean(double[][] matrix, int dimension)
        {
            int rows = matrix.Length;
            if (rows == 0) return new double[0];
            int cols = matrix[0].Length;
            double[] mean;

            if (dimension == 0)
            {
                mean = new double[cols];
                double N = rows;

                // for each column
                for (int j = 0; j < cols; j++)
                {
                    // for each row
                    for (int i = 0; i < rows; i++)
                        mean[j] += matrix[i][j];

                    mean[j] = mean[j] / N;
                }
            }
            else if (dimension == 1)
            {
                mean = new double[rows];
                double N = cols;

                // for each row
                for (int j = 0; j < rows; j++)
                {
                    // for each column
                    for (int i = 0; i < cols; i++)
                        mean[j] += matrix[j][i];

                    mean[j] = mean[j] / N;
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return mean;
        }

        /// <summary>
        ///   Calculates the weighted matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="weights">A vector containing the importance of each sample in the matrix.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        /// 
        public static double[] Mean(double[][] matrix, double[] weights)
        {
            return WeightedMean(matrix, weights, 0);
        }

        /// <summary>
        ///   Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="sums">The sum vector containing already calculated sums for each column of the matix.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(double[,] matrix, double[] sums)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[] mean = new double[cols];
            double N = rows;

            for (int j = 0; j < cols; j++)
                mean[j] = sums[j] / N;

            return mean;
        }


        /// <summary>
        ///   Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(double[,] matrix)
        {
            return StandardDeviation(matrix, Mean(matrix));
        }

        /// <summary>
        ///   Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[,] matrix, double[] means)
        {
            return Matrix.Sqrt(Variance(matrix, means));
        }

        /// <summary>
        ///   Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[][] matrix, double[] means)
        {
            return Matrix.Sqrt(Variance(matrix, means));
        }

        /// <summary>
        ///   Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[][] matrix)
        {
            return StandardDeviation(matrix, Mean(matrix));
        }


        /// <summary>
        ///   Centers an observation, subtracting the empirical mean from the variable.
        /// </summary>
        public static void Center(double[] observation)
        {
            Center(observation, Mean(observation));
        }

        /// <summary>
        ///   Centers an observation, subtracting the empirical mean from the variable.
        /// </summary>
        public static void Center(double[] observation, double mean)
        {
            for (int i = 0; i < observation.Length; i++)
                observation[i] -= mean;
        }


        /// <summary>
        ///   Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variancees will be calculated.</param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[,] matrix)
        {
            return Variance(matrix, Mean(matrix));
        }

        /// <summary>
        ///   Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[,] matrix, double[] means)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double N = rows;

            double[] variance = new double[cols];

            // for each column (for each variable)
            for (int j = 0; j < cols; j++)
            {
                double sum1 = 0.0;
                double sum2 = 0.0;
                double x = 0.0;

                // for each row (observation of the variable)
                for (int i = 0; i < rows; i++)
                {
                    x = matrix[i, j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                // calculate the variance
                variance[j] = (sum2 - ((sum1 * sum1) / N)) / (N - 1);
            }

            return variance;
        }

        /// <summary>
        ///   Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[][] matrix)
        {
            return Variance(matrix, Mean(matrix));
        }

        /// <summary>
        ///   Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matix.</param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[][] matrix, double[] means)
        {
            int rows = matrix.Length;
            if (rows == 0) return new double[0];
            int cols = matrix[0].Length;
            double N = rows;

            double[] variance = new double[cols];

            // for each column (for each variable)
            for (int j = 0; j < cols; j++)
            {
                double sum1 = 0.0;
                double sum2 = 0.0;
                double x = 0.0;

                // for each row (observation of the variable)
                for (int i = 0; i < rows; i++)
                {
                    x = matrix[i][j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                // calculate the variance
                variance[j] = (sum2 - ((sum1 * sum1) / N)) / (N - 1);
            }

            return variance;
        }


        /// <summary>
        ///   Calculates the matrix Medians vector.
        /// </summary>
        /// <param name="matrix">A matrix whose medians will be calculated.</param>
        /// <returns>Returns a vector containing the medians of the given matrix.</returns>
        public static double[] Median(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[] medians = new double[cols];

            for (int i = 0; i < cols; i++)
            {
                double[] data = new double[rows];

                // Creates a copy of the given values
                for (int j = 0; j < rows; j++)
                    data[j] = matrix[j, i];

                Array.Sort(data); // Sort it

                int N = data.Length;

                if (N % 2 == 0)
                    medians[i] = (data[N / 2] + data[(N / 2) - 1]) * 0.5; // N is even 
                else medians[i] = data[N / 2];                      // N is odd
            }

            return medians;
        }

        /// <summary>
        ///   Calculates the matrix Medians vector.
        /// </summary>
        /// <param name="matrix">A matrix whose medians will be calculated.</param>
        /// <returns>Returns a vector containing the medians of the given matrix.</returns>
        public static double[] Median(double[][] matrix)
        {
            if (matrix == null) throw new ArgumentNullException("matrix");

            int rows = matrix.Length;
            int cols = matrix[0].Length;
            double[] medians = new double[cols];

            for (int i = 0; i < cols; i++)
            {
                double[] data = new double[rows];

                // Creates a copy of the given values
                for (int j = 0; j < rows; j++)
                    data[j] = matrix[j][i];

                Array.Sort(data); // Sort it

                int N = data.Length;

                if (N % 2 == 0)
                    medians[i] = (data[N / 2] + data[(N / 2) - 1]) * 0.5; // N is even 
                else medians[i] = data[N / 2];                      // N is odd
            }

            return medians;
        }


        /// <summary>
        ///   Calculates the matrix Modes vector.
        /// </summary>
        /// <param name="matrix">A matrix whose modes will be calculated.</param>
        /// <returns>Returns a vector containing the modes of the given matrix.</returns>
        public static double[] Mode(this double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[] mode = new double[cols];

            for (int i = 0; i < cols; i++)
            {
                int[] itemCount = new int[rows];
                double[] itemArray = new double[rows];
                int count = 0;

                // for each row
                for (int j = 0; j < rows; j++)
                {
                    int index = Array.IndexOf<double>(itemArray, matrix[j, i], 0, count);

                    if (index >= 0)
                    {
                        itemCount[index]++;
                    }
                    else
                    {
                        itemArray[count] = matrix[j, i];
                        itemCount[count] = 1;
                        count++;
                    }
                }

                int maxValue = 0;
                int maxIndex = 0;

                for (int j = 0; j < count; j++)
                {
                    if (itemCount[j] > maxValue)
                    {
                        maxValue = itemCount[j];
                        maxIndex = j;
                    }
                }

                mode[i] = itemArray[maxIndex];
            }

            return mode;
        }

        /// <summary>
        ///   Calculates the matrix Modes vector.
        /// </summary>
        /// <param name="matrix">A matrix whose modes will be calculated.</param>
        /// <returns>Returns a vector containing the modes of the given matrix.</returns>
        public static double[] Mode(this double[][] matrix)
        {
            int rows = matrix.Length;
            int cols = matrix[0].Length;

            double[] mode = new double[cols];

            for (int i = 0; i < cols; i++)
            {
                int[] itemCount = new int[rows];
                double[] itemArray = new double[rows];
                int count = 0;

                // for each row
                for (int j = 0; j < matrix.Length; j++)
                {
                    int index = Array.IndexOf<double>(itemArray, matrix[j][i], 0, count);

                    if (index >= 0)
                    {
                        itemCount[index]++;
                    }
                    else
                    {
                        itemArray[count] = matrix[j][i];
                        itemCount[count] = 1;
                        count++;
                    }
                }

                int maxValue = 0;
                int maxIndex = 0;

                for (int j = 0; j < count; j++)
                {
                    if (itemCount[j] > maxValue)
                    {
                        maxValue = itemCount[j];
                        maxIndex = j;
                    }
                }

                mode[i] = itemArray[maxIndex];
            }

            return mode;
        }


        /// <summary>
        ///   Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number matrix containing the matrix values.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Skewness(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Computes the Skewness vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number array containing the vector values.</param>
        /// <param name="means">The values' mean, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviations, if already known.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[,] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.GetLength(0);
            double[] skewness = new double[matrix.GetLength(1)];
            for (int j = 0; j < skewness.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    // Sum of third moment deviations
                    sum += Math.Pow(matrix[i, j] - means[j], 3);
                }

                skewness[j] = sum / ((n - 1) * Math.Pow(standardDeviations[j], 3));
            }

            return skewness;
        }

        /// <summary>
        ///   Computes the Skewness vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///   Skewness characterizes the degree of asymmetry of a distribution
        ///   around its mean. Positive skewness indicates a distribution with
        ///   an asymmetric tail extending towards more positive values. Negative
        ///   skewness indicates a distribution with an asymmetric tail extending
        ///   towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number array containing the vector values.</param>
        /// <param name="means">The values' mean, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviations, if already known.</param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(double[][] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.Length;
            double[] skewness = new double[means.Length];

            for (int j = 0; j < means.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < matrix.Length; i++)
                {
                    // Sum of third moment deviations
                    sum += Math.Pow(matrix[i][j] - means[j], 3);
                }

                skewness[j] = sum / ((n - 1) * Math.Pow(standardDeviations[j], 3));
            }

            return skewness;
        }


        /// <summary>
        ///   Computes the Kurtosis vector for the given matrix.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Kurtosis(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Computes the Kurtosis vector for the given matrix.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>The kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(double[,] matrix, double[] means, double[] standardDeviations)
        {
            int n = matrix.GetLength(0);
            double[] kurtosis = new double[matrix.GetLength(1)];
            for (int j = 0; j < kurtosis.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    // Sum of fourth moment deviations
                    sum += Math.Pow(matrix[i, j] - means[j], 4);
                }

                kurtosis[j] = sum / (n * System.Math.Pow(standardDeviations[j], 4)) - 3.0;
            }

            return kurtosis;
        }

        /// <summary>
        ///   Computes the Kurtosis vector for the given matrix.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>The kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(double[][] matrix, double[] means, double[] standardDeviations)
        {
            int rows = matrix.Length;

            double[] kurtosis = new double[means.Length];
            for (int j = 0; j < means.Length; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < matrix.Length; i++)
                {
                    // Sum of fourth moment deviations
                    sum += Math.Pow(matrix[i][j] - means[j], 4);
                }

                kurtosis[j] = sum / (rows * Math.Pow(standardDeviations[j], 4)) - 3.0;
            }

            return kurtosis;
        }


        /// <summary>
        ///   Computes the Standard Error vector for a given matrix.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>Returns the standard error vector for the matrix.</returns>
        public static double[] StandardError(double[,] matrix)
        {
            return StandardError(matrix.GetLength(0), StandardDeviation(matrix));
        }

        /// <summary>
        ///   Computes the Standard Error vector for a given matrix.
        /// </summary>
        /// <param name="samples">The number of samples in the matrix.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>Returns the standard error vector for the matrix.</returns>
        public static double[] StandardError(int samples, double[] standardDeviations)
        {
            double[] standardErrors = new double[standardDeviations.Length];
            double sqrt = System.Math.Sqrt(samples);
            for (int i = 0; i < standardDeviations.Length; i++)
            {
                standardErrors[i] = standardDeviations[i] / sqrt;
            }
            return standardErrors;
        }


        /// <summary>
        ///   Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix)
        {
            return Covariance(matrix, Mean(matrix));
        }

        /// <summary>
        ///   Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="dimension">
        ///   The dimension of the matrix to consider as observations. Pass 0 if the matrix has
        ///   observations as rows and variables as columns, pass 1 otherwise. Default is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix, int dimension)
        {
            return Scatter(matrix, Mean(matrix, dimension), matrix.GetLength(dimension) - 1, dimension);
        }

        /// <summary>
        ///   Calculates the covariance matrix of a sample matrix.
        /// </summary>        
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix, double[] means)
        {
            return Scatter(matrix, means, matrix.GetLength(0) - 1, 0);
        }


        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[,] matrix, double[] means)
        {
            return Scatter(matrix, means, 1.0, 0);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[,] matrix, double[] means, double divisor)
        {
            return Scatter(matrix, means, divisor, 0);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="dimension">
        ///   Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[,] matrix, double[] means, int dimension)
        {
            return Scatter(matrix, means, 1.0, dimension);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <param name="dimension">
        ///   Pass 0 if the mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[,] matrix, double[] means, double divisor, int dimension)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] cov;

            if (dimension == 0)
            {
                if (means.Length != cols) throw new ArgumentException(
                    "Length of the mean vector should equal the number of columns", "means");

                cov = new double[cols, cols];
                for (int i = 0; i < cols; i++)
                {
                    for (int j = i; j < cols; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < rows; k++)
                            s += (matrix[k, j] - means[j]) * (matrix[k, i] - means[i]);
                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else if (dimension == 1)
            {
                if (means.Length != rows) throw new ArgumentException(
                    "Length of the mean vector should equal the number of rows", "means");

                cov = new double[rows, rows];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = i; j < rows; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < cols; k++)
                            s += (matrix[j, k] - means[j]) * (matrix[i, k] - means[i]);
                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return cov;
        }

        /// <summary>
        ///   Calculates the covariance matrix of a sample matrix.
        /// </summary> 
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[][] matrix)
        {
            return Covariance(matrix, Mean(matrix));
        }

        /// <summary>
        ///   Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="dimension">
        ///   The dimension of the matrix to consider as observations. Pass 0 if the matrix has
        ///   observations as rows and variables as columns, pass 1 otherwise. Default is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[][] matrix, int dimension)
        {
            int size = (dimension == 0) ? matrix.Length : matrix[0].Length;
            return Scatter(matrix, Mean(matrix, dimension), size - 1, dimension);
        }

        /// <summary>
        ///   Calculates the covariance matrix of a sample matrix.
        /// </summary> 
        /// <remarks>
        ///   In statistics and probability theory, the covariance matrix is a matrix of
        ///   covariances between elements of a vector. It is the natural generalization
        ///   to higher dimensions of the concept of the variance of a scalar-valued
        ///   random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[][] matrix, double[] means)
        {
            return Scatter(matrix, means, matrix.Length - 1, 0);
        }


        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[][] matrix, double[] means)
        {
            return Scatter(matrix, means, 1.0, 0);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[][] matrix, double[] means, double divisor)
        {
            return Scatter(matrix, means, divisor, 0);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="dimension">
        ///   Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[][] matrix, double[] means, int dimension)
        {
            return Scatter(matrix, means, 1.0, dimension);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <param name="dimension">
        ///   Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(double[][] matrix, double[] means, double divisor, int dimension)
        {
            int rows = matrix.Length;
            if (rows == 0) return new double[0, 0];
            int cols = matrix[0].Length;

            double[,] cov;

            if (dimension == 0)
            {
                if (means.Length != cols) throw new ArgumentException(
                    "Length of the mean vector should equal the number of columns", "means");

                cov = new double[cols, cols];
                for (int i = 0; i < cols; i++)
                {
                    for (int j = i; j < cols; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < rows; k++)
                            s += (matrix[k][j] - means[j]) * (matrix[k][i] - means[i]);
                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else if (dimension == 1)
            {
                if (means.Length != rows) throw new ArgumentException(
                    "Length of the mean vector should equal the number of rows", "means");

                cov = new double[rows, rows];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = i; j < rows; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < cols; k++)
                            s += (matrix[j][k] - means[j]) * (matrix[i][k] - means[i]);
                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return cov;
        }


        /// <summary>
        ///   Calculates the correlation matrix for a matrix of samples.
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the correlation matrix is the same
        ///   as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <param name="matrix">A multi-dimensional array containing the matrix values.</param>
        /// <returns>The correlation matrix.</returns>
        public static double[,] Correlation(double[,] matrix)
        {
            double[] means = Mean(matrix);
            return Correlation(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///   Calculates the correlation matrix for a matrix of samples.
        /// </summary>
        /// <remarks>
        ///   In statistics and probability theory, the correlation matrix is the same
        ///   as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <param name="matrix">A multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>The correlation matrix.</returns>
        public static double[,] Correlation(double[,] matrix, double[] means, double[] standardDeviations)
        {
            double[,] scores = ZScores(matrix, means, standardDeviations);

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double N = rows;
            double[,] cor = new double[cols, cols];
            for (int i = 0; i < cols; i++)
            {
                for (int j = i; j < cols; j++)
                {
                    double c = 0.0;
                    for (int k = 0; k < rows; k++)
                        c += scores[k, j] * scores[k, i];
                    c /= N - 1.0;
                    cor[i, j] = c;
                    cor[j, i] = c;
                }
            }

            return cor;
        }


        /// <summary>
        ///   Generates the Standard Scores, also known as Z-Scores, from the given data.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The Z-Scores for the matrix.</returns>
        public static double[,] ZScores(double[,] matrix)
        {
            double[] mean = Mean(matrix);
            return ZScores(matrix, mean, StandardDeviation(matrix, mean));
        }

        /// <summary>
        ///   Generates the Standard Scores, also known as Z-Scores, from the given data.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>The Z-Scores for the matrix.</returns>
        public static double[,] ZScores(double[,] matrix, double[] means, double[] standardDeviations)
        {
            return Center(matrix, means).Standardize(standardDeviations, true);
        }

        /// <summary>
        ///   Generates the Standard Scores, also known as Z-Scores, from the given data.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The Z-Scores for the matrix.</returns>
        public static double[][] ZScores(double[][] matrix)
        {
            double[] mean = Mean(matrix);
            return ZScores(matrix, mean, StandardDeviation(matrix, mean));
        }

        /// <summary>
        ///   Generates the Standard Scores, also known as Z-Scores, from the given data.
        /// </summary>
        /// 
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// 
        /// <returns>The Z-Scores for the matrix.</returns>
        /// 
        public static double[][] ZScores(double[][] matrix, double[] means, double[] standardDeviations)
        {
            return Center(matrix, means).Standardize(standardDeviations, true);
        }


        /// <summary>
        ///   Centers column data, subtracting the empirical mean from each variable.
        /// </summary>
        /// 
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        /// 
        public static double[,] Center(this double[,] matrix, bool inPlace = true)
        {
            return Center(matrix, Mean(matrix), inPlace);
        }

        /// <summary>
        ///   Centers column data, subtracting the empirical mean from each variable.
        /// </summary>   
        /// 
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        /// 
        public static double[,] Center(this double[,] matrix, double[] means, bool inPlace = true)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] result = inPlace ? matrix : new double[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = matrix[i, j] - means[j];

            return result;
        }

        /// <summary>
        ///   Centers column data, subtracting the empirical mean from each variable.
        /// </summary>
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        public static double[][] Center(this double[][] matrix, bool inPlace = true)
        {
            return Center(matrix, Mean(matrix), inPlace);
        }

        /// <summary>Centers column data, subtracting the empirical mean from each variable.</summary>
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        public static double[][] Center(this double[][] matrix, double[] means, bool inPlace = true)
        {
            double[][] result = matrix;

            if (!inPlace)
            {
                result = new double[matrix.Length][];
                for (int i = 0; i < matrix.Length; i++)
                    result[i] = new double[matrix[i].Length];
            }

            for (int i = 0; i < matrix.Length; i++)
            {
                double[] row = result[i];
                for (int j = 0; j < row.Length; j++)
                    row[j] = matrix[i][j] - means[j];
            }

            return result;
        }


        /// <summary>
        ///   Standardizes column data, removing the empirical standard deviation from each variable.
        /// </summary>
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <remarks>This method does not remove the empirical mean prior to execution.</remarks>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        public static double[,] Standardize(this double[,] matrix, bool inPlace = true)
        {
            return Standardize(matrix, StandardDeviation(matrix), inPlace);
        }

        /// <summary>
        ///   Standardizes column data, removing the empirical standard deviation from each variable.
        /// </summary>
        /// 
        /// <remarks>This method does not remove the empirical mean prior to execution.</remarks>
        ///
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        /// 
        public static double[,] Standardize(this double[,] matrix, double[] standardDeviations, bool inPlace = true)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            double[,] result = inPlace ? matrix : new double[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i, j] = matrix[i, j] / standardDeviations[j];

            return result;
        }


        /// <summary>
        ///   Standardizes column data, removing the empirical standard deviation from each variable.
        /// </summary>
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <remarks>This method does not remove the empirical mean prior to execution.</remarks>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        public static double[][] Standardize(this double[][] matrix, bool inPlace = true)
        {
            return Standardize(matrix, StandardDeviation(matrix), inPlace);
        }

        /// <summary>
        ///   Standardizes column data, removing the empirical standard deviation from each variable.
        /// </summary>
        /// <param name="matrix">A matrix where each column represent a variable and each row represent a observation.</param>
        /// <remarks>This method does not remove the empirical mean prior to execution.</remarks>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <param name="inPlace">True to perform the operation in place, altering the original input matrix.</param>
        public static double[][] Standardize(this double[][] matrix, double[] standardDeviations, bool inPlace = true)
        {
            double[][] result = matrix;

            if (!inPlace)
            {
                result = new double[matrix.Length][];
                for (int i = 0; i < matrix.Length; i++)
                    result[i] = new double[matrix[i].Length];
            }

            for (int i = 0; i < matrix.Length; i++)
            {
                double[] resultRow = result[i];
                double[] sourceRow = matrix[i];
                for (int j = 0; j < resultRow.Length; j++)
                    resultRow[j] = sourceRow[i] / standardDeviations[j];
            }

            return result;
        }
        #endregion

        #region Weighted Matrix Measures
        /// <summary>
        ///   Calculates the weighted matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="weights">A vector containing the importance of each sample in the matrix.</param>
        /// <param name="dimension">
        ///   The dimension along which the means will be calculated. Pass
        ///   0 to compute a row vector containing the mean of each column,
        ///   or 1 to compute a column vector containing the mean of each row.
        ///   Default value is 0.
        /// </param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        /// 
        public static double[] WeightedMean(double[][] matrix, double[] weights, int dimension)
        {
            int rows = matrix.Length;
            if (rows == 0) return new double[0];
            int cols = matrix[0].Length;
            double[] mean;

            if (dimension == 0)
            {
                mean = new double[cols];

                // for each row
                for (int i = 0; i < rows; i++)
                {
                    double[] row = matrix[i];
                    double w = weights[i];

                    // for each column
                    for (int j = 0; j < cols; j++)
                        mean[j] += row[j] * w;
                }
            }
            else if (dimension == 1)
            {
                mean = new double[rows];

                // for each row
                for (int j = 0; j < rows; j++)
                {
                    double[] row = matrix[j];
                    double w = weights[j];

                    // for each column
                    for (int i = 0; i < cols; i++)
                        mean[j] += row[i] * w;
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return mean;
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] WeightedCovariance(double[][] matrix, double[] weights, double[] means)
        {
            double sw = 1.0;
            for (int i = 0; i < weights.Length; i++)
                sw -= weights[i] * weights[i];

            return WeightedScatter(matrix, weights, means, sw, 0);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="dimension">
        ///   Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] WeightedCovariance(double[][] matrix, double[] weights, int dimension)
        {
            double[] mean = Tools.WeightedMean(matrix, weights, dimension);
            return Tools.WeightedCovariance(matrix, weights, mean, dimension);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="dimension">
        ///   Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] WeightedCovariance(double[][] matrix, double[] weights, double[] means, int dimension)
        {
            double sw = 1.0;
            for (int i = 0; i < weights.Length; i++)
                sw -= weights[i] * weights[i];

            return WeightedScatter(matrix, weights, means, sw, dimension);
        }

        /// <summary>
        ///   Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///   By dividing the Scatter matrix by the sample size, we get the population
        ///   Covariance matrix. By dividing by the sample size minus one, we get the
        ///   sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="weights">An unit vector containing the importance of each sample
        /// in <see param="values"/>. The sum of this array elements should add up to 1.</param>
        /// <param name="means">The values' mean vector, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <param name="dimension">
        ///   Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] WeightedScatter(double[][] matrix, double[] weights, double[] means, double divisor, int dimension)
        {
            int rows = matrix.Length;
            if (rows == 0) return new double[0, 0];
            int cols = matrix[0].Length;

            double[,] cov;

            if (dimension == 0)
            {
                if (means.Length != cols) throw new ArgumentException(
                    "Length of the mean vector should equal the number of columns", "means");

                cov = new double[cols, cols];
                for (int i = 0; i < cols; i++)
                {
                    for (int j = i; j < cols; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < rows; k++)
                            s += weights[k] * (matrix[k][j] - means[j]) * (matrix[k][i] - means[i]);

                        //s /= divisor; // This normalizer often causes NaNs
                        cov[i, j] = s;
                        cov[j, i] = s;

                    }
                }
            }
            else if (dimension == 1)
            {
                if (means.Length != rows) throw new ArgumentException(
                    "Length of the mean vector should equal the number of rows", "means");

                cov = new double[rows, rows];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = i; j < rows; j++)
                    {
                        double s = 0.0;
                        for (int k = 0; k < cols; k++)
                            s += weights[k] * (matrix[j][k] - means[j]) * (matrix[i][k] - means[i]);
                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return cov;
        }
        #endregion


        // ------------------------------------------------------------


        #region Summarizing, grouping and extending operations
        /// <summary>
        ///   Calculates the prevalence of a class.
        /// </summary>
        /// <param name="positives">An array of counts detailing the occurence of the first class.</param>
        /// <param name="negatives">An array of counts detailing the occurence of the second class.</param>
        /// <returns>An array containing the proportion of the first class over the total of occurances.</returns>
        public static double[] Proportions(int[] positives, int[] negatives)
        {
            double[] r = new double[positives.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (double)positives[i] / (positives[i] + negatives[i]);
            return r;
        }

        /// <summary>
        ///   Calculates the prevalence of a class.
        /// </summary>
        /// <param name="data">A matrix containing counted, grouped data.</param>
        /// <param name="positiveColumn">The index for the column which contains counts for occurence of the first class.</param>
        /// <param name="negativeColumn">The index for the column which contains counts for occurence of the second class.</param>
        /// <returns>An array containing the proportion of the first class over the total of occurances.</returns>
        public static double[] Proportions(int[][] data, int positiveColumn, int negativeColumn)
        {
            double[] r = new double[data.Length];
            for (int i = 0; i < r.Length; i++)
                r[i] = (double)data[i][positiveColumn] / (data[i][positiveColumn] + data[i][negativeColumn]);
            return r;
        }

        /// <summary>
        ///   Groups the occurances contained in data matrix of binary (dichotomous) data.
        /// </summary>
        /// <param name="data">A data matrix containing at least a column of binary data.</param>
        /// <param name="labelColumn">Index of the column which contains the group label name.</param>
        /// <param name="dataColumn">Index of the column which contains the binary [0,1] data.</param>
        /// <returns>
        ///    A matrix containing the group label in the first column, the number of occurances of the first class
        ///    in the second column and the number of occurances of the second class in the third column.
        /// </returns>
        public static int[][] Group(int[][] data, int labelColumn, int dataColumn)
        {
            var groups = new List<int>();
            var groupings = new List<int[]>();

            for (int i = 0; i < data.Length; i++)
            {
                int group = data[i][labelColumn];
                if (!groups.Contains(group))
                {
                    groups.Add(group);

                    int positives = 0, negatives = 0;
                    for (int j = 0; j < data.Length; j++)
                    {
                        if (data[j][labelColumn] == group)
                        {
                            if (data[j][dataColumn] == 0)
                                negatives++;
                            else positives++;
                        }
                    }

                    groupings.Add(new int[] { group, positives, negatives });
                }
            }

            return groupings.ToArray();
        }

        /// <summary>
        ///   Extends a grouped data into a full observation matrix.
        /// </summary>
        /// <param name="data">The group labels.</param>
        /// <param name="positives">
        ///   An array containing he occurence of the positive class
        ///   for each of the groups.</param>
        /// <param name="negatives">
        ///   An array containing he occurence of the negative class
        ///   for each of the groups.</param>
        /// <returns>A full sized observation matrix.</returns>
        public static int[][] Expand(int[] data, int[] positives, int[] negatives)
        {
            List<int[]> rows = new List<int[]>();

            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < positives[i]; j++)
                    rows.Add(new int[] { data[i], 1 });

                for (int j = 0; j < negatives[i]; j++)
                    rows.Add(new int[] { data[i], 0 });
            }

            return rows.ToArray();
        }

        /// <summary>
        ///   Expands a grouped data into a full observation matrix.
        /// </summary>
        /// 
        /// <param name="data">The grouped data matrix.</param>
        /// <param name="labelColumn">Index of the column which contains the labels
        /// in the grouped data matrix. </param>
        /// <param name="positiveColumn">Index of the column which contains
        ///   the occurances for the first class.</param>
        /// <param name="negativeColumn">Index of the column which contains
        ///   the occurances for the second class.</param>
        ///   
        /// <returns>A full sized observation matrix.</returns>
        /// 
        public static int[][] Expand(int[][] data, int labelColumn, int positiveColumn, int negativeColumn)
        {
            List<int[]> rows = new List<int[]>();

            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < data[i][positiveColumn]; j++)
                    rows.Add(new int[] { data[i][labelColumn], 1 });

                for (int j = 0; j < data[i][negativeColumn]; j++)
                    rows.Add(new int[] { data[i][labelColumn], 0 });
            }

            return rows.ToArray();
        }

        /// <summary>
        ///   Expands a grouped data into a full observation matrix.
        /// </summary>
        /// 
        public static double[][] Expand(int[] classes)
        {
            int max = classes.Max();

            double[][] outputs = new double[classes.Length][];

            for (int i = 0; i < classes.Length; i++)
            {
                outputs[i] = new double[max + 1];
                outputs[i][classes[i]] = 1.0;
            }

            return outputs;
        }
        #endregion


        #region Determination and performance measures
        /// <summary>
        ///   Gets the coefficient of determination, as known as the R-Squared (R²)
        /// </summary>
        /// <remarks>
        ///    The coefficient of determination is used in the context of statistical models
        ///    whose main purpose is the prediction of future outcomes on the basis of other
        ///    related information. It is the proportion of variability in a data set that
        ///    is accounted for by the statistical model. It provides a measure of how well
        ///    future outcomes are likely to be predicted by the model.
        ///    
        ///    The R^2 coefficient of determination is a statistical measure of how well the
        ///    regression approximates the real data points. An R^2 of 1.0 indicates that the
        ///    regression perfectly fits the data.
        /// </remarks>
        public static double Determination(double[] actual, double[] expected)
        {
            // R-squared = 100 * SS(regression) / SS(total)

            int N = actual.Length;
            double SSe = 0.0;
            double SSt = 0.0;
            double avg = 0.0;
            double d;

            // Calculate expected output mean
            for (int i = 0; i < N; i++)
                avg += expected[i];
            avg /= N;

            // Calculate SSe and SSt
            for (int i = 0; i < N; i++)
            {
                d = expected[i] - actual[i];
                SSe += d * d;

                d = expected[i] - avg;
                SSt += d * d;
            }

            // Calculate R-Squared
            return 1.0 - (SSe / SSt);
        }
        #endregion


        #region Permutations and combinatorials
        /// <summary>
        ///   Returns a random sample of size k from a population of size n.
        /// </summary>
        public static int[] Random(int n, int k)
        {
            int[] idx = Tools.Random(n);
            return idx.Submatrix(k);
        }

        /// <summary>
        ///   Returns a random permutation of size n.
        /// </summary>
        public static int[] Random(int n)
        {
            Random random = Accord.Math.Tools.Random;

            double[] x = new double[n];
            int[] idx = Matrix.Indices(0, n);

            for (int i = 0; i < n; i++)
                x[i] = random.NextDouble();

            Array.Sort(x, idx);

            return idx;
        }

        /// <summary>
        ///   Shuffles an array.
        /// </summary>
        public static void Shuffle(int[] array)
        {
            Random random = Accord.Math.Tools.Random;

            // i is the number of items remaining to be shuffled.
            for (int i = array.Length; i > 1; i--)
            {
                // Pick a random element to swap with the i-th element.
                int j = random.Next(i);

                // Swap array elements.
                var aux = array[j];
                array[j] = array[i - 1];
                array[i - 1] = aux;
            }
        }
        #endregion


        // ------------------------------------------------------------

        /// <summary>
        ///   Computes the whitening transform for the given data, making
        ///   its covariance matrix equals the identity matrix.
        /// </summary>
        /// <param name="value">A matrix where each column represent a
        ///   variable and each row represent a observation.</param>
        /// <param name="transformMatrix">The base matrix used in the
        ///   transformation.</param>
        /// <returns>
        ///   The transformed source data (which now has unit variance).
        /// </returns>
        /// 
        public static double[,] Whitening(double[,] value, out double[,] transformMatrix)
        {
            if (value == null)
                throw new ArgumentNullException("value");


            int cols = value.GetLength(1);

            double[,] cov = Tools.Covariance(value);

            // Diagonalizes the covariance matrix
            SingularValueDecomposition svd = new SingularValueDecomposition(cov,
                true,  // compute left vectors (to become a transformation matrix)
                false, // do not compute right vectors since they aren't necessary
                true,  // transpose if necessary to avoid erroneous assumptions in SVD
                true); // perform operation in-place, reducing memory usage


            // Retrieve the transformation matrix
            transformMatrix = svd.LeftSingularVectors;

            // Perform scaling to have unit variance
            double[] singularValues = svd.Diagonal;
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < singularValues.Length; j++)
                    transformMatrix[i, j] /= Math.Sqrt(singularValues[j]);

            // Return the transformed data
            return value.Multiply(transformMatrix);
        }

    }
}

