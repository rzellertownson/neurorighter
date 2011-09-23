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

namespace Accord.Statistics.Models.Regression
{
    using System;
    using System.Linq;
    using Accord.Statistics.Testing;
    using AForge;

    /// <summary>
    ///   Binary Logistic Regression.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   In statistics, logistic regression (sometimes called the logistic model or
    ///   logit model) is used for prediction of the probability of occurrence of an
    ///   event by fitting data to a logistic curve. It is a generalized linear model
    ///   used for binomial regression.</para>
    /// <para>
    ///   Like many forms of regression analysis, it makes use of several predictor
    ///   variables that may be either numerical or categorical. For example, the
    ///   probability that a person has a heart attack within a specified time period
    ///   might be predicted from knowledge of the person's age, sex and body mass index.</para>
    /// <para>
    ///   Logistic regression is used extensively in the medical and social sciences
    ///   as well as marketing applications such as prediction of a customer's
    ///   propensity to purchase a product or cease a subscription.</para>  
    /// 
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       Bishop, Christopher M.; Pattern Recognition and Machine Learning. 
    ///       Springer; 1st ed. 2006.</description></item>
    ///     <item><description>
    ///       Amos Storkey. (2005). Learning from Data: Learning Logistic Regressors. School of Informatics.
    ///       Available on: http://www.inf.ed.ac.uk/teaching/courses/lfd/lectures/logisticlearn-print.pdf </description></item>
    ///     <item><description>
    ///       Cosma Shalizi. (2009). Logistic Regression and Newton's Method. Available on:
    ///       http://www.stat.cmu.edu/~cshalizi/350/lectures/26/lecture-26.pdf </description></item>
    ///     <item><description>
    ///       Edward F. Conor. Logistic Regression. Website. Available on: 
    ///       http://userwww.sfsu.edu/~efc/classes/biol710/logistic/logisticreg.htm </description></item>
    ///   </list></para>  
    /// </remarks>
    /// 
    /// <example>
    ///   <code>
    ///    // Suppose we have the following data about some patients.
    ///    // The first variable is continuous and represent patient
    ///    // age. The second variable is dicotomic and give whether
    ///    // they smoke or not (This is completely fictional data).
    ///    double[][] input =
    ///    {
    ///        new double[] { 55, 0 }, // 0 - no cancer
    ///        new double[] { 28, 0 }, // 0
    ///        new double[] { 65, 1 }, // 0
    ///        new double[] { 46, 0 }, // 1 - have cancer
    ///        new double[] { 86, 1 }, // 1
    ///        new double[] { 56, 1 }, // 1
    ///        new double[] { 85, 0 }, // 0
    ///        new double[] { 33, 0 }, // 0
    ///        new double[] { 21, 1 }, // 0
    ///        new double[] { 42, 1 }, // 1
    ///    };
    ///
    ///    // We also know if they have had lung cancer or not, and 
    ///    // we would like to know whether smoking has any connection
    ///    // with lung cancer (This is completely fictional data).
    ///    double[] output =
    ///    {
    ///        0, 0, 0, 1, 1, 1, 0, 0, 0, 1
    ///    };
    ///
    ///
    ///    // To verify this hypothesis, we are going to create a logistic
    ///    // regression model for those two inputs (age and smoking).
    ///    LogisticRegression regression = new LogisticRegression(inputs: 2);
    ///
    ///    // Next, we are going to estimate this model. For this, we
    ///    // will use the Iteravely reweighted least squares method.
    ///    var teacher = new IterativeReweightedLeastSquares(regression);
    ///
    ///    // Now, we will iteratively estimate our model. The Run method returns
    ///    // the maximum relative change in the model parameters and we will use
    ///    // it as the convergence criteria.
    ///
    ///    double delta = 0;
    ///    do
    ///    {
    ///        // Perform an iteration
    ///        delta = teacher.Run(input, output);
    ///
    ///    } while (delta > 0.001);
    ///
    ///    // At this point, we can compute the odds ratio of our variables.
    ///    // In the model, the variable at 0 is always the intercept term, 
    ///    // with the other following in the sequence. Index 1 is the age
    ///    // and index 2 is whether the patient smokes or not.
    ///
    ///    // For the age variable, we have that individuals with
    ///    //   higher age have 1.021 greater odds of getting lung
    ///    //   cancer controlling for cigarrete smoking.
    ///    double ageOdds = regression.GetOddsRatio(1); // 1.0208597028836701
    ///
    ///    // For the smoking/non smoking category variable, however, we
    ///    //   have that individuals who smoke have 5.858 greater odds
    ///    //   of developing lung cancer compared to those who do not 
    ///    //   smoke, controlling for age (remember, this is completely
    ///    //   fictional and for demonstration purposes only).
    ///    double smokeOdds = regression.GetOddsRatio(2); // 5.8584748789881331
    ///   </code>
    /// </example>
    /// 
    [Serializable]
    public class LogisticRegression : ICloneable
    {

