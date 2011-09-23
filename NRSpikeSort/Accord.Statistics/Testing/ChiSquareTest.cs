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
    ///   Two-Sample (Goodness-of-fit) Chi-Square Test (Upper tail)
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   A chi-square test (also chi-squared or χ2  test) is any statistical
    ///   hypothesis test in which the sampling distribution of the test statistic
    ///   is a chi-square distribution when the null hypothesis is true, or any in
    ///   which this is asymptotically true, meaning that the sampling distribution
    ///   (if the null hypothesis is true) can be made to approximate a chi-square
    ///   distribution as closely as desired by making the sample size large enough.</para>
    /// <para>
    ///   The chi-square test is used whenever one would like to test whether the
    ///   actual data differs from a random distribution. </para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Chi-square_test">
    ///        Wikipedia, The Free Encyclopedia. Chi-Square Test. Available on:
    ///        http://en.wikipedia.org/wiki/Chi-square_test </a></description></item>
    ///   
    ///     <item><description><a href="http://www2.lv.psu.edu/jxm57/irp/chisquar.html">
    ///        J. S. McLaughlin. Chi-Square Test. Available on:
    ///        http://www2.lv.psu.edu/jxm57/irp/chisquar.html </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class ChiSquareTest : HypothesisTest
    {

        private ChiSquareDistribution distribution;



        /// <summary>
        ///   Gets the degrees of freedom for the Chi-Square distribution.
        /// </summary>
        /// 
        public int DegreesOfFreedom
        {
            get { return distribution.DegreesOfFreedom; }
        }


        #region Constructors
        /// <summary>
        ///   Constructs a Chi-Square Test.
        /// </summary>
        /// <param name="statistic">The test statistic.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        /// <param name="threshold">The significance threshold. By default, 0.05 will be used.</param>
        /// 
        public ChiSquareTest(double statistic, int degreesOfFreedom, double threshold)
        {
            this.Statistic = statistic;
            this.Threshold = threshold;
            this.distribution = new ChiSquareDistribution(degreesOfFreedom);

            this.PValue = distribution.SurvivalFunction(Statistic);
        }

        /// <summary>
        ///   Constructs a Chi-Square Test.
        /// </summary>
        /// <param name="statistic">The test statistic.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        /// 
        public ChiSquareTest(double statistic, int degreesOfFreedom)
            : this(statistic, degreesOfFreedom, 0.05)
        {
        }

        /// <summary>
        ///   Construct a Chi-Square Test.
        /// </summary>
        /// <param name="expected">The expected variable values.</param>
        /// <param name="observed">The observed variable values.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        /// <param name="threshold">The significance threshold. By default, 0.05 will be used.</param>
        /// 
        public ChiSquareTest(double[] expected, double[] observed, int degreesOfFreedom, double threshold)
        {
            if (expected == null)
                throw new ArgumentNullException("expected");

            if (observed == null)
                throw new ArgumentNullException("observed");


            // X² = sum(o - e)²
            //          -----
            //            e

            double sum = 0.0;
            for (int i = 0; i < observed.Length; i++)
            {
                double d = observed[i] - expected[i];
                sum += (d * d) / expected[i];
            }

            this.Statistic = sum;
            this.Threshold = threshold;
            this.distribution = new ChiSquareDistribution(degreesOfFreedom);

            this.PValue = distribution.SurvivalFunction(Statistic);
        }
        #endregion


    }
}
