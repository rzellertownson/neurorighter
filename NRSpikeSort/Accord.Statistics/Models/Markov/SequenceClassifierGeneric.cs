// Accord Statistics Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Models.Markov
{
    using System;
    using Accord.Statistics.Distributions;
    using Accord.Statistics.Models.Markov.Topology;

    /// <summary>
    ///   Arbitrary-density Hidden Markov Model Set for Sequence Classification.
    /// </summary>
    /// 
    /// <remarks>
    ///   This class uses a set of hidden Markov models to classify sequences of
    ///   real (double-precision floating point) numbers or arrays of those numbers.
    ///   Each model will try to learn and recognize each of the different output classes.
    /// </remarks>
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
    ///   var teacher = new SequenceClassifierLearning&lt;NormalDistribution&gt;(classifier,
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
    [Serializable]
    public class SequenceClassifier<TDistribution> : 
        SequenceClassifierBase<HiddenMarkovModel<TDistribution>>,
        ISequenceClassifier where TDistribution : IDistribution
    {

        /// <summary>
        ///   Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        public SequenceClassifier(int classes, ITopology topology, TDistribution initial)
            : base(classes)
        {
            for (int i = 0; i < classes; i++)
                Models[i] = new HiddenMarkovModel<TDistribution>(topology, initial);
        }

        /// <summary>
        ///   Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        public SequenceClassifier(int classes, ITopology topology, TDistribution initial, string[] names)
            : base(classes)
        {
            for (int i = 0; i < classes; i++)
                Models[i] = new HiddenMarkovModel<TDistribution>(topology, initial) { Tag = names[i] };
        }

        /// <summary>
        ///   Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        public SequenceClassifier(int classes, ITopology[] topology, TDistribution[] initial, string[] names)
            : base(classes)
        {
            for (int i = 0; i < classes; i++)
                Models[i] = new HiddenMarkovModel<TDistribution>(topology[i], initial[i]) { Tag = names[i] };
        }

        /// <summary>
        ///   Creates a new Sequence Classifier with the given number of classes.
        /// </summary>
        public SequenceClassifier(HiddenMarkovModel<TDistribution>[] models)
            : base(models)
        {
        }


        /// <summary>
        ///   Computes the most likely class for a given sequence.
        /// </summary>
        public new int Compute(Array sequence)
        {
            return base.Compute(sequence);
        }

        /// <summary>
        ///   Computes the most likely class for a given sequence.
        /// </summary>
        public new int Compute(Array sequence, out double likelihood)
        {
            return base.Compute(sequence, out likelihood);
        }

        /// <summary>
        ///   Computes the most likely class for a given sequence.
        /// </summary>
        public new int Compute(Array sequence, out double[] likelihoods)
        {
            return base.Compute(sequence, out likelihoods);
        }

    }
}
