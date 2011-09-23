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
    ///   F (Fisher-Snedecor) distribution.
    /// </summary>
    /// 
    [Serializable]
    public class FDistribution : UnivariateContinuousDistribution
    {

        // Distribution parameters
        private int d1;
        private int d2;

        // Derived values
        private double b;

        private double? mean;
        private double? variance;


        /// <summary>
        ///   Constructs a F-distribution with
        ///   the given degrees of freedom.
        /// </summary>
        /// 
        /// <param name="degrees1">The first degree of freedom.</param>
        /// <param name="degrees2">The second degree of freedom.</param>
        /// 
        public FDistribution(int degrees1, int degrees2)
        {
            this.d1 = degrees1;
            this.d2 = degrees2;

            this.b = Special.Beta(degrees1 * 0.5, degrees2 * 0.5);
        }

        /// <summary>
        ///   Gets the first degree of freedom.
        /// </summary>
        /// 
        public int DegreesOfFreedom1
        {
            get { return d1; }
        }

        /// <summary>
        ///   Gets the second degree of freedom.
        /// </summary>
        /// 
        public int DegreesOfFreedom2
        {
            get { return d2; }
        }

        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get
            {
                if (!mean.HasValue)
                {
                    if (d2 <= 2)
                    {
                        mean = Double.NaN;
                    }
                    else
                    {
                        mean = d2 / (d2 - 2.0);
                    }
                }

                return mean.Value;
            }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get
            {
                if (!variance.HasValue)
                {
                    if (d2 <= 4)
                    {
                        variance = Double.NaN;
                    }
                    else
                    {
                        variance = (2.0 * d2 * d2 * (d1 + d2 - 2)) /
                            (d1 * (d2 - 2) * (d2 - 2) * (d2 - 4));
                    }
                }

                return variance.Value;
            }
        }

        /// <summary>
        ///   Gets the entropy for this distribution.
        /// </summary>
        /// 
        public override double Entropy
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        ///   Gets the cumulative distribution function (cdf) for
        ///   the F-distribution evaluated at point <c>x</c>.
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
            double u = (d1 * x) / (d1 * x + d2);
            return Special.Ibeta(d1 * 0.5, d2 * 0.5, u);
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   the F-distribution evaluated at point <c>x</c>.
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
            double u = Math.Pow(d1 * x, d1) * Math.Pow(d2, d2) /
                Math.Pow(d1 * x + d2, d1 + d2);
            return Math.Sqrt(u) / (x * b);
        }


        /// <summary>
        ///   Not available.
        /// </summary>
        /// 
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
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
            return new FDistribution(d1, d2);
        }
    }
}
