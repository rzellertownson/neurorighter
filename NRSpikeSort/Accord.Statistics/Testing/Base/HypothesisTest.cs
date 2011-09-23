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
    using Accord.Statistics.Distributions;

    /// <summary>
    ///   Hypothesis type
    /// </summary>
    /// 
    /// <remarks>
    ///   The type of the hypothesis being made expresses the way in
    ///   which a value of a parameter may deviate from that assumed
    ///   in the null hypothesis. It can either state that a value is
    ///   higher, lower or simply different than the one assumed under
    ///   the null hypothesis.
    /// </remarks>
    /// 
    public enum Hypothesis
    {
        /// <summary>
        ///   The test considers the upper tail from a probability distribution.
        /// </summary>
        /// 
        /// <remarks>
        ///   The one-tailed, upper tail test is a statistical test in which a given
        ///   statistical hypothesis, H0 (the null hypothesis), will be rejected when
        ///   the value of the test statistic is sufficiently large. 
        /// </remarks>
        /// 
        OneUpper,

        /// <summary>
        ///   The test considers the lower tail from a probability distribution.
        /// </summary>
        /// 
        /// <remarks>
        ///   The one-tailed, lower tail test is a statistical test in which a given
        ///   statistical hypothesis, H0 (the null hypothesis), will be rejected when
        ///   the value of the test statistic is sufficiently small. 
        /// </remarks>
        /// 
        OneLower,

        /// <summary>
        ///   The test considers the two tails from a probability distribution.
        /// </summary>
        /// 
        /// <remarks>
        ///   The two-tailed test is a statistical test in which a given statistical
        ///   hypothesis, H0 (the null hypothesis), will be rejected when the value of
        ///   the test statistic is either sufficiently small or sufficiently large. 
        /// </remarks>
        /// 
        TwoTail
    };

    /// <summary>
    ///   Base class for Hypothesis Tests.
    /// </summary>
    /// 
    /// <remarks>
    ///   A statistical hypothesis test is a method of making decisions using data, whether from
    ///   a controlled experiment or an observational study (not controlled). In statistics, a 
    ///   result is called statistically significant if it is unlikely to have occurred by chance
    ///   alone, according to a pre-determined threshold probability, the significance level.
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Statistical_hypothesis_testing">
    ///       Wikipedia, The Free Encyclopedia. Statistical Hypothesis Testing. </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public abstract class HypothesisTest : IFormattable
    {
        private double pvalue;
        private double statistic;
        private double threshold = 0.05;
        private Hypothesis hypothesis;


        /// <summary>
        ///   Initializes a new instance of the <see cref="HypothesisTest"/> class.
        /// </summary>
        /// 
        protected HypothesisTest()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="HypothesisTest"/> class.
        /// </summary>
        /// <param name="statistic">The test statistic.</param>
        /// 
        protected HypothesisTest(double statistic)
        {
            this.Statistic = statistic;
        }

        /// <summary>
        ///   Gets the significance threshold. Default value is 0.05 (5%).
        /// </summary>
        /// 
        public double Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }

        /// <summary>
        ///   Gets whether the null hypothesis can be accepted or should be rejected.
        /// </summary>
        /// <remarks>
        ///   A test result is said to be statistically significant when the result
        ///   would be very unlikely to have occurred by chance alone.
        /// </remarks>
        /// 
        public bool Significant
        {
            get { return pvalue < threshold; }
        }

        /// <summary>
        ///   Gets the P-value associated with this test.
        /// </summary>
        /// <remarks>
        /// <para>
        ///   In statistical hypothesis testing, the p-value is the probability of
        ///   obtaining a test statistic at least as extreme as the one that was
        ///   actually observed, assuming that the null hypothesis is true.</para>
        /// <para>
        ///   The lower the p-value, the less likely the result can be explained
        ///   by chance alone, assuming the null hypothesis is true.</para>  
        /// </remarks>
        /// 
        public double PValue
        {
            get { return pvalue; }
            protected set { pvalue = value; }
        }

        /// <summary>
        ///   Gets the test statistic.
        /// </summary>
        /// 
        public double Statistic
        {
            get { return statistic; }
            protected set { statistic = value; }
        }

        /// <summary>
        ///   Gets the test type.
        /// </summary>
        /// 
        public Hypothesis Hypothesis
        {
            get { return hypothesis; }
            protected set { hypothesis = value; }
        }

        /// <summary>
        ///   Converts the numeric P-Value of this test to its equivalent string representation.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return PValue.ToString(format, formatProvider);
        }

        /// <summary>
        ///   Converts the numeric P-Value of this test to its equivalent string representation.
        /// </summary>
        public override string ToString()
        {
            return PValue.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }


    }
}
