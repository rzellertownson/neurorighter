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
    using System;
    using Accord.Math;
    using Accord.Statistics.Distributions.Fitting;

    /// <summary>
    ///   Log-Normal (Galton) distribution.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   The log-normal distribution is a probability distribution of a random
    ///   variable whose logarithm is normally distributed.</para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       Wikipedia, The Free Encyclopedia. Log-normal distribution.
    ///       Available on: http://en.wikipedia.org/wiki/Log-normal_distribution </description></item>
    ///     <item><description>
    ///       NIST/SEMATECH e-Handbook of Statistical Methods. Lognormal Distribution.
    ///       Available on: http://www.itl.nist.gov/div898/handbook/eda/section3/eda3669.htm </description></item>
    ///     <item><description>
    ///       Weisstein, Eric W. "Normal Distribution Function." From MathWorld--A Wolfram Web
    ///       Resource. http://mathworld.wolfram.com/NormalDistributionFunction.html </description></item>
    ///  </list></para>  
    /// </remarks>
    /// 
    [Serializable]
    public class LognormalDistribution : UnivariateContinuousDistribution
    {

        // Distribution parameters
        private double location = 0; // mean of the variable's natural logarithm
        private double shape = 1;    // std. dev. of the variable's natural logarithm

        // Distribution measures
        private double? mean;
        private double? variance;
        private double? entropy;

        // Derived measures
        private double shape2; // variance of the variable's natural logarithm
        private double constant; // 1/sqrt(2*pi*shape²)

        private bool immutable;


        /// <summary>
        ///   Constructs a Log-Normal (Galton) distribution
        ///   with zero location and unit shape.
        /// </summary>
        /// 
        public LognormalDistribution()
        {
            initialize(location, shape, shape * shape);
        }

        /// <summary>
        ///   Constructs a Log-Normal (Galton) distribution
        ///   with given location and unit shape.
        /// </summary>
        /// 
        /// <param name="location">The distribution's location value.</param>
        /// 
        public LognormalDistribution(double location)
        {
            initialize(location, shape, shape * shape);
        }

        /// <summary>
        ///   Constructs a Log-Normal (Gaulton) distribution
        ///   with given mean and standard deviation.
        /// </summary>
        /// 
        /// <param name="location">The distribution's location value.</param>
        /// <param name="shape">The distribution's shape deviation.</param>
        /// 
        public LognormalDistribution(double location, double shape)
        {
            initialize(location, shape, shape * shape);
        }

        /// <summary>
        ///   Shape parameter of the log-normal distribution.
        /// </summary>
        /// 
        public double Shape
        {
            get { return shape; }
        }

        /// <summary>
        ///   Location parameter of the log-normal distribution.
        /// </summary>
        /// 
        public double Location
        {
            get { return location; }
        }

        /// <summary>
        ///   Gets the Mean for this Log-Normal distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get
            {
                if (mean == null)
                    mean = Math.Exp(location + shape2 / 2.0);
                return mean.Value;
            }
        }

        /// <summary>
        ///   Gets the Variance (the square of the standard
        ///   deviation) for this Log-Normal distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get
            {
                if (variance == null)
                    variance = (Math.Exp(shape2) - 1.0) * Math.Exp(2 * location + shape2);
                return variance.Value;
            }
        }

        /// <summary>
        ///   Gets the Entropy for this Log-Normal distribution.
        /// </summary>
        /// 
        public override double Entropy
        {
            get
            {
                if (entropy == null)
                    entropy = 0.5 + 0.5 * Math.Log(2.0 * Math.PI * shape2) + location;
                return entropy.Value;
            }
        }

        /// <summary>
        ///   Gets the cumulative distribution function (cdf) for
        ///   the this Log-Normal distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///   A single point in the distribution range.</param>
        /// <remarks>
        /// <para>
        ///   The Cumulative Distribution Function (CDF) describes the cumulative
        ///   probability that a given value or any value smaller than it will occur.</para>
        /// <para>
        ///  The calculation is computed through the relationship to the error function
        ///  as <see cref="Accord.Math.Special.Erfc">erfc</see>(-z/sqrt(2)) / 2. See 
        ///  [Weisstein] for more details.</para>  
        ///  
        /// <para>    
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///       Weisstein, Eric W. "Normal Distribution Function." From MathWorld--A Wolfram Web
        ///       Resource. http://mathworld.wolfram.com/NormalDistributionFunction.html </description></item>
        ///   </list></para>
        /// </remarks>
        /// 
        public override double DistributionFunction(double x)
        {
            double z = (Math.Log(x) - location) / shape;
            return 0.5 * Special.Erfc(-z / Special.Sqrt2);
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
            double z = (Math.Log(x) - location) / shape;
            return constant * Math.Exp((-z * z) * 0.5) / x;
        }


        /// <summary>
        ///   Gets the Standard Log-Normal Distribution,
        ///   with zero location and unit shape.
        /// </summary>
        /// 
        public static LognormalDistribution Standard { get { return standard; } }

        private static readonly LognormalDistribution standard = new LognormalDistribution() { immutable = true };


        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).</param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">Optional arguments which may be used during fitting, such
        ///   as regularization constants and additional parameters.</param>
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

            observations = observations.Apply(Math.Log);

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
            return new LognormalDistribution(location, shape);
        }


        private void initialize(double mu, double dev, double var)
        {
            this.location = mu;
            this.shape = dev;
            this.shape2 = var;

            // Compute derived values
            this.constant = 1.0 / (Special.Sqrt2PI * dev);
        }


        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static LognormalDistribution Estimate(double[] observations)
        {
            return Estimate(observations, null, null);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static LognormalDistribution Estimate(double[] observations, NormalOptions options)
        {
            return Estimate(observations, null, options);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static LognormalDistribution Estimate(double[] observations, double[] weights, NormalOptions options = null)
        {
            LognormalDistribution n = new LognormalDistribution();
            n.Fit(observations, weights, options);
            return n;
        }
    }
}
