// Accord Statistics Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Models.Markov.Learning
{
    using System;
    using Accord.Math;

    /// <summary>
    ///   Configuration function delegate for Sequence Classifier Learning algorithms.
    /// </summary>
    public delegate IUnsupervisedLearning ClassifierLearningAlgorithmConfiguration(int modelIndex);


    /// <summary>
    ///   Abstract base class for Sequence Classifier learning algorithms.
    /// </summary>
    /// 
    public abstract class SequenceClassifierLearningBase<TClassifier, TModel> 
        where TClassifier : SequenceClassifierBase<TModel> 
        where TModel : IHiddenMarkovModel
    {

        private TClassifier classifier;
        private bool updateThresholdModel = false;
        private ClassifierLearningAlgorithmConfiguration algorithm;

        /// <summary>
        ///   Gets the classifier being trained by this instance.
        /// </summary>
        /// <value>The classifier being trained by this instance.</value>
        /// 
        public TClassifier Classifier
        {
            get { return classifier; }
        }

        /// <summary>
        ///   Gets or sets the configuration function specifying which
        ///   training algorithm should be used for each of the models
        ///   in the hidden Markov model set.
        /// </summary>
        /// 
        public ClassifierLearningAlgorithmConfiguration Algorithm
        {
            get { return algorithm; }
            set { algorithm = value; }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether a threshold
        ///   model should be created after training to support rejection.
        /// </summary>
        /// <value><c>true</c> to update the threshold model after training;
        /// otherwise, <c>false</c>.</value>
        /// 
        public bool Rejection
        {
            get { return updateThresholdModel; }
            set { updateThresholdModel = value; }
        }


        /// <summary>
        ///   Creates a new instance of the learning algorithm for a given 
        ///   Markov sequence classifier using the specified configuration
        ///   function.
        /// </summary>
        /// 
        protected SequenceClassifierLearningBase(TClassifier classifier,
            ClassifierLearningAlgorithmConfiguration algorithm)
        {
            this.classifier = classifier;
            this.algorithm = algorithm;
        }

        

        /// <summary>
        ///   Trains each model to recognize each of the output labels.
        /// </summary>
        /// <returns>The sum log-likelihood for all models after training.</returns>
        /// 
        protected double Run<T>(T[] inputs, int[] outputs)
        {
            int classes = classifier.Classes;
            double[] logLikelihood = new double[classes];

            // For each model,
#if !DEBUG
            AForge.Parallel.For(0, classes, i =>
#else
            for (int i = 0; i < classes; i++)
#endif
            {
                // Select the input/output set corresponding
                //  to the model's specialization class
                int[] inx = outputs.Find(y => y == i);
                T[] observations = inputs.Submatrix(inx);

                if (observations.Length > 0)
                {
                    // Create and configure the learning algorithm
                    IUnsupervisedLearning teacher = algorithm(i);

                    // Train the current model in the input/output subset
                    logLikelihood[i] = teacher.Run(observations as Array[]);
                }
            }
#if !DEBUG
            );
#endif

            if (updateThresholdModel)
                classifier.Threshold = Threshold();

            // Returns the sum log-likelihood for all models.
            return logLikelihood.Sum();
        }

        /// <summary>
        ///   Creates a new <see cref="Threshold">threshold model</see>
        ///   for the current set of Markov models in this sequence classifier.
        /// </summary>
        /// <returns>A <see cref="Threshold">threshold Markov model</see>.</returns>
        /// 
        public abstract TModel Threshold(); 

    }
}
