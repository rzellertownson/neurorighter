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

namespace Accord.Statistics.Distributions.Univariate
{
    using Accord.Math;
    using System;
    using Accord.Statistics.Distributions.Fitting;

    /// <summary>
    ///   Normal (Gaussian) distribution.
    /// </summary>
    /// 
    /// <remarks>
    ///   The Gaussian is the most widely used distribution for continuous
    ///   variables. In the case of a single variable, it is governed by
    ///   two parameters, the mean and the variance.
    /// </remarks>
    /// 
    [Serializable]
    public class NormalDistribution : UnivariateContinuousDistribution
    {

        // Distribution parameters
        private double mean = 0;  // mean
        private double sigma = 1; // standard deviation

        // Distribution measures
        private double? entropy;

        // Derived measures
        private double variance = 1;
        private double lnconstant; // log(1/sqrt(2*pi*variance))

        private bool immutable;


        /// <summary>
        ///   Constructs a Normal (Gaussian) distribution
        ///   with zero mean and unit standard deviation.
        /// </summary>
        /// 
        public NormalDistribution()
        {
            initialize(mean, sigma, sigma * sigma);
        }

        /// <summary>
        ///   Constructs a Normal (Gaussian) distribution
        ///   with given mean and unit standard deviation.
        /// </summary>
        /// 
        /// <param name="mean">The distribution's mean value.</param>
        /// 
        public NormalDistribution(double mean)
        {
            initialize(mean, sigma, sigma * sigma);
        }

        /// <summary>
        ///   Constructs a Normal (Gaussian) distribution
        ///   with given mean and standard deviation.
        /// </summary>
        /// 
        /// <param name="mean">The distribution's mean value.</param>
        /// <param name="sigma">The distribution's standard deviation.</param>
        /// 
        public NormalDistribution(double mean, double sigma)
        {
            if (sigma <= 0)
                throw new ArgumentOutOfRangeException("sigma", "Sigma must be positive.");

            initialize(mean, sigma, sigma * sigma);
        }



        /// <summary>
        ///   Gets the Mean for this Normal distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get { return mean; }
        }