        private double[] coefficients;
        private double[] standardErrors;


        //---------------------------------------------


        #region Constructor
        /// <summary>
        ///   Creates a new Logistic Regression Model.
        /// </summary>
        /// 
        /// <param name="inputs">The number of input variables for the model.</param>
        /// 
        public LogisticRegression(int inputs)
        {
            this.coefficients = new double[inputs + 1];
            this.standardErrors = new double[inputs + 1];
        }

        /// <summary>
        ///   Creates a new Logistic Regression Model.
        /// </summary>
        /// 
        /// <param name="inputs">The number of input variables for the model.</param>
        /// <param name="intercept">The starting intercept value. Default is 0.</param>
        /// 
        public LogisticRegression(int inputs, double intercept)
            : this(inputs)
        {
            this.coefficients[0] = intercept;
        }
        #endregion


        //---------------------------------------------


        #region Properties
        /// <summary>
        ///   Gets the coefficient vector, in which the
        ///   first value is always the intercept value.
        /// </summary>
        /// 
        public double[] Coefficients
        {
            get { return coefficients; }
        }

        /// <summary>
        ///   Gets the standard errors associated with each
        ///   cofficient during the model estimation phase.
        /// </summary>
        /// 
        public double[] StandardErrors
        {
            get { return standardErrors; }
        }

        /// <summary>
        ///   Gets the number of inputs handled by this model.
        /// </summary>
        /// 
        public int Inputs
        {
            get { return coefficients.Length - 1; }
        }
        #endregion


        //---------------------------------------------


        #region Public Methods
        /// <summary>
        ///   Computes the model output for the given input vector.
        /// </summary>
        /// 
        /// <param name="input">The input vector.</param>
        /// <returns>The output value.</returns>
        /// 
        public double Compute(double[] input)
        {
            double logit = coefficients[0];

            for (int i = 1; i < coefficients.Length; i++)
                logit += input[i - 1] * coefficients[i];

            return Logistic(logit);
        }

        /// <summary>
        ///   Computes the model output for each of the given input vectors.
        /// </summary>
        /// 
        /// <param name="input">The array of input vectors.</param>
        /// <returns>The array of output values.</returns>
        /// 
        public double[] Compute(double[][] input)
        {
            double[] output = new double[input.Length];

            for (int i = 0; i < input.Length; i++)
                output[i] = Compute(input[i]);

            return output;
        }


        /// <summary>
        ///   Gets the Odds Ratio for a given coefficient.
        /// </summary>
        /// <remarks>
        ///   The odds ratio can be computed raising euler's number
        ///   (e ~~ 2.71) to the power of the associated coefficient.
        /// </remarks>
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        /// <returns>
        ///   The Odds Ratio for the given coefficient.
        /// </returns>
        /// 
        public double GetOddsRatio(int index)
        {
            return Math.Exp(coefficients[index]);
        }

        /// <summary>
        ///   Gets the 95% confidence interval for the
        ///   Odds Ratio for a given coefficient.
        /// </summary>
        /// 
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        /// 
        public DoubleRange GetConfidenceInterval(int index)
        {
            double coeff = coefficients[index];
            double error = standardErrors[index];

            double upper = coeff + 1.96 * error;
            double lower = coeff - 1.96 * error;

            DoubleRange ci = new DoubleRange(Math.Exp(lower), Math.Exp(upper));

            return ci;
        }

