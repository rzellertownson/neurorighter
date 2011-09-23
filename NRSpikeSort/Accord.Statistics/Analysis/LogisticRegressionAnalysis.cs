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

namespace Accord.Statistics.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Accord.Math;
    using Accord.Statistics.Models.Regression;
    using Accord.Statistics.Models.Regression.Fitting;
    using Accord.Statistics.Testing;
    using AForge;

    /// <summary>
    ///   Logistic Regression Analysis.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The Logistic Regression Analysis tries to extract useful
    ///   information about a logistic regression model. </para>
    /// 
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       E. F. Connor. Logistic Regression. Available on:
    ///       http://userwww.sfsu.edu/~efc/classes/biol710/logistic/logisticreg.htm </description></item>
    ///     <item><description>
    ///       C. Shalizi. Logistic Regression and Newton's Method. Lecture notes. Available on:
    ///       http://www.stat.cmu.edu/~cshalizi/350/lectures/26/lecture-26.pdf </description></item>
    ///     <item><description>
    ///       A. Storkey. Learning from Data: Learning Logistic Regressors. Available on:
    ///       http://www.inf.ed.ac.uk/teaching/courses/lfd/lectures/logisticlearn-print.pdf </description></item>
    ///   </list></para>  
    /// </remarks>
    /// 
    [Serializable]
    public class LogisticRegressionAnalysis : IRegressionAnalysis
    {
        private LogisticRegression regression;

        private int inputCount;
        private double[] coefficients;
        private double[] standardErrors;
        private double[] oddsRatios;

        private WaldTest[] waldTests;
        private ChiSquareTest[] ratioTests;

        private DoubleRange[] confidences;

        private double deviance;
        private double logLikelihood;
        private ChiSquareTest chiSquare;

        private double[][] inputData;
        private double[] outputData;

        private string[] inputNames;
        private string outputName;

        private double[,] source;
        private double[] result;

        private LogisticCoefficientCollection coefficientCollection;


        //---------------------------------------------


        #region Constructors
        /// <summary>
        ///   Constructs a Logistic Regression Analysis.
        /// </summary>
        /// 
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// 
        public LogisticRegressionAnalysis(double[][] inputs, double[] outputs)
        {
            // Initial argument checking
            if (inputs == null) throw new ArgumentNullException("inputs");
            if (outputs == null) throw new ArgumentNullException("outputs");

            if (inputs.Length != outputs.Length)
                throw new ArgumentException("The number of rows in the input array must match the number of given outputs.");


            this.inputCount = inputs[0].Length;
            int coefficientCount = inputCount + 1;

            // Store data sets
            this.inputData = inputs;
            this.outputData = outputs;


            // Create additional structures
            this.coefficients = new double[coefficientCount];
            this.waldTests = new WaldTest[coefficientCount];
            this.standardErrors = new double[coefficientCount];
            this.oddsRatios = new double[coefficientCount];
            this.confidences = new DoubleRange[coefficientCount];
            this.ratioTests = new ChiSquareTest[coefficientCount];


            // Start regression using the Null Model
            this.regression = new LogisticRegression(inputCount);


            // Create coefficient object collection
            List<LogisticCoefficient> list = new List<LogisticCoefficient>(coefficientCount);
            for (int i = 0; i < coefficientCount; i++)
                list.Add(new LogisticCoefficient(this, i));
            this.coefficientCollection = new LogisticCoefficientCollection(list);

            this.source = inputs.ToMatrix();
        }

        /// <summary>
        ///   Constructs a Logistic Regression Analysis.
        /// </summary>
        /// 
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output, binary data for the analysis.</param>
        /// <param name="inputNames">The names of the input variables.</param>
        /// <param name="outputName">The name of the output variable.</param>
        /// 
        public LogisticRegressionAnalysis(double[][] inputs, double[] outputs,
            String[] inputNames, String outputName)
            : this(inputs, outputs)
        {
            this.inputNames = inputNames;
            this.outputName = outputName;
        }
        #endregion


        //---------------------------------------------


        #region Public Properties

        /// <summary>
        /// Source data used in the analysis.
        /// </summary>
        /// 
        public double[,] Source
        {
            get { return source; }
        }

        /// <summary>
        /// Gets the the dependent variable value
        /// for each of the source input points.
        /// </summary>
        /// 
        public double[] Outputs
        {
            get { return outputData; }
        }

        /// <summary>
        ///   Gets the resulting probabilities obtained
        ///   by the logistic regression model.
        /// </summary>
        /// 
        public double[] Result
        {
            get { return result; }
        }


        /// <summary>
        ///   Gets the Logistic Regression model created
        ///   and evaluated by this analysis.
        /// </summary>
        /// 
        public LogisticRegression Regression
        {
            get { return regression; }
        }

        /// <summary>
        ///   Gets the collection of coefficients of the model.
        /// </summary>
        /// 
        public ReadOnlyCollection<LogisticCoefficient> Coefficients
        {
            get { return coefficientCollection; }
        }

        /// <summary>
        ///   Gets the Log-Likelihood for the model.
        /// </summary>
        /// 
        public double LogLikelihood
        {
            get { return this.logLikelihood; }
        }

        /// <summary>
        ///   Gets the Chi-Square (Likelihood Ratio) Test for the model.
        /// </summary>
        /// 
        public ChiSquareTest ChiSquare
        {
            get { return this.chiSquare; }
        }

        /// <summary>
        ///   Gets the Deviance of the model.
        /// </summary>
        /// 
        public double Deviance
        {
            get { return deviance; }
        }

        /// <summary>
        ///   Gets the name of the input variables for the model.
        /// </summary>
        /// 
        public String[] Inputs
        {
            get { return inputNames; }
        }

        /// <summary>
        ///   Gets the name of the output variable for the model.
        /// </summary>
        /// 
        public String Output
        {
            get { return outputName; }
        }

        /// <summary>
        ///   Gets the Odds Ratio for each coefficient
        ///   found during the logistic regression.
        /// </summary>
        /// 
        public double[] OddsRatios
        {
            get { return this.oddsRatios; }
        }

        /// <summary>
        ///   Gets the Standard Error for each coefficient
        ///   found during the logistic regression.
        /// </summary>
        /// 
        public double[] StandardErrors
        {
            get { return this.standardErrors; }
        }

        /// <summary>
        ///   Gets the Wald Tests for each coefficient.
        /// </summary>
        /// 
        public WaldTest[] WaldTests
        {
            get { return this.waldTests; }
        }

        /// <summary>
        ///   Gets the Likelihood-Ratio Tests for each coefficient.
        /// </summary>
        /// 
        public ChiSquareTest[] LikelihoodRatioTests
        {
            get { return this.ratioTests; }
        }

        /// <summary>
        ///   Gets the value of each coefficient.
        /// </summary>
        /// 
        public double[] CoefficientValues
        {
            get { return this.coefficients; }
        }

        /// <summary>
        ///   Gets the 95% Confidence Intervals (C.I.)
        ///   for each coefficient found in the regression.
        /// </summary>
        /// 
        public DoubleRange[] Confidences
        {
            get { return this.confidences; }
        }

        #endregion


        //---------------------------------------------


        #region Public Methods
        /// <summary>
        ///   Gets the Log-Likelihood Ratio between this model and another model.
        /// </summary>
        /// 
        /// <param name="model">Another logistic regression model.</param>
        /// <returns>The Likelihood-Ratio between the two models.</returns>
        /// 
        public double GetLikelihoodRatio(LogisticRegression model)
        {
            return regression.GetLogLikelihoodRatio(inputData, outputData, model);
        }


        /// <summary>
        ///   Computes the Logistic Regression Analysis.
        /// </summary>
        /// 
        /// <remarks>The likelihood surface for the
        ///   logistic regression learning is convex, so there will be only one
        ///   peak. Any local maxima will be also a global maxima.
        /// </remarks>
        /// 
        /// <returns>
        ///   True if the model converged, false otherwise.
        /// </returns>
        /// 
        public bool Compute()
        {
            return Compute(10e-4, 50);
        }

        /// <summary>
        ///   Computes the Logistic Regression Analysis.
        /// </summary>
        /// 
        /// <remarks>The likelihood surface for the
        ///   logistic regression learning is convex, so there will be only one
        ///   peak. Any local maxima will be also a global maxima.
        /// </remarks>
        /// 
        /// <param name="limit">
        ///   The difference between two iterations of the regression algorithm
        ///   when the algorithm should stop. If not specified, the value of
        ///   10e-4 will be used. The difference is calculated based on the largest
        ///   absolute parameter change of the regression.
        /// </param>
        /// 
        /// <returns>
        ///   True if the model converged, false otherwise.
        /// </returns>
        /// 
        public bool Compute(double limit)
        {
            return Compute(limit, 50);
        }

        /// <summary>
        ///   Computes the Logistic Regression Analysis.
        /// </summary>
        /// 
        /// <remarks>The likelihood surface for the
        ///   logistic regression learning is convex, so there will be only one
        ///   peak. Any local maxima will be also a global maxima.
        /// </remarks>
        /// 
        /// <param name="limit">
        ///   The difference between two iterations of the regression algorithm
        ///   when the algorithm should stop. If not specified, the value of
        ///   10e-4 will be used. The difference is calculated based on the largest
        ///   absolute parameter change of the regression.
        /// </param>
        /// 
        /// <param name="maxIterations">
        ///   The maximum number of iterations to be performed by the regression
        ///   algorithm.
        /// </param>
        /// 
        /// <returns>
        ///   True if the model converged, false otherwise.
        /// </returns>
        /// 
        public bool Compute(double limit, int maxIterations)
        {
            double delta;
            int iteration = 0;

            var learning = new IterativeReweightedLeastSquares(regression);

            do // learning iterations until convergence
            {
                delta = learning.Run(inputData, outputData);
                iteration++;

            } while (delta > limit && iteration < maxIterations);

            // Check if the full model has converged
            bool converged = iteration <= maxIterations;


            // Store model information
            this.result = regression.Compute(inputData);
            this.deviance = regression.GetDeviance(inputData, outputData);
            this.logLikelihood = regression.GetLogLikelihood(inputData, outputData);
            this.chiSquare = regression.ChiSquare(inputData, outputData);

            // Store coefficient information
            for (int i = 0; i < regression.Coefficients.Length; i++)
            {
                this.standardErrors[i] = regression.StandardErrors[i];

                this.waldTests[i] = regression.GetWaldTest(i);
                this.coefficients[i] = regression.Coefficients[i];
                this.confidences[i] = regression.GetConfidenceInterval(i);
                this.oddsRatios[i] = regression.GetOddsRatio(i);
            }


            // Perform likelihood-ratio tests against diminished nested models
            LogisticRegression innerModel = new LogisticRegression(inputCount - 1);
            learning = new IterativeReweightedLeastSquares(innerModel);

            for (int i = 0; i < inputCount; i++)
            {
                // Create a diminished inner model without the current variable
                double[][] data = inputData.RemoveColumn(i);


                iteration = 0;

                do // learning iterations until convergence
                {
                    delta = learning.Run(data, outputData);
                    iteration++;

                } while (delta > limit && iteration < maxIterations);

                double ratio = 2.0 * (logLikelihood - innerModel.GetLogLikelihood(data, outputData));
                ratioTests[i + 1] = new ChiSquareTest(ratio, 1);
            }



            // Returns true if the full model has converged, false otherwise.
            return converged;
        }
        #endregion




        /// <summary>
        ///   Computes the analysis using given source data and parameters.
        /// </summary>
        void IAnalysis.Compute()
        {
            Compute();
        }

    }


    #region Support Classes

    /// <summary>
    ///   Represents a Logistic Regression Coefficient found in the Logistic Regression,
    ///   allowing it to be bound to controls like the DataGridView. This class cannot
    ///   be instantiated outside the <see cref="LogisticRegressionAnalysis"/>.
    /// </summary>
    /// 
    [Serializable]
    public class LogisticCoefficient
    {
        private LogisticRegressionAnalysis analysis;
        private int index;


        internal LogisticCoefficient(LogisticRegressionAnalysis analysis, int index)
        {
            this.analysis = analysis;
            this.index = index;
        }

        /// <summary>
        ///   Gets the name for the current coefficient.
        /// </summary>
        /// 
        public string Name
        {
            get
            {
                if (index == 0) return "Intercept";
                else return analysis.Inputs[index - 1];
            }
        }

        /// <summary>
        ///   Gets the Odds ratio for the current coefficient.
        /// </summary>
        /// 
        public double OddsRatio
        {
            get { return analysis.OddsRatios[index]; }
        }

        /// <summary>
        ///   Gets the Standard Error for the current coefficient.
        /// </summary>
        /// 
        public double StandardError
        {
            get { return analysis.StandardErrors[index]; }
        }

        /// <summary>
        ///   Gets the 95% confidence interval (CI) for the current coefficient.
        /// </summary>
        /// 
        public DoubleRange Confidence
        {
            get { return analysis.Confidences[index]; }
        }

        /// <summary>
        ///   Gets the upper limit for the 95% confidence interval.
        /// </summary>
        /// 
        public double ConfidenceUpper
        {
            get { return Confidence.Max; }
        }

        /// <summary>
        ///   Gets the lower limit for the 95% confidence interval.
        /// </summary>
        /// 
        public double ConfidenceLower
        {
            get { return Confidence.Min; }
        }

        /// <summary>
        ///   Gets the coefficient value.
        /// </summary>
        /// 
        public double Value
        {
            get { return analysis.CoefficientValues[index]; }
        }

        /// <summary>
        ///   Gets the Wald's test performed for this coefficient.
        /// </summary>
        /// 
        public WaldTest Wald
        {
            get { return analysis.WaldTests[index]; }
        }

        /// <summary>
        ///   Gets the Likelihood-Ratio test performed for this coefficient.
        /// </summary>
        /// 
        public ChiSquareTest LikelihoodRatio
        {
            get { return analysis.LikelihoodRatioTests[index]; }
        }


    }

    /// <summary>
    ///   Represents a collection of Logistic Coefficients found in the
    ///   <see cref="LogisticRegressionAnalysis"/>. This class cannot be instantiated.
    /// </summary>
    /// 
    [Serializable]
    public class LogisticCoefficientCollection : ReadOnlyCollection<LogisticCoefficient>
    {
        internal LogisticCoefficientCollection(IList<LogisticCoefficient> coefficients)
            : base(coefficients) { }
    }
    #endregion

}
