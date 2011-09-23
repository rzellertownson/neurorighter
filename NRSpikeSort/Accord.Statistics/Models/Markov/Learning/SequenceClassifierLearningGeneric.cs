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
    using Accord.Statistics.Distributions;

    /// <summary>
    ///   Arbitrary-density hidden Markov Sequence Classifier learning algorithm.
    /// </summary>
    /// 
    /// <example>
    ///   <para>
    ///   The following example creates a continuous-density hidden Markov model sequence
    ///   classifier to recognize two classes of univariate sequence of observations.</para>
    ///   
    ///   <code>
    ///   // Create a Continuous density Hidden Markov Model Sequence Classifier
    ///   // to detect a univariate sequence and the same sequence backwards.
    ///   double[][] sequences = new double[][] 
    ///   {
    ///       new double[] { 0,1,2,3,4 }, // This is the first  sequence with label = 0
    ///       new double[] { 4,3,2,1,0 }, // This is the second sequence with label = 1
    ///   };
    ///   
    ///   // Labels for the sequences
    ///   int[] labels = { 0, 1 };
    ///
    ///   // Creates a new Continuous-density Hidden Markov Model Sequence Classifier
    ///   //  containing 2 hidden Markov Models with 2 states and an underlying Normal
    ///   //  distribution as the continuous probability density.
    ///   NormalDistribution density = new NormalDistribution();
    ///   var classifier = new SequenceClassifier&lt;NormalDistribution&gt;(2, new Ergodic(2), density);
    ///
    ///   // Create a new learning algorithm to train the sequence classifier
    ///   var teacher = new SequenceClassifierLearning&lt;NormalDistribution&gt;(classifier,
    ///
    ///       // Train each model until the log-likelihood changes less than 0.001
    ///       modelIndex => new BaumWelchLearning&lt;NormalDistribution&gt;(classifier.Models[modelIndex])
    ///       {
    ///           Tolerance = 0.0001,
    ///           Iterations = 0
    ///       }
    ///   );
    ///   
    ///   // Train the sequence classifier using the algorithm
    ///   teacher.Run(sequences, labels);
    ///   
    ///   
    ///   // Calculate the probability that the given
    ///   //  sequences originated from the model
    ///   double likelihood;
    ///   
    ///   // Try to classify the first sequence (output should be 0)
    ///   int c1 = classifier.Compute(sequences[0], out likelihood);
    ///   
    ///   // Try to classify the second sequence (output should be 1)
    ///   int c2 = classifier.Compute(sequences[1], out likelihood);
    ///   </code>
    ///   
    ///   <para>
    ///   The following example creates a continuous-density hidden Markov model sequence
    ///   classifier to recognize two classes of multivariate sequence of observations.</para>
    ///   
    ///   <code>
    ///   // Create a Continuous density Hidden Markov Model Sequence Classifier
    ///   // to detect a multivariate sequence and the same sequence backwards.
    ///   double[][][] sequences = new double[][][]
    ///   {
    ///       new double[][] 
    ///       { 
    ///           // This is the first  sequence with label = 0
    ///           new double[] { 0 },
    ///           new double[] { 1 },
    ///           new double[] { 2 },
    ///           new double[] { 3 },
    ///           new double[] { 4 },
    ///       }, 
    ///       
    ///       new double[][]
    ///       {
    ///           // This is the second sequence with label = 1
    ///           new double[] { 4 },
    ///           new double[] { 3 },
    ///           new double[] { 2 },
    ///           new double[] { 1 },
    ///           new double[] { 0 },
    ///       }
    ///   };
    ///   
    ///   // Labels for the sequences
    ///   int[] labels = { 0, 1 };
    ///   
    ///   // Creates a sequence classifier containing 2 hidden Markov Models
    ///   //  with 2 states and an underlying Normal distribution as density.
    ///   MultivariateNormalDistribution density = new MultivariateNormalDistribution(1);
    ///   var classifier = new SequenceClassifier&lt;MultivariateNormalDistribution&gt;(2, new Ergodic(2), density);
    ///   
    ///   // Configure the learning algorithms to train the sequence classifier
    ///   var teacher = new SequenceClassifierLearning&lt;MultivariateNormalDistribution&gt;(classifier,
    ///
    ///      // Train each model until the log-likelihood changes less than 0.001
    ///      modelIndex => new BaumWelchLearning&lt;NormalDistribution&gt;(classifier.Models[modelIndex])
    ///      {
    ///           Tolerance = 0.0001,
    ///           Iterations = 0
    ///      {
    ///   );
    ///   
    ///   // Train the sequence classifier using the algorithm
    ///   double logLikelihood = teacher.Run(sequences, labels);
    ///   
    ///    
    ///   // Calculate the probability that the given
    ///   //  sequences originated from the model
    ///   double likelihood1, likelihood2;
    ///   
    ///   // Try to classify the first sequence (output should be 0)
    ///   int c1 = classifier.Compute(sequences[0], out likelihood1);
    ///
    ///   // Try to classify the second sequence (output should be 1)
    ///   int c2 = classifier.Compute(sequences[1], out likelihood2);
    ///   </code>
    /// </example>
    /// 
    public class SequenceClassifierLearning<TDistribution> : 
        SequenceClassifierLearningBase<SequenceClassifier<TDistribution>,
        HiddenMarkovModel<TDistribution>> 
        where TDistribution : IDistribution
    {


        /// <summary>
        ///   Creates a new instance of the learning algorithm for a given 
        ///   Markov sequence classifier using the specified configuration
        ///   function.
        /// </summary>
        /// 
        public SequenceClassifierLearning(SequenceClassifier<TDistribution> classifier,
            ClassifierLearningAlgorithmConfiguration algorithm)
            : base(classifier, algorithm)
        {
        }


        /// <summary>
        ///   Trains each model to recognize each of the output labels.
        /// </summary>
        /// <returns>The sum log-likelihood for all models after training.</returns>
        /// 
        public double Run(Array[] inputs, int[] outputs)
        {
            return base.Run<Array>(inputs, outputs);
        }

        /// <summary>
        ///   Creates a new <see cref="Threshold">threshold model</see>
        ///   for the current set of Markov models in this sequence classifier.
        /// </summary>
        /// <returns>
        ///   A <see cref="Threshold">threshold Markov model</see>.
        /// </returns>
        /// 
        public override HiddenMarkovModel<TDistribution> Threshold()
        {
            HiddenMarkovModel<TDistribution>[] Models = base.Classifier.Models;

            int states = 0;

            // Get the total number of states
            for (int i = 0; i < Models.Length; i++)
                states += Models[i].States;

            // Create the transition and emission matrices
            double[,] transition = new double[states, states];
            TDistribution[] emissions = new TDistribution[states];
            double[] initial = new double[states];

            for (int i = 0, m = 0; i < Models.Length; i++)
            {
                for (int j = 0; j < Models[i].States; j++)
                {
                    for (int k = 0; k < Models[i].States; k++)
                    {
                        if (j != k)
                            transition[j + m, k + m] = (1 - Models[i].Transitions[j, k]) / (states - 1.0);
                        else transition[j + m, k + m] = Models[i].Transitions[j, k];
                    }
                    emissions[j + m] = Models[i].Emissions[j];
                }

                initial[m] = 1.0 / Models.Length;
                m += Models[i].States;
            }


            return new HiddenMarkovModel<TDistribution>(transition, emissions, initial) { Tag = "Threshold" };
        }


    }
}
