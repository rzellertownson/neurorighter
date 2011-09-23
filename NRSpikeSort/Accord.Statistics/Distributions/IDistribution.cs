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

namespace Accord.Statistics.Distributions
{
    using System;
    using Accord.Statistics.Distributions.Fitting;

    /// <summary>
    ///   Common interface for probability distributions.
    /// </summary>
    /// 
    public interface IDistribution : ICloneable
    {
        /// <summary>
        ///   Gets the cumulative distribution function (cdf) for
        ///   the this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <remarks>
        ///   The Cumulative Distribution Function (CDF) describes the cumulative
        ///   probability that a given value or any value smaller than it will occur.
        /// </remarks>
        /// 
        double DistributionFunction(params double[] x);

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///   A single point in the distribution range. For a 
        ///   univariate distribution, this should be a single
        ///   double value. For a multivariate distribution,
        ///   this should be a double array.</param>
        /// <remarks>
        ///   The Probability Density Function (PDF) describes the
        ///   probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the current distribution.</returns>
        ///   
        double ProbabilityFunction(params double[] x);

        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///   The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).</param>
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        ///   
        void Fit(Array observations);

        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///   The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).</param>
        /// <param name="weights">
        ///   The weight vector containing the weight for each of the samples.</param>
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        /// 
        void Fit(Array observations, double[] weights);

        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///   The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).</param>
        /// <param name="options">
        ///   Optional arguments which may be used during fitting, such
        ///   as regularization constants and additional parameters.</param>
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        /// 
        void Fit(Array observations, IFittingOptions options);

        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///   The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///   The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///   Optional arguments which may be used during fitting, such
        ///   as regularization constants and additional parameters.</param>
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        /// 
        void Fit(Array observations, double[] weights, IFittingOptions options);

    }

    /// <summary>
    ///   Common interface for univariate probability distributions.
    /// </summary>
    /// 
    public interface IUnivariateDistribution : IDistribution
    {
        /// <summary>
        ///   Gets the mean value for the distribution.
        /// </summary>
        /// 
        double Mean { get; }

        /// <summary>
        ///   Gets the variance value for the distribution.
        /// </summary>
        /// 
        double Variance { get; }

        /// <summary>
        ///   Gets the median value for the distribution.
        /// </summary>
        /// 
        double Median { get; }

        /// <summary>
        ///   Gets the mode value for the distribution.
        /// </summary>
        /// 
        double Mode { get; }

        /// <summary>
        ///   Gets entropy of the distribution.
        /// </summary>
        /// 
        double Entropy { get; }

    }

    /// <summary>
    ///   Common interface for multivariate probability distributions.
    /// </summary>
    /// 
    public interface IMultivariateDistribution : IDistribution
    {
        /// <summary>
        ///   Gets the number of variables for the distribution.
        /// </summary>
        /// 
        int Dimension { get; }

        /// <summary>
        ///   Gets the Mean vector for the distribution.
        /// </summary>
        /// 
        double[] Mean { get; }

        /// <summary>
        ///   Gets the Median vector for the distribution.
        /// </summary>
        /// 
        double[] Median { get; }

        /// <summary>
        ///   Gets the Mode vector for the distribution.
        /// </summary>
        /// 
        double[] Mode { get; }

        /// <summary>
        ///   Gets the Variance vector for the distribution.
        /// </summary>
        /// 
        double[] Variance { get; }

        /// <summary>
        ///   Gets the Variance-Covariance matrix for the distribution.
        /// </summary>
        /// 
        double[,] Covariance { get; }
    }

}