        /// <summary>
        ///   Gets the Wald Test for a given coefficient.
        /// </summary>
        /// 
        /// <remarks>
        ///   The Wald statistical test is a test for a model parameter in which
        ///   the estimated parameter θ is compared with another proposed parameter
        ///   under the assumption that the difference between them will be approximately
        ///   normal. There are several problems with the use of the Wald test. Please
        ///   take a look on substitute tests based on the log-likelihood if possible.
        /// </remarks>
        /// 
        /// <param name="index">
        ///   The coefficient's index. The first value
        ///   (at zero index) is the intercept value.
        /// </param>
        /// 
        public WaldTest GetWaldTest(int index)
        {
            return new WaldTest(coefficients[index], 0.0, standardErrors[index]);
        }


        /// <summary>
        ///   Gets the Log-Likelihood for the model.
        /// </summary>
        /// 
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <returns>
        ///   The Log-Likelihood (a measure of performance) of
        ///   the model calculated over the given data sets.
        /// </returns>
        /// 
        public double GetLogLikelihood(double[][] input, double[] output)
        {
            double sum = 0;

            for (int i = 0; i < input.Length; i++)
            {
                double y = Compute(input[i]);
                double o = output[i];

                sum += o * Math.Log(y) + (1 - o) * Math.Log(1 - y);
            }

            return sum;
        }

        /// <summary>
        ///   Gets the Deviance for the model.
        /// </summary>
        /// 
        /// <remarks>
        ///   The deviance is defined as -2*Log-Likelihood.
        /// </remarks>
        /// 
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <returns>
        ///   The deviance (a measure of performance) of the model
        ///   calculated over the given data sets.
        /// </returns>
        /// 
        public double GetDeviance(double[][] input, double[] output)
        {
            return -2.0 * GetLogLikelihood(input, output);
        }

        /// <summary>
        ///   Gets the Log-Likelihood Ratio between two models.
        /// </summary>
        /// 
        /// <remarks>
        ///   The Log-Likelihood ratio is defined as 2*(LL - LL0).
        /// </remarks>
        /// 
        /// <param name="input">A set of input data.</param>
        /// <param name="output">A set of output data.</param>
        /// <param name="regression">Another Logistic Regression model.</param>
        /// <returns>The Log-Likelihood ratio (a measure of performance
        /// between two models) calculated over the given data sets.</returns>
        /// 
        public double GetLogLikelihoodRatio(double[][] input, double[] output, LogisticRegression regression)
        {
            return 2.0 * (this.GetLogLikelihood(input, output) - regression.GetLogLikelihood(input, output));
        }


        /// <summary>
        ///   The likelihood ratio test of the overall model, also called the model chi-square test.
        /// </summary>
        /// 
        /// <remarks>
        ///   <para>
        ///   The Chi-square test, also called the likelihood ratio test or the log-likelihood test
        ///   is based on the deviance of the model (-2*log-likelihood). The log-likelihood ratio test 
        ///   indicates whether there is evidence of the need to move from a simpler model to a more
        ///   complicated one (where the simpler model is nested within the complicated one).</para>
        ///   <para>
        ///   The difference between the log-likelihood ratios for the researcher's model and a
        ///   simpler model is often called the "model chi-square".</para>
        /// </remarks>
        /// 
        public ChiSquareTest ChiSquare(double[][] input, double[] output)
        {
            double y0 = output.Count(y => y == 0.0);
            double y1 = output.Length - y0;

            LogisticRegression regression = new LogisticRegression(Inputs, Math.Log(y1 / y0));

            double ratio = GetLogLikelihoodRatio(input, output, regression);
            return new ChiSquareTest(ratio, coefficients.Length - 1);
        }



        /// <summary>
        ///   Creates a new LogisticRegression that is a copy of the current instance.
        /// </summary>
        /// 
        public object Clone()
        {
            LogisticRegression regression = new LogisticRegression(coefficients.Length);
            regression.coefficients = (double[])this.coefficients.Clone();
            regression.standardErrors = (double[])this.standardErrors.Clone();
            return regression;
        }
        #endregion


        //---------------------------------------------


        #region Static Methods
        /// <summary>
        ///   The Logistic function.
        /// </summary>
        /// 
        /// <param name="value">The logit parameter.</param>
        /// 
        public static double Logistic(double value)
        {
            return 1.0 / (1.0 + System.Math.Exp(-value));
        }
        #endregion

    }

}
