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
    ///   Snedecor's F-Test.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   An F-test is any statistical test in which the test statistic has an
    ///   F-distribution under the null hypothesis. It is most often used when 
    ///   comparing statistical models that have been fit to a data set, in order
    ///   to identify the model that best fits the population from which the data
    ///   were sampled.</para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/F-test">
    ///        Wikipedia, The Free Encyclopedia. F-Test Test. Available on:
    ///        http://en.wikipedia.org/wiki/F-test </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    /// <seealso cref="OneWayAnova"/>
    /// <seealso cref="TwoWayAnova"/>
    /// 
    [Serializable]
    public class FTest : HypothesisTest
    {
        /// <summary>
        ///   Gets the distribution associated
        ///   with the test statistic.
        /// </summary>
        /// 
        public FDistribution StatisticDistribution { get; private set; }

        /// <summary>
        ///   Gets the degrees of freedom for the
        ///   numerator in the test distribution.
        /// </summary>
        /// 
        public int DegreesOfFreedom1 { get { return StatisticDistribution.DegreesOfFreedom1; } }

        /// <summary>
        ///   Gets the degrees of freedom for the
        ///   denominator in the test distribution.
        /// </summary>
        /// 
        public int DegreesOfFreedom2 { get { return StatisticDistribution.DegreesOfFreedom2; } }

        /// <summary>
        ///   Creates a new F-Test for a given statistic
        ///   with given degrees of freedom.
        /// </summary>
        /// 
        /// <param name="statistic">The test statistic.</param>
        /// <param name="d1">The degrees of freedom for the numerator.</param>
        /// <param name="d2">The degrees of freedom for the denominator.</param>
        /// 
        public FTest(double statistic, int d1, int d2)
            : base(statistic)
        {
            base.Hypothesis = Hypothesis.OneUpper;

            StatisticDistribution = new FDistribution(d1, d2);
            PValue = 1.0 - StatisticDistribution.DistributionFunction(statistic);
        }

        /// <summary>
        ///   Creates a new F-Test.
        /// </summary>
        /// 
        protected FTest()
        {
        }

    }
}
