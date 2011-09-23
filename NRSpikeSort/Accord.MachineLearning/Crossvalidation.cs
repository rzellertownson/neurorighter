// Accord Machine Learning Library
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

namespace Accord.MachineLearning
{
    using System;
    using System.Collections.Generic;
    using Accord.Math;

    /// <summary>
    ///   Fitting function delegate.
    /// </summary>
    /// <param name="trainingSamples">
    ///   The sample indexes to be used as training samples in
    ///   the model fitting procedure.
    /// </param>
    /// <param name="validationSamples">
    ///   The sample indexes to be used as validation samples in
    ///   the model fitting procedure.
    /// </param>
    /// <remarks>
    ///   The fitting function is called during the Cross-validation
    ///   procedure to fit a model with the given set of samples for
    ///   training and validation.
    /// </remarks>
    /// 
    public delegate CrossvalidationInfo<TModel> CrossvalidationFittingFunction<TModel>(int[] trainingSamples, int[] validationSamples);

    /// <summary>
    ///   k-Fold Cross-Validation.
    /// </summary>
    /// <remarks>
    /// <para>
    ///   Cross-validation is a technique for estimating the performance of a predictive
    ///   model. It can be used to measure how the results of a statistical analysis will
    ///   generalize to an independent data set. It is mainly used in settings where the
    ///   goal is prediction, and one wants to estimate how accurately a predictive model
    ///   will perform in practice.</para>
    /// <para>
    ///   One round of cross-validation involves partitioning a sample of data into
    ///   complementary subsets, performing the analysis on one subset (called the
    ///   training set), and validating the analysis on the other subset (called the
    ///   validation set or testing set). To reduce variability, multiple rounds of 
    ///   cross-validation are performed using different partitions, and the validation 
    ///   results are averaged over the rounds.</para> 
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Cross-validation_(statistics)">
    ///       Wikipedia, The Free Encyclopedia. Cross-validation (statistics). Available on:
    ///       http://en.wikipedia.org/wiki/Cross-validation_(statistics) </a></description></item>
    ///   </list></para> 
    /// </remarks>
    /// 
    /// <example>
    ///   <code>
    ///   //Example binary data
    ///   double[][] data =
    ///   {
    ///        new double[] { -1, -1 }, new double[] {  1, -1 },
    ///        new double[] { -1,  1 }, new double[] {  1,  1 },
    ///        new double[] { -1, -1 }, new double[] {  1, -1 },
    ///        new double[] { -1,  1 }, new double[] {  1,  1 },
    ///        new double[] { -1, -1 }, new double[] {  1, -1 },
    ///        new double[] { -1,  1 }, new double[] {  1,  1 },
    ///        new double[] { -1, -1 }, new double[] {  1, -1 },
    ///        new double[] { -1,  1 }, new double[] {  1,  1 },
    ///    };
    ///
    ///    int[] xor = // result of xor
    ///    {
    ///        -1,  1,
    ///         1, -1,
    ///        -1,  1,
    ///         1, -1,
    ///        -1,  1,
    ///         1, -1,
    ///        -1,  1,
    ///         1, -1,
    ///    };
    ///
    ///
    ///    // Create a new Cross-validation algorithm passing the data set size and the number of folds
    ///    var crossvalidation = new Crossvalidation&lt;KernelSupportVectorMachine>(data.Length, 3);
    ///
    ///    // Define a fitting function using Support Vector Machines
    ///    crossvalidation.Fitting = delegate(int[] trainingSet, int[] validationSet)
    ///    {
    ///        // The trainingSet and validationSet arguments specifies the
    ///        // indices of the original data set to be used as training and
    ///        // validation sets, respectively.
    ///        double[][] trainingInputs = data.Submatrix(trainingSet);
    ///        int[] trainingOutputs = xor.Submatrix(trainingSet);
    ///
    ///        double[][] validationInputs = data.Submatrix(validationSet);
    ///        int[] validationOutputs = xor.Submatrix(validationSet);
    ///
    ///        // Create a Kernel Support Vector Machine to operate on this set
    ///        var svm = new KernelSupportVectorMachine(new Polynomial(2), 2);
    ///
    ///        // Create a training algorithm and learn this set
    ///        var smo = new SequentialMinimalOptimization(svm, trainingInputs, trainingOutputs);
    ///
    ///        double trainingError = smo.Run();
    ///        double validationError = smo.ComputeError(validationInputs, validationOutputs);
    ///
    ///        // Return a new information structure containing the model and the errors achieved.
    ///        return new CrossvalidationInfo&lt;KernelSupportVectorMachine>(svm, trainingError, validationError);
    ///    };
    ///
    /// 
    ///    // Compute the cross-validation
    ///    crossvalidation.Compute();
    ///
    ///    // Get the average training and validation errors
    ///    double errorTraining   = crossvalidation.TrainingError;
    ///    double errorValidation = crossvalidation.ValidationError;
    ///   </code>
    /// </example>
    /// 
    public class Crossvalidation<TModel>
    {

