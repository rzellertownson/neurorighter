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

namespace Accord.Statistics.Models.Regression.Linear
{
    using System;

    /// <summary>
    ///   Simple Linear Regression of the form y = Ax + B.
    /// </summary>
    /// 
    /// <remarks>
    ///   In linear regression, the model specification is that the dependent
    ///   variable, y is a linear combination of the parameters (but need not
    ///   be linear in the independent variables). As the linear regression
    ///   has a closed form solution, the regression coefficients can be
    ///   efficiently computed using the Regress method of this class.
    /// </remarks>
    /// 
    /// <example>
    ///   <code>
    ///   // Declare some example data
    ///   double[] inputs =  { 80, 60, 10, 20, 30 };
    ///   double[] outputs = { 20, 40, 30, 50, 60 };
    ///     
    ///   // Create a new Simple Linear Regression
    ///   SimpleLinearRegression linreg = new SimpleLinearRegression();
    ///     
    ///   // Perform the linear regression
    ///   linreg.Regress(inputs, outputs);
    ///     
    ///   // Compute the output for a given input
    ///   double y = linreg.Compute(85);
    ///   </code>
    /// </example>
    /// 
    [Serializable]
    public class SimpleLinearRegression : ILinearRegression
    {
        private MultipleLinearRegression regression;

        /// <summary>
        ///   Creates a new Simple Linear Regression of the form y = Ax + B.
        /// </summary>
        /// 
        public SimpleLinearRegression()
        {
            this.regression = new MultipleLinearRegression(2);
        }

        /// <summary>
        ///   Angular coefficient (Slope).
        /// </summary>
        /// 
        public double Slope
        {
            get { return regression.Coefficients[1]; }
        }

        /// <summary>
        ///   Linear coefficient (Intercept).
        /// </summary>
        /// 
        public double Intercept
        {
            get { return regression.Coefficients[0]; }
        }


        /// <summary>
        ///   Performs the regression using the input and output
        ///   data, returning the sum of squared errors of the fit.
        /// </summary>
        /// 
        /// <param name="inputs">The input data.</param>
        /// <param name="outputs">The output data.</param>
        /// <returns>The regression Sum-of-Squares error.</returns>
        /// 
        public double Regress(double[] inputs, double[] outputs)
        {
            if (inputs.Length != outputs.Length)
                throw new ArgumentException("Number of input and output samples does not match", "outputs");

            double[][] X = new double[inputs.Length][];

            for (int i = 0; i < inputs.Length; i++)
            {
                // b[0]*1 + b[1]*inputs[i]
                X[i] = new double[] { 1.0, inputs[i] };
            }

            return regression.Regress(X, outputs);
        }

        /// <summary>
        ///   Computes the regression output for a given input.
        /// </summary>
        /// 
        /// <param name="input">An array of input values.</param>
        /// <returns>The array of calculated output values.</returns>
        /// 
        public double[] Compute(double[] input)
        {
            double[] output = new double[input.Length];

            // Call Compute(v) for each input vector v
            for (int i = 0; i < input.Length; i++)
                output[i] = Compute(input[i]);

            return output;
        }

        /// <summary>
        ///   Computes the regression for a single input.
        /// </summary>
        /// 
        /// <param name="input">The input value.</param>
        /// <returns>The calculated output.</returns>
        /// 
        public double Compute(double input)
        {
            return Slope * input + Intercept;
        }

        /// <summary>
        ///   Gets the coefficient of determination, as known as R² (r-squared).
        /// </summary>
        /// 
        /// <remarks>
        ///   <para>
        ///    The coefficient of determination is used in the context of statistical models
        ///    whose main purpose is the prediction of future outcomes on the basis of other
        ///    related information. It is the proportion of variability in a data set that
        ///    is accounted for by the statistical model. It provides a measure of how well
        ///    future outcomes are likely to be predicted by the model.</para>
        ///   <para>
        ///    The R² coefficient of determination is a statistical measure of how well the
        ///    regression line approximates the real data points. An R² of 1.0 indicates
        ///    that the regression line perfectly fits the data.</para> 
        /// </remarks>
        /// 
        /// <returns>The R² (r-squared) coefficient for the given data.</returns>
        /// 
        public double CoefficientOfDetermination(double[] inputs, double[] outputs, bool adjust)
        {
            double[][] X = new double[inputs.Length][];

            for (int i = 0; i < inputs.Length; i++)
            {
                // b[0]*1 + b[1]*inputs[i]
                X[i] = new double[] { 1.0, inputs[i] };
            }

            return regression.CoefficientOfDetermination(X, outputs, adjust);
        }

        /// <summary>
        ///   Gets the coefficient of determination, or R² (r-squared).
        /// </summary>
        /// 
        /// <remarks>
        ///   <para>
        ///    The coefficient of determination is used in the context of statistical models
        ///    whose main purpose is the prediction of future outcomes on the basis of other
        ///    related information. It is the proportion of variability in a data set that
        ///    is accounted for by the statistical model. It provides a measure of how well
        ///    future outcomes are likely to be predicted by the model.</para>
        ///   <para>
        ///    The R² coefficient of determination is a statistical measure of how well the
        ///    regression line approximates the real data points. An R² of 1.0 indicates
        ///    that the regression line perfectly fits the data.</para> 
        /// </remarks>
        /// 
        /// <returns>The R² (r-squared) coefficient for the given data.</returns>
        /// 
        public double CoefficientOfDetermination(double[] inputs, double[] outputs)
        {
            return CoefficientOfDetermination(inputs, outputs, false);
        }

        /// <summary>
        ///   Returns a System.String representing the regression.
        /// </summary>
        /// 
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture,
                "y(x) = {0}x + {1}", Slope, Intercept);
        }


        #region ILinearRegression Members
        /// <summary>
        ///   Computes the model output for a given input.
        /// </summary>
        double[] ILinearRegression.Compute(double[] inputs)
        {
            if (inputs.Length > 1)
                throw new ArgumentException("Simple regression supports only one-length input vectors", "inputs");

            return new double[] { this.Compute(inputs[0]) };
        }
        #endregion

    }
}
