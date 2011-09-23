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
    using Accord.Statistics.Distributions.Fitting;

    /// <summary>
    ///   Bernoulli probability distribution.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The Bernoulli distribution is a distribution for a single
    ///   binary variable x E {0,1}, representing, for example, the
    ///   flipping of a coin. It is governed by a single continuous
    ///   parameter representing the probability of an observation
    ///   to be equal to 1.</para>
    ///   
    /// <para>    
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Bernoulli_distribution">
    ///       Wikipedia, The Free Encyclopedia. Bernoulli distribution. Available on:
    ///       http://en.wikipedia.org/wiki/Bernoulli_distribution </a></description></item>
    ///     <item><description>
    ///       C. Bishop. “Pattern Recognition and Machine Learning”. Springer. 2006.</description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class BernoulliDistribution : UnivariateDiscreteDistribution
    {
        // Distribution parameters
        private double probability;

        // Derived parameter values
        private double complement;

        // Distribution measures
        private double? entropy;


        /// <summary>
        ///   Creates a new <see cref="BernoulliDistribution">Bernoulli</see> distribution.
        /// </summary>
        /// 
        /// <param name="mean">The probability of an observation being equal to 1.</param>
        /// 
        public BernoulliDistribution(double mean)
        {
            this.initialize(mean);
        }

        private void initialize(double mean)
        {
            this.probability = mean;
            this.complement = 1.0 - mean;

            this.entropy = null;
        }

        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get { return probability; }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get { return probability * complement; }
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
                    entropy = -probability * System.Math.Log(probability) -
                                complement * System.Math.Log(complement);
                }

                return entropy.Value;
            }
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
        public override double DistributionFunction(int x)
        {
            if (x < 0) return 0;
            if (x >= 1) return 1;
            return complement;
        }

        /// <summary>
        ///   Gets the probability mass function (pmf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range.</param>
        /// 
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the current distribution.
        /// </returns>
        /// <remarks>
        ///   The Probability Mass Function (PMF) describes the
        ///   probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// 
        public override double ProbabilityMassFunction(int x)
        {
            if (x == 1) return probability;
            if (x == 0) return complement;
            return 0;
        }

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
            double mean = Statistics.Tools.WeightedMean(observations, weights);
            initialize(mean);
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
            return new BernoulliDistribution(probability);
        }
    }
}
