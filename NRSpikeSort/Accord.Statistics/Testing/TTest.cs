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
    ///   Test Hypothesis for the T-Test.
    /// </summary>
    /// 
    public enum TTestHypotesis
    {
        /// <summary>
        ///   Tests if the sample's mean is significantly
        ///   different than the hypothesized mean value.
        /// </summary>
        /// 
        MeanIsDifferentThanHypothesis,

        /// <summary>
        ///   Tests if the sample's mean is significantly
        ///   greater than the hypothesized mean value.
        /// </summary>
        /// 
        MeanIsGreaterThanHypothesis,

        /// <summary>
        ///   Tests if the sample's mean is significantly
        ///   smaller than the hypothesized mean value.
        /// </summary>
        MeanIsSmallerThanHypothesis,
    }

    /// <summary>
    ///   One-sample Student's T test.
    /// </summary>
    /// 
    /// <remarks>
    ///  <para>
    ///   The two-sample t-test assesses whether the means of two groups are statistically 
    ///   different from each other.</para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Student's_t-test">
    ///       Wikipedia, The Free Encyclopedia. Student's T-Test. </a></description></item>
    ///     <item><description><a href="http://www.le.ac.uk/bl/gat/virtualfc/Stats/ttest.html">
    ///       William M.K. Trochim. The T-Test. Research methods Knowledge Base, 2009. 
    ///       Available on: http://www.le.ac.uk/bl/gat/virtualfc/Stats/ttest.html </a></description></item>
    ///     <item><description><a href="http://en.wikipedia.org/wiki/One-way_ANOVA">
    ///       Graeme D. Ruxton. The unequal variance t-test is an underused alternative to Student's
    ///       t-test and the Mann–Whitney U test. Oxford Journals, Behavioral Ecology Volume 17, Issue 4, pp.
    ///       688-690. 2006. Available on: http://beheco.oxfordjournals.org/content/17/4/688.full </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class TTest : HypothesisTest
    {

        /// <summary>
        ///   Gets the probability distribution associated
        ///   with the test statistic.
        /// </summary>
        public TDistribution StatisticDistribution { get; private set; }

        /// <summary>
        ///   Tests the null hypothesis that the population mean is equal to a specified value.
        /// </summary>
        /// 
        public TTest(double[] sample, double hypothesizedMean, TTestHypotesis type)
        {
            int n = sample.Length;
            double x = Accord.Statistics.Tools.Mean(sample);
            double s = Accord.Statistics.Tools.StandardDeviation(sample, x);

            StatisticDistribution = new TDistribution(n - 1);
            Statistic = (x - hypothesizedMean) / (s / Math.Sqrt(n));


            if (type == TTestHypotesis.MeanIsDifferentThanHypothesis)
            {
                PValue = 2.0 * StatisticDistribution.SurvivalFunction(Statistic);
                Hypothesis = Testing.Hypothesis.TwoTail;
            }
            else if (type == TTestHypotesis.MeanIsGreaterThanHypothesis)
            {
                PValue = StatisticDistribution.SurvivalFunction(Statistic);
                Hypothesis = Testing.Hypothesis.OneUpper;
            }
            else if (type == TTestHypotesis.MeanIsSmallerThanHypothesis)
            {
                PValue = StatisticDistribution.DistributionFunction(Statistic);
                Hypothesis = Testing.Hypothesis.OneLower;
            }
        }

    }

}
