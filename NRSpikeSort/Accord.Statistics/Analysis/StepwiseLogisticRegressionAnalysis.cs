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
    using System.Collections.ObjectModel;
    using Accord.Math;
    using Accord.Statistics.Models.Regression;
    using Accord.Statistics.Testing;
    using Accord.Statistics.Models.Regression.Fitting;

    /// <summary>
    ///   Backward Stepwise Logistic Regression Analysis.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The Backward Stepwise regression is an exploratory analysis procedure,
    ///   where the analysis begins with a full (saturated) model and at each step
    ///   variables are eliminated from the model in a iterative fashion.</para>
    /// <para>
    ///   Significance tests are performed after each removal to track which of
    ///   the variables can be discarded safely without implying in degradation.</para>
    /// <para>
    ///   When no more variables can be removed from the model without causing
    ///   a significative loss in the model likelihood, the method can stop.</para>  
    /// </remarks>
    /// 
    [Serializable]
    public class StepwiseLogisticRegressionAnalysis : IRegressionAnalysis
    {

        private double[][] inputData;
        private double[] outputData;

        private string[] inputNames;
        private string outputName;

        private double[,] source;
        private double[] result;
        private int[] resultVariables;


        private StepwiseLogisticRegressionModel currentModel;
        private StepwiseLogisticRegressionModelCollection nestedModelCollection;
        private double fullLikelihood;

        private double threshold = 0.15;

        // Fitting parameters
        private int maxIterations = 100;
        private double limit = 10e-4;


        //---------------------------------------------


        #region Constructors
        /// <summary>
        ///   Constructs a Stepwise Logistic Regression Analysis.
        /// </summary>
        /// 
        /// <param name="inputs">The input data for the analysis.</param>
        /// <param name="outputs">The output data for the analysis.</param>
        /// <param name="inputNames">The names for the input variables.</param>
        /// <param name="outputName">The name for the output variable.</param>
        /// 
        public StepwiseLogisticRegressionAnalysis(double[][] inputs, double[] outputs, String[] inputNames, String outputName)
        {
            // Initial argument checking
            if (inputs == null) throw new ArgumentNullException("inputs");
            if (outputs == null) throw new ArgumentNullException("outputs");

            if (inputs.Length != outputs.Length)
                throw new ArgumentException("The number of rows in the input array must match the number of given outputs.");


            this.inputData = inputs;
            this.outputData = outputs;

            this.inputNames = inputNames;
            this.outputName = outputName;

            this.source = inputs.ToMatrix();
        }
        #endregion


        //---------------------------------------------


        #region Properties
        /// <summary>
        ///   Source data used in the analysis.
        /// </summary>
        /// 
        public double[,] Source
        {
            get { return source; }
        }

        /// <summary>
        ///   Gets the the dependent variable value
        ///   for each of the source input points.
        /// </summary>
        /// 
        public double[] Outputs
        {
            get { return outputData; }
        }

        /// <summary>
        ///   Gets the resulting probabilities obtained
        ///   by the most likely logistic regression model.
        /// </summary>
        /// 
        public double[] Result
        {
            get { return result; }
        }

        /// <summary>
        ///   Gets the current nested model.
        /// </summary>
        /// 
        public StepwiseLogisticRegressionModel Current
        {
            get { return this.currentModel; }
        }

        /// <summary>
        ///   Gets the collection of nested models obtained after 
        ///   a step of the backward stepwise procedure.
        /// </summary>
        /// 
        public StepwiseLogisticRegressionModelCollection Nested
        {
            get { return nestedModelCollection; }
        }

        /// <summary>
        ///   Gets the name of the input variables.
        /// </summary>
        /// 
        public String[] Inputs
        {
            get { return this.inputNames; }
        }

        /// <summary>
        ///   Gets the name of the output variables.
        /// </summary>
        /// 
        public String Output
        {
            get { return this.outputName; }
        }

        /// <summary>
        ///   Gets or sets the significance threshold used to
        ///   determine if a nested model is significant or not.
        /// </summary>
        /// 
        public double Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }


        /// <summary>
        ///   Gets the final set of input variables indices
        ///   as selected by the stepwise procedure.
        /// </summary>
        /// 
        public int[] Variables
        {
            get { return this.resultVariables; }
        }
        #endregion


        //---------------------------------------------


        /// <summary>
        ///   Computes the Stepwise Logistic Regression.
        /// </summary>
        /// 
        public void Compute()
        {
            int changed;
            do
            {
                changed = DoStep();

            } while (changed != -1);

            resultVariables = currentModel.Variables;
            result = currentModel.Regression.Compute(inputData);
        }

        /// <summary>
        ///   Computes one step of the Stepwise Logistic Regression Analysis.
        /// </summary>
        /// <returns>
        ///   Returns the index of the variable discarded in the step or -1
        ///   in case no variable could be discarded.
        /// </returns>
        /// 
        public int DoStep()
        {
            // Check if we are performing the first step
            if (currentModel == null)
            {
                // This is the first step. We should create the full model.
                int inputCount = inputData[0].Length;
                LogisticRegression regression = new LogisticRegression(inputCount);
                int[] variables = Matrix.Indices(0, inputCount);
                fit(regression, inputData, outputData);
                ChiSquareTest test = regression.ChiSquare(inputData, outputData);
                fullLikelihood = regression.GetLogLikelihood(inputData, outputData);

                if (Double.IsNaN(fullLikelihood))
                {
                    throw new ConvergenceException(
                        "Perfect separation detected. Please rethink the use of logistic regression.");
                }

                currentModel = new StepwiseLogisticRegressionModel(this, regression, variables, test);
            }


            // Verify first if a variable reduction is possible
            if (currentModel.Regression.Inputs == 1)
                return -1; // cannot reduce further


            // Now go and create the diminished nested models
            var nestedModels = new StepwiseLogisticRegressionModel[currentModel.Regression.Inputs];
            for (int i = 0; i < nestedModels.Length; i++)
            {
                // Create a diminished nested model without the current variable
                LogisticRegression regression = new LogisticRegression(currentModel.Regression.Inputs - 1);
                int[] variables = currentModel.Variables.RemoveAt(i);
                double[][] subset = inputData.Submatrix(0, inputData.Length - 1, variables);
                fit(regression, subset, outputData);

                // Check the significance of the nested model
                double logLikelihood = regression.GetLogLikelihood(subset, outputData);
                double ratio = 2.0 * (fullLikelihood - logLikelihood);
                ChiSquareTest test = new ChiSquareTest(ratio, inputNames.Length - variables.Length, threshold);

                // Store the nested model
                nestedModels[i] = new StepwiseLogisticRegressionModel(this, regression, variables, test);
            }

            // Select the model with the highest p-value
            double pmax = 0; int imax = -1;
            for (int i = 0; i < nestedModels.Length; i++)
            {
                if (nestedModels[i].ChiSquare.PValue >= pmax)
                {
                    imax = i;
                    pmax = nestedModels[i].ChiSquare.PValue;
                }
            }

            // Create the read-only nested model collection
            this.nestedModelCollection = new StepwiseLogisticRegressionModelCollection(nestedModels);


            // If the model with highest p-value is not significant,
            if (imax >= 0 && pmax > threshold)
            {
                // Then this means the variable can be safely discarded from the full model
                int removed = currentModel.Variables[imax];

                // Our diminished nested model will become our next full model.
                this.currentModel = nestedModels[imax];

                // Finally, return the index of the removed variable
                return removed;
            }
            else
            {
                // Else we can not safely remove any variable from the model.
                return -1;
            }
        }


        /// <summary>
        ///   Fits a logistic regression model to data until convergence.
        /// </summary>
        /// 
        private bool fit(LogisticRegression regression, double[][] input, double[] output)
        {
            IterativeReweightedLeastSquares irls =
                new IterativeReweightedLeastSquares(regression);

            double delta;
            int iteration = 0;

            do // learning iterations until convergence
            {
                delta = irls.Run(input, output);
                iteration++;

            } while (delta > limit && iteration < maxIterations);

            // Check if the full model has converged
            return iteration <= maxIterations;
        }

    }

    /// <summary>
    ///   Stepwise Logistic Regression Nested Model.
    /// </summary>
    /// 
    [Serializable]
    public class StepwiseLogisticRegressionModel
    {
        /// <summary>
        ///   Gets the Stepwise Logistic Regression Analysis
        ///   from which this model belongs to.
        /// </summary>
        /// 
        public StepwiseLogisticRegressionAnalysis Analysis { get; private set; }

        /// <summary>
        ///   Gets the regression model.
        /// </summary>
        /// 
        public LogisticRegression Regression { get; private set; }

        /// <summary>
        ///   Gets the subset of the original variables used by the model.
        /// </summary>
        /// 
        public int[] Variables { get; private set; }

        /// <summary>
        ///   Gets the Chi-Square Likelihood Ratio test for the model.
        /// </summary>
        /// 
        public ChiSquareTest ChiSquare { get; private set; }

        /// <summary>
        ///   Gets the subset of the original variables used by the model.
        /// </summary>
        /// 
        public string[] Names
        {
            get { return Analysis.Inputs.Submatrix(Variables); }
        }

        /// <summary>
        ///   Constructs a new Logistic regression model.
        /// </summary>
        /// 
        internal StepwiseLogisticRegressionModel(StepwiseLogisticRegressionAnalysis analysis, LogisticRegression regression,
            int[] variables, ChiSquareTest test)
        {
            this.Analysis = analysis;
            this.Regression = regression;
            this.Variables = variables;
            this.ChiSquare = test;
        }

    }

    /// <summary>
    ///   Stepwise Logistic Regression Nested Model collection.
    ///   This class cannot be instantiated.
    /// </summary>
    /// 
    [Serializable]
    public class StepwiseLogisticRegressionModelCollection :
        ReadOnlyCollection<StepwiseLogisticRegressionModel>
    {
        internal StepwiseLogisticRegressionModelCollection(StepwiseLogisticRegressionModel[] models)
            : base(models) { }
    }
}
