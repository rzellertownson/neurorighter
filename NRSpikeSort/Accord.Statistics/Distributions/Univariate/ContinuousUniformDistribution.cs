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
    using System.Linq;

    /// <summary>
    ///   Continuous Uniform Distribution.
    /// </summary>
    /// 
    [Serializable]
    public class ContinuousUniformDistribution : UnivariateContinuousDistribution
    {
        private double a;
        private double b;

        private bool immutable;

        /// <summary>
        ///   Creates a new uniform distribution defined in the interval [0;1].
        /// </summary>
        /// 
        public ContinuousUniformDistribution()
            : this(0, 1)
        {
        }

        /// <summary>
        ///   Creates a new uniform distribution defined in the interval [a;b].
        /// </summary>
        /// 
        /// <param name="a">The starting number a.</param>
        /// <param name="b">The ending number b.</param>
        /// 
        public ContinuousUniformDistribution(double a, double b)
        {
            if (a > b)
                throw new ArgumentOutOfRangeException("b", 
                    "The starting number a must be lower than b.");

            this.a = a;
            this.b = b;
        }

        /// <summary>
        ///   Gets the minimum value of the distribution (a).
        /// </summary>
        /// 
        public double Minimum { get { return a; } }

        /// <summary>
        ///   Gets the maximum value of the distribution (b).
        /// </summary>
        /// 
        public double Maximum { get { return b; } }

        /// <summary>
        ///   Gets the length of the distribution (b-a).
        /// </summary>
        /// 
        public double Length { get { return b - a; } }

        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get { return (a + b) / 2; }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get { return ((b - a) * (b - a)) / 12.0; }
        }

        /// <summary>
        ///   Gets the entropy for this distribution.
        /// </summary>
        /// 
        public override double Entropy
        {
            get { return Math.Log(b - a); }
        }

        /// <summary>
        ///   Gets the cumulative distribution function (cdf) for
        ///   the this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range.</param>
        /// 
        /// <remarks>
        ///   The Cumulative Distribution Function (CDF) describes the cumulative
        ///   probability that a given value or any value smaller than it will occur.
        /// </remarks>
        /// 
        public override double DistributionFunction(double x)
        {
            if (x < a)
                return 0;
            if (x >= b)
                return 1;
            return (x - a) / (b - a);
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range.</param>
        /// 
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the current distribution.
        /// </returns>
        /// 
        /// <remarks>
        ///   The Probability Density Function (PDF) describes the
        ///   probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// 
        public override double ProbabilityDensityFunction(double x)
        {
            if (x >= a && x <= b)
                return 1.0 / (b - a);
            else return 0;
        }

        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// 
        /// <param name="observations">The array of observations to fit the model against. The array
        /// elements can be either of type double (for univariate data) or
        /// type double[] (for multivariate data).</param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">Optional arguments which may be used during fitting, such
        /// as regularization constants and additional parameters.</param>
        /// 
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        /// 
        public override void Fit(double[] observations, double[] weights, Fitting.IFittingOptions options)
        {
            if (immutable)
                throw new InvalidOperationException("This object can not be modified.");

            if (options != null)
                throw new ArgumentException("No options may be specified.");

            a = observations.Min();
            b = observations.Max();
        }

        /// <summary>
        ///   Creates a new object that is a copy of the current instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A new object that is a copy of this instance.
        /// </returns>
        /// 
        public override object Clone()
        {
            return new ContinuousUniformDistribution(a, b);
        }


        /// <summary>
        ///   Gets the Standard Uniform Distribution,
        ///   starting at zero and ending at one (a=0, b=1).
        /// </summary>
        /// 
        public static ContinuousUniformDistribution Standard { get { return standard; } }

        private static readonly ContinuousUniformDistribution standard = new ContinuousUniformDistribution() { immutable = true };


    }
}
