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

namespace Accord.Statistics.Testing
{
    using System;
    using Accord.Statistics.Distributions.Univariate;

    /// <summary>
    ///   One-sample Z-Test (location test).
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The term Z-test is often used to refer specifically to the one-sample
    ///   location test comparing the mean of a set of measurements to a given
    ///   constant. Due to the central limit theorem, many test statistics are 
    ///   approximately normally distributed for large samples. Therefore, many
    ///   statistical tests can be performed as approximate Z-tests if the sample
    ///   size is large.</para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Z-test">
    ///        Wikipedia, The Free Encyclopedia. Z-Test. Available on:
    ///        http://en.wikipedia.org/wiki/Z-test </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class ZTest : HypothesisTest
    {

        /// <summary>
        ///   Constructs a Z test.
        /// </summary>
        /// <param name="samples">The data samples from which the test will be performed.</param>
        /// <param name="hypothesizedMean">The constant to be compared with the samples.</param>
        /// <param name="type">The type of hypothesis to test.</param>
        /// 
        public ZTest(double[] samples, double hypothesizedMean, Hypothesis type)
        {
            double mean = Tools.Mean(samples);
            double stdDev = Tools.StandardDeviation(samples, mean);
            double stdError = Tools.StandardError(samples.Length, stdDev);
            double z = (hypothesizedMean - mean) / stdError;

            this.init(z, type);
        }

        /// <summary>
        ///   Constructs a Z test.
        /// </summary>
        /// <param name="sampleMean">The sample's mean.</param>
        /// <param name="sampleStdDev">The sample's standard deviation.</param>
        /// <param name="hypothesizedMean">The hypothesized value for the distribution's mean.</param>
        /// <param name="samples">The sample's size.</param>
        /// <param name="type">The type of hypothesis to test.</param>
        /// 
        public ZTest(double sampleMean, double sampleStdDev, double hypothesizedMean, int samples, Hypothesis type)
        {
            double stdError = Tools.StandardError(samples, sampleStdDev);
            double z = (hypothesizedMean - sampleMean) / stdError;

            this.init(z, type);
        }

        /// <summary>
        ///   Constructs a Z test.
        /// </summary>
        /// <param name="statistic">The test statistic, as given by (x-μ)/SE.</param>
        /// <param name="type">The type of hypothesis to test.</param>
        /// 
        public ZTest(double statistic, Hypothesis type)
        {
            this.init(statistic, type);
        }


        private void init(double statistic, Hypothesis type)
        {
            this.Statistic = statistic;
            this.Hypothesis = type;

            if (this.Hypothesis == Hypothesis.TwoTail)
            {
                this.PValue = 2.0 * NormalDistribution.Standard.
                      DistributionFunction(-System.Math.Abs(Statistic));
            }
            else
            {
                this.PValue = NormalDistribution.Standard.
                      DistributionFunction(-System.Math.Abs(Statistic));
            }
        }

    }
}
