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

    /// <summary>
    ///   Student's t-distribution.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>    
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Student's_t-distribution">
    ///       Wikipedia, The Free Encyclopedia. Student's t-distribution. Available on:
    ///       http://en.wikipedia.org/wiki/Student's_t-distribution </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class TDistribution : UnivariateContinuousDistribution
    {
        private double constant;


        /// <summary>
        ///   Gets the degrees of freedom for the distribution.
        /// </summary>
        /// 
        public double DegreesOfFreedom { get; private set; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="TDistribution"/> class.
        /// </summary>
        /// 
        /// <param name="degreesOfFreedom">The degrees of freedom.</param>
        /// 
        public TDistribution(double degreesOfFreedom)
        {
            if (degreesOfFreedom < 1)
                throw new ArgumentOutOfRangeException("degreesOfFreedom");

            this.DegreesOfFreedom = degreesOfFreedom;

            double v = degreesOfFreedom;

            // TODO: Use LogGamma instead.
            this.constant = Special.Gamma((v + 1) / 2.0) / (Math.Sqrt(v * Math.PI) * Special.Gamma(v / 2.0));
        }


        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get { return (DegreesOfFreedom > 1) ? 0 : Double.NaN; }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get
            {
                if (DegreesOfFreedom > 2)
                    return DegreesOfFreedom / (DegreesOfFreedom - 2);
                else if (DegreesOfFreedom > 1)
                    return Double.PositiveInfinity;
                return Double.NaN;
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
            double v = DegreesOfFreedom;
            double sqrt = Math.Sqrt(x * x + v);
            double u = (x + sqrt) / (2 * sqrt);
            return Special.Ibeta(v / 2.0, v / 2.0, u);
        }

        /// <summary>
        ///   Gets the survival function, also known as
        ///   the complementary distribution function.
        /// </summary>
        /// 
        public double SurvivalFunction(double x)
        {
            return 1.0 - DistributionFunction(x);
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range.</param>
        /// 
        /// <returns>
        /// The probability of <c>x</c> occurring
        /// in the current distribution.
        /// </returns>
        /// 
        /// <remarks>
        /// The Probability Density Function (PDF) describes the
        /// probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// 
        public override double ProbabilityDensityFunction(double x)
        {
            double v = DegreesOfFreedom;
            return constant * Math.Pow(1 + (x * x) / DegreesOfFreedom, -(v + 1) / 2.0);
        }

        /// <summary>
        ///  Not supported.
        /// </summary>
        /// 
        public override void Fit(double[] observations, double[] weights, Fitting.IFittingOptions options)
        {
            throw new NotSupportedException();
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
            return new TDistribution(DegreesOfFreedom);
        }

    }
}
