// Accord Statistics Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//

namespace Accord.Statistics.Models.Markov.Learning
{
    using Accord.Math;
    using Accord.Statistics.Distributions.Univariate;

    /// <summary>
    ///   Discrete-density hidden Markov Sequence Classifier learning algorithm.
    /// </summary>
    /// 
    /// <example>
    ///   <code>
    ///   // Declare some testing data
    ///   int[][] inputs = new int[][]
    ///   {
    ///       new int[] { 0,1,1,0 },   // Class 0
    ///       new int[] { 0,0,1,0 },   // Class 0
    ///       new int[] { 0,1,1,1,0 }, // Class 0
    ///       new int[] { 0,1,0 },     // Class 0
    ///   
    ///       new int[] { 1,0,0,1 },   // Class 1
    ///       new int[] { 1,1,0,1 },   // Class 1
    ///       new int[] { 1,0,0,0,1 }, // Class 1
    ///       new int[] { 1,0,1 },     // Class 1
    ///   };
    ///   
    ///   int[] outputs = new int[]
    ///   {
    ///       0,0,0,0, // First four sequences are of class 0
    ///       1,1,1,1, // Last four sequences are of class 1
    ///   };
    ///   
    ///   
    ///   // We are trying to predict two different classes
    ///   int classes = 2;
    ///
    ///   // Each sequence may have up to two symbols (0 or 1)
    ///   int symbols = 2;
    ///
    ///   // Nested models will have two states each
    ///   int[] states = new int[] { 2, 2 };
    ///
    ///   // Creates a new Hidden Markov Model Sequence Classifier with the given parameters
    ///   SequenceClassifier classifier = new SequenceClassifier(classes, states, symbols);
    ///   
    ///   // Create a new learning algorithm to train the sequence classifier
    ///   var teacher = new SequenceClassifierLearning(classifier,
    ///   
    ///       // Train each model until the log-likelihood changes less than 0.001
    ///       modelIndex => new BaumWelchLearning(classifier.Models[modelIndex])
    ///       {
    ///           Tolerance = 0.001,
    ///           Iterations = 0
    ///       }
    ///   );
    ///   
    ///   // Train the sequence classifier using the algorithm
    ///   double likelihood = teacher.Run(inputs, outputs);
    ///   
    ///   </code>
    /// </example>
    /// 
    public class SequenceClassifierLearning :
        SequenceClassifierLearningBase<SequenceClassifier, HiddenMarkovModel>
    {

        private int smoothingKernelSize = 3;
        private double smoothingSigma = 1.0;
        private double[] gaussianKernel;

        /// <summary>
        ///   Gets or sets the smoothing kernel's sigma
        ///   for the threshold model.
        /// </summary>
        /// <value>The smoothing kernel's sigma.</value>
        public double Smoothing
        {
            get { return smoothingSigma; }
            set
            {
                smoothingSigma = value;
                createSmoothingKernel();
            }
        }

        private void createSmoothingKernel()
        {
            AForge.Math.Gaussian g = new AForge.Math.Gaussian(smoothingSigma);
            gaussianKernel = g.Kernel(smoothingKernelSize);

            // Normalize
            double norm = gaussianKernel.Euclidean();
            gaussianKernel = gaussianKernel.Divide(norm);
        }

        /// <summary>
        ///   Creates a new instance of the learning algorithm for a given 
        ///   Markov sequence classifier using the specified configuration
        ///   function.
        /// </summary>
        public SequenceClassifierLearning(SequenceClassifier classifier,
            ClassifierLearningAlgorithmConfiguration algorithm)
            : base(classifier, algorithm)
        {
            createSmoothingKernel();
        }


        /// <summary>
        ///   Trains each model to recognize each of the output labels.
        /// </summary>
        /// <returns>The sum log-likelihood for all models after training.</returns>
        public double Run(int[][] inputs, int[] outputs)
        {
            return base.Run<int[]>(inputs, outputs);
        }


        /// <summary>
        ///   Creates a new <see cref="Threshold">threshold model</see>
        ///   for the current set of Markov models in this sequence classifier.
        /// </summary>
        /// <returns>
        ///   A <see cref="Threshold">threshold Markov model</see>.
        /// </returns>
        public override HiddenMarkovModel Threshold()
        {
            HiddenMarkovModel[] Models = base.Classifier.Models;

            int states = 0;
            int symbols = Models[0].Symbols;

            // Get the total number of states
            for (int i = 0; i < Models.Length; i++)
                states += Models[i].States;

            // Create the transition and emission matrices
            double[,] transition = new double[states, states];
            double[,] emissions = new double[states, symbols];
            double[] initial = new double[states];

            for (int i = 0, m = 0; i < Models.Length; i++)
            {
                for (int j = 0; j < Models[i].States; j++)
                {
                    for (int k = 0; k < Models[i].States; k++)
                    {
                        if (j != k)
                            transition[j + m, k + m] = (1.0 - Models[i].Transitions[j, k]) / (states - 1.0);
                        else transition[j + m, k + m] = Models[i].Transitions[j, k];
                    }
                    emissions.SetRow(j + m, Models[i].Emissions.GetRow(j));
                }

                initial[m] = 1.0 / Models.Length;
                m += Models[i].States;
            }

            
            if (smoothingSigma > 0)
            {
                // Gaussian smoothing
                for (int i = 0; i < states; i++)
                {
                    double[] e = emissions.GetRow(i);
                    double[] g = e.Convolve(gaussianKernel, true);
                    g = g.Divide(g.Sum()); // Make probabilities
                    emissions.SetRow(i, g);
                }
            }

            return new HiddenMarkovModel(transition, emissions, initial) { Tag = "Threshold" };
        }
    }
}