        /// <summary>
        ///   Gets the Variance (the square of the standard
        ///   deviation) for this Normal distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get { return variance; }
        }

        /// <summary>
        ///   Gets the Standard Deviation (the square root of
        ///   the variance) for this Normal distribution.
        /// </summary>
        /// 
        public override double StandardDeviation
        {
            get { return sigma; }
        }

        /// <summary>
        ///   Gets the Entropy for this Normal distribution.
        /// </summary>
        /// 
        public override double Entropy
        {
            get
            {
                if (!entropy.HasValue)
                {
                    entropy = 0.5 * (Math.Log(2.0 * Math.PI * variance) + 1);
                }

                return entropy.Value;
            }
        }

        /// <summary>
        ///   Gets the cumulative distribution function (cdf) for
        ///   the this Normal distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">
        ///   A single point in the distribution range.</param>
        ///   
        /// <remarks>
        /// <para>
        ///   The Cumulative Distribution Function (CDF) describes the cumulative
        ///   probability that a given value or any value smaller than it will occur.</para>
        /// <para>
        ///  The calculation is computed through the relationship to the error function
        ///  as <see cref="Accord.Math.Special.Erfc">erfc</see>(-z/sqrt(2)) / 2.</para>  
        ///  
        /// <para>    
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///       Weisstein, Eric W. "Normal Distribution." From MathWorld--A Wolfram Web Resource.
        ///       Available on: http://mathworld.wolfram.com/NormalDistribution.html </description></item>
        ///     <item><description><a href="http://en.wikipedia.org/wiki/Normal_distribution#Cumulative_distribution_function">
        ///       Wikipedia, The Free Encyclopedia. Normal distribution. Available on:
        ///       http://en.wikipedia.org/wiki/Normal_distribution#Cumulative_distribution_function </a></description></item>
        ///   </list></para>
        /// </remarks>
        /// 
        public override double DistributionFunction(double x)
        {
            double z = (x - mean) / sigma;
            return Special.Erfc(-z / Special.Sqrt2) * 0.5;

            /*
                // For a normal distribution with zero variance, the cdf is the Heaviside
                // step function (Wipedia, http://en.wikipedia.org/wiki/Normal_distribution)
                return (x >= mean) ? 1.0 : 0.0;
            */
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   the Normal distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range. For a
        ///   univariate distribution, this should be a single
        ///   double value. For a multivariate distribution,
        ///   this should be a double array.</param>
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the current distribution.
        /// </returns>
        /// <remarks>
        ///   The Probability Density Function (PDF) describes the
        ///   probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// 
        public override double ProbabilityDensityFunction(double x)
        {
            double z = (x - mean) / sigma;
            double lnp = lnconstant + ((-z * z) * 0.5);

            return Math.Exp(lnp);

            /*
                 // In the case the variance is zero, return the weak limit function 
                 // of the sequence of Gaussian functions with variance towards zero.

                 // In this case, the pdf can be seen as a Dirac delta function
                 // (Wikipedia, http://en.wikipedia.org/wiki/Dirac_delta_function).

                 return (x == mean) ? Double.PositiveInfinity : 0.0;
             */
        }

        /// <summary>
        ///   Gets the Z-Score for a given value.
        /// </summary>
        /// 
        public double ZScore(double x)
        {
            return (x - mean) / sigma;
        }



        /// <summary>
        ///   Gets the Standard Gaussian Distribution,
        ///   with zero mean and unit variance.
        /// </summary>
        /// 
        public static NormalDistribution Standard { get { return standard; } }

        private static readonly NormalDistribution standard = new NormalDistribution() { immutable = true };


        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// 
        /// <param name="observations">The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).</param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">Optional arguments which may be used during fitting, such
        ///   as regularization constants and additional parameters.</param>
        ///   
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        /// 
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            if (immutable) throw new InvalidOperationException();

            double mu, var;

            if (weights != null)
            {
#if DEBUG
                for (int i = 0; i < weights.Length; i++)
                    if (Double.IsNaN(weights[i]) || Double.IsInfinity(weights[i]))
                        throw new Exception("Invalid numbers in the weight vector.");
#endif

                // Compute weighted mean
                mu = Statistics.Tools.WeightedMean(observations, weights);

                // Compute weighted variance
                var = Statistics.Tools.WeightedVariance(observations, weights, mu);
            }
            else
            {
                // Compute weighted mean
                mu = Statistics.Tools.Mean(observations);

                // Compute weighted variance
                var = Statistics.Tools.Variance(observations, mu);
            }

            if (options != null)
            {
                // Parse optional estimation options
                NormalOptions o = (NormalOptions)options;
                double regularization = o.Regularization;

                if (var == 0 || Double.IsNaN(var) || Double.IsInfinity(var))
                    var = regularization;
            }

            if (var <= 0)
            {
                throw new ArgumentException("Variance is zero. Try specifying "
                    + "a regularization constant in the fitting options.");
            }

            initialize(mu, Math.Sqrt(var), var);
        }


        /// <summary>
        ///   Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///   A new object that is a copy of this instance.
        /// </returns>
        /// 
        public override object Clone()
        {
            return new NormalDistribution(mean, sigma);
        }


        private void initialize(double mu, double dev, double var)
        {
            this.mean = mu;
            this.sigma = dev;
            this.variance = var;

            // Compute derived values
            this.lnconstant = -Math.Log(Special.Sqrt2PI * dev);
        }


        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static NormalDistribution Estimate(double[] observations)
        {
            return Estimate(observations, null, null);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static NormalDistribution Estimate(double[] observations, NormalOptions options)
        {
            return Estimate(observations, null, options);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static NormalDistribution Estimate(double[] observations, double[] weights, NormalOptions options)
        {
            NormalDistribution n = new NormalDistribution();
            n.Fit(observations, weights, options);
            return n;
        }
    }
}
