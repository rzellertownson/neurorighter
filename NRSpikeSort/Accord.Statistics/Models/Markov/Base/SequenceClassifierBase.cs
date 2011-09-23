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
    using Accord.Math;

    /// <summary>
    ///   Base class for (HMM) Sequence Classifiers. This class cannot
    ///   be instantiated.
    /// </summary>
    /// 
    [Serializable]
    public abstract class SequenceClassifierBase<TModel> where TModel : IHiddenMarkovModel
    {

        private TModel threshold;
        private TModel[] models;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SequenceClassifierBase&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="classes">The number of classes in the classification problem.</param>
        /// 
        protected SequenceClassifierBase(int classes)
        {
            models = new TModel[classes];
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SequenceClassifierBase&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="models">The models specializing in each of the classes of the classification problem.</param>
        /// 
        protected SequenceClassifierBase(TModel[] models)
        {
            this.models = models;
        }

        /// <summary>
        ///   Gets or sets the threshold model.
        /// </summary>
        /// <remarks>
        /// <para>
        ///   For gesture spotting, Lee and Kim introduced a threshold model which is
        ///   composed of parts of the models in a hidden Markov sequence classifier.</para>
        /// <para>
        ///   The threshold model acts as a baseline for decision rejection. If none of
        ///   the classifiers is able to produce a higher likelihood than the threshold
        ///   model, the decision is rejected.</para>
        /// <para>
        ///   In the original Lee and Kim publication, the threshold model is constructed
        ///   by creating a fully connected ergodic model by removing all outgoing transitions
        ///   of states in all gesture models and fully connecting those states.</para>
        /// <para>
        ///   References:
        ///   <list type="bullet">
        ///     <item><description>
        ///        H. Lee, J. Kim, An HMM-based threshold model approach for gesture
        ///        recognition, IEEE Trans. Pattern Anal. Mach. Intell. 21 (10) (1999)
        ///        961–973.</description></item>
        ///   </list></para>
        /// </remarks>
        /// 
        public TModel Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }

        /// <summary>
        ///   Gets the collection of models specialized in each class
        ///   of the sequence classification problem.
        /// </summary>
        /// 
        public TModel[] Models
        {
            get { return models; }
        }

        /// <summary>
        ///   Gets the <see cref="IHiddenMarkovModel">Hidden Markov
        ///   Model</see> implementation responsible for recognizing
        ///   each of the classes given the desired class label.
        /// </summary>
        /// <param name="label">The class label of the model to get.</param>
        /// 
        public TModel this[int label]
        {
            get { return models[label]; }
        }

        /// <summary>
        ///   Gets the number of classes which can be recognized by this classifier.
        /// </summary>
        /// 
        public int Classes
        {
            get { return models.Length; }
        }

        /// <summary>
        ///   Computes the most likely class for a given sequence.
        /// </summary>
        /// 
        protected int Compute(Array sequence)
        {
            double[] likelihoods;
            return Compute(sequence, out likelihoods);
        }

        /// <summary>
        ///   Computes the most likely class for a given sequence.
        /// </summary>
        /// 
        protected int Compute(Array sequence, out double likelihood)
        {
            double[] likelihoods;
            int label = Compute(sequence, out likelihoods);
            likelihood = (label >= 0) ? likelihoods[label] : 0;
            return label;
        }



        /// <summary>
        ///   Computes the most likely class for a given sequence.
        /// </summary>
        /// <returns>Return the label of the given sequence, or -1 if it has
        /// been rejected by the <see cref="Threshold">threshold model</see>.</returns>
        /// 
        protected int Compute(Array sequence, out double[] likelihoods)
        {
            double[] response = new double[models.Length];

            double min = 0.0;

            // For every model in the set (including threshold)
#if !DEBUG
            AForge.Parallel.For(0, models.Length + 1, i =>
#else
            for (int i = 0; i < models.Length + 1; i++)
#endif
            {
                if (i < models.Length)
                {
                    // Evaluate the probability of the sequence
                    response[i] = models[i].Evaluate(sequence);
                }
                else if (threshold != null)
                {
                    // Evaluate the current threshold 
                    min = threshold.Evaluate(sequence);
                }
            }
#if !DEBUG
            );
#endif

            if (min != 0)
            {
                for (int i = 0; i < response.Length; i++)
                    response[i] -= min;
            }

            int imax; double max;
            max = response.Max(out imax);
            likelihoods = response;

            // Returns the index of the most likely model.
            return (max > 0) ? imax : -1;
        }


    }
}