        private int[][] folds;
        private CrossvalidationInfo<TModel>[] models;



        /// <summary>
        ///   Gets or sets the model fitting function.
        /// </summary>
        /// <remarks>
        ///   The fitting function should accept an array of integers containing the
        ///   indexes for the training samples, an array of integers containing the
        ///   indexes for the validation samples and should return information about
        ///   the model fitted using those two subsets of the available data.
        /// </remarks>
        /// 
        public CrossvalidationFittingFunction<TModel> Fitting { get; set; }

        /// <summary>
        ///   Gets the models created for each fold of the cross validation.
        /// </summary>
        /// 
        public CrossvalidationInfo<TModel>[] Models { get { return models; } }

        /// <summary>
        ///   Gets the average validation error.
        /// </summary>
        /// 
        public double ValidationError { get; private set; }

        /// <summary>
        ///   Gets the average training error.
        /// </summary>
        /// 
        public double TrainingError { get; private set; }

        /// <summary>
        ///   Gets the array of indexes contained in each fold.
        /// </summary>
        /// 
        public int[][] Folds { get { return folds; } }

        /// <summary>
        ///   Gets the number of folds in the k-fold cross validation.
        /// </summary>
        /// 
        public int K { get { return folds.Length; } }


        /// <summary>
        ///   Creates a new k-fold cross-validation algorithm.
        /// </summary>
        /// 
        /// <param name="size">The complete dataset for training and testing.</param>
        /// 
        public Crossvalidation(int size)
            : this(size, 10)
        {
        }

        /// <summary>
        ///   Creates a new k-fold cross-validation algorithm.
        /// </summary>
        /// 
        /// <param name="size">The complete dataset for training and testing.</param>
        /// <param name="folds">The number of folds, usually denoted as <c>k</c> (default is 10).</param>
        /// 
        public Crossvalidation(int size, int folds)
        {
            if (folds > size)
                throw new ArgumentException("The number of folds can not exceed the total number of samples in the data set", "folds");


            this.folds = new int[folds][];

            // Create the index vector
            int[] idx = new int[size];

            float n = (float)folds / size;
            for (int i = 0; i < size; i++)
                idx[i] = (int)System.Math.Ceiling((i + 0.9) * n) - 1;

            // Shuffle the indices vector
            Statistics.Tools.Shuffle(idx);

            // Create foldings
            for (int i = 0; i < folds; i++)
                this.folds[i] = idx.Find(x => x == i);

            // Create the k nested models structures
            models = new CrossvalidationInfo<TModel>[folds];
        }


        /// <summary>
        ///   Computes the cross validation algorithm.
        /// </summary>
        /// 
        public void Compute()
        {
            for (int i = 0; i < folds.Length; i++)
            {

                List<int[]> list = new List<int[]>();
                for (int j = 0; j < folds.Length; j++)
                    if (i != j) list.Add(folds[j]);

                // Create training set
                int[] trainingSet = Matrix.Combine(list.ToArray());

                // Select validation set
                int[] validationSet = folds[i];

                // Fit and evaluate the model
                models[i] = Fitting(trainingSet, validationSet);
            }

            // Return average training and validation error
            double vsum = 0, tsum = 0;
            for (int i = 0; i < folds.Length; i++)
            {
                tsum += models[i].TrainingError;
                vsum += models[i].ValidationError;
            }
            this.TrainingError = tsum / folds.Length;
            this.ValidationError = vsum / folds.Length;
        }

    }

    /// <summary>
    ///   Information class to store the training and validation errors of a model. 
    /// </summary>
    /// 
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// 
    public class CrossvalidationInfo<TModel>
    {
        /// <summary>
        ///   Gets the model.
        /// </summary>
        /// 
        public TModel Model { get; private set; }

        /// <summary>
        ///   Gets the validation error for the model.
        /// </summary>
        /// 
        public double ValidationError { get; private set; }

        /// <summary>
        ///   Gets the training error for the model.
        /// </summary>
        /// 
        public double TrainingError { get; private set; }

        /// <summary>
        ///   Gets or sets a tag for user-defined information.
        /// </summary>
        /// 
        private object Tag { get; set; }


        /// <summary>
        ///   Creates a new CrossvalidationInfo class.
        /// </summary>
        /// 
        /// <param name="model">The fitted model.</param>
        /// <param name="trainingError">The training error for the model.</param>
        /// <param name="validationError">The validation error for the model.</param>
        /// 
        public CrossvalidationInfo(TModel model, double trainingError, double validationError)
        {
            this.Model = model;
            this.TrainingError = trainingError;
            this.ValidationError = validationError;
        }

    }
}
