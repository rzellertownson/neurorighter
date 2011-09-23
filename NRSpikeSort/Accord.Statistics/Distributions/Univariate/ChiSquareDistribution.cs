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
    ///   Chi-Square (χ²) probability distribution
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   In probability theory and statistics, the chi-square distribution (also chi-squared
    ///   or χ²-distribution) with k degrees of freedom is the distribution of a sum of the 
    ///   squares of k independent standard normal random variables. It is one of the most 
    ///   widely used probability distributions in inferential statistics, e.g. in hypothesis 
    ///   testing, or in construction of confidence intervals.</para>
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Chi-square_distribution">
    ///       Wikipedia, The Free Encyclopedia. Chi-square distribution. Available on: 
    ///       http://en.wikipedia.org/wiki/Chi-square_distribution </a></description></item>
    ///   </list></para>     
    /// </remarks>
    /// 
    [Serializable]
    public class ChiSquareDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters
        private int degreesOfFreedom;

        // Distribution measures
        private double? entropy;

        /// <summary>
        ///   Constructs a new Chi-Square distribution
        ///   with given degrees of freedom.
        /// </summary>
        public ChiSquareDistribution(int degreesOfFreedom)
        {
            this.degreesOfFreedom = degreesOfFreedom;
        }

        /// <summary>
        ///   Gets the Degrees of Freedom for this distribution.
        /// </summary>
        public int DegreesOfFreedom
        {
            get { return degreesOfFreedom; }
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   the χ² distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        ///   The Probability Density Function (PDF) describes the
        ///   probability that a given value <c>x</c> will occur.</para>
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///       <a href="http://en.wikipedia.org/wiki/Chi-square_distribution#Probability_density_function">
        ///       Wikipedia, the free encyclopedia. Chi-square distribution. </a></description></item>
        ///   </list></para>
        /// </remarks>
        /// 
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the current distribution.</returns>
        ///   
        public override double ProbabilityDensityFunction(double x)
        {
            double v = degreesOfFreedom;
            double m1 = Math.Pow(x, (v - 2.0) / 2.0);
            double m2 = Math.Exp(-x / 2.0);
            double m3 = Math.Pow(2, v / 2.0) * Special.Gamma(v / 2.0);
            return (m1 * m2) / m3;
        }

        /// <summary>
        ///   Gets the cumulative distribution function (cdf) for
        ///   the χ² distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <remarks>
        ///   The Cumulative Distribution Function (CDF) describes the cumulative
        ///   probability that a given value or any value smaller than it will occur.
        /// </remarks>
        /// 
        public override double DistributionFunction(double x)
        {
            return Special.ChiSq(degreesOfFreedom, x);
        }

        /// <summary>
        ///   Gets the complementary cumulative distribution
        ///   function for the χ² evaluated at point <c>x</c>.
        /// </summary>
        /// 
        public double SurvivalFunction(double x)
        {
            return Special.ChiSqc(degreesOfFreedom, x);
        }


        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get { return degreesOfFreedom; }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get { return 2.0 * degreesOfFreedom; }
        }

        /// <summary>
        ///   Gets the entropy for this distribution.
        /// </summary>
        /// 
        public override double Entropy
        {
            get
            {
                if (!entropy.HasValue)
                {
                    double kd2 = degreesOfFreedom / 2.0;
                    double m1 = Math.Log(2.0 * Special.Gamma(kd2));
                    double m2 = (1.0 - kd2) * Special.Digamma(kd2);
                    entropy =  kd2 + m1 + m2;
                }

                return entropy.Value;
            }
        }

        /// <summary>
        ///   This method is not supported.
        /// </summary>
        /// 
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new System.NotSupportedException();
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
            return new ChiSquareDistribution(degreesOfFreedom);
        }
    }

}