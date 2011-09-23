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
    using Accord.Statistics.Distributions.Univariate;
    using Accord.Statistics.Models.Markov.Topology;
    using Accord.Statistics.Distributions.Multivariate;

    /// <summary>
    ///   Arbitrary-density Hidden Markov Model.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   Hidden Markov Models (HMM) are stochastic methods to model temporal and sequence
    ///   data. They are especially known for their application in temporal pattern recognition
    ///   such as speech, handwriting, gesture recognition, part-of-speech tagging, musical
    ///   score following, partial discharges and bioinformatics.</para>
    /// <para>
    ///   Dynamical systems of discrete nature assumed to be governed by a Markov chain emits
    ///   a sequence of observable outputs. Under the Markov assumption, it is also assumed that
    ///   the latest output depends only on the current state of the system. Such states are often
    ///   not known from the observer when only the output values are observable.</para>
    ///   
    /// <para>
    ///   Hidden Markov Models attempt to model such systems and allow, among other things,
    ///   <list type="number">
    ///     <item><description>
    ///       To infer the most likely sequence of states that produced a given output sequence,</description></item>
    ///     <item><description>
    ///       Infer which will be the most likely next state (and thus predicting the next output),</description></item>
    ///     <item><description>
    ///       Calculate the probability that a given sequence of outputs originated from the system
    ///       (allowing the use of hidden Markov models for sequence classification).</description></item>
    ///     </list></para>
    ///     
    ///  <para>     
    ///   The “hidden” in Hidden Markov Models comes from the fact that the observer does not
    ///   know in which state the system may be in, but has only a probabilistic insight on where
    ///   it should be.</para>
    ///   
    ///  <para>
    ///   The arbitrary-density Hidden Markov Model uses any probability density function (such
    ///   as <see cref="Accord.Statistics.Distributions.Univariate.NormalDistribution">Gaussian</see>
    ///   <see cref="Accord.Statistics.Distributions.Univariate.Mixture{T}">Mixture Model</see>) for
    ///   computing the state probability. In other words, in a continuous HMM the matrix of emission
    ///   probabilities B is replaced by an array of either discrete or continuous probability density
    ///   functions.</para>
    ///  
    ///  <para>
    ///   If a <see cref="Accord.Statistics.Distributions.Univariate.GeneralDiscreteDistribution">general
    ///   discrete distribution</see> is used as the underlying probability density function, the
    ///   model becomes equivalent to the <see cref="HiddenMarkovModel">discrete Hidden Markov Model</see>.
    ///  </para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       http://en.wikipedia.org/wiki/Hidden_Markov_model </description></item>
    ///   </list></para>
    /// </remarks>
    ///
    /// <seealso cref="HiddenMarkovModel">Discrete-density Hidden Markov Model</seealso>
    /// 
    /// 
    /// <example>
    ///   In the following example, we will create a Continuous Hidden Markov Model using
    ///   a univariate Normal distribution to model properly model continuous sequences.
    ///   <code>
    ///   // Create continuous sequences. In the sequences below, there
    ///   //  seems to be two states, one for values between 0 and 1 and
    ///   //  another for values between 5 and 7. The states seems to be
    ///   //  switched on every observation.
    ///   double[][] sequences = new double[][] 
    ///   {
    ///       new double[] { 0.1, 5.2, 0.3, 6.7, 0.1, 6.0 },
    ///       new double[] { 0.2, 6.2, 0.3, 6.3, 0.1, 5.0 },
    ///       new double[] { 0.1, 7.0, 0.1, 7.0, 0.2, 5.6 },
    ///   };
    /// 
    ///             
    ///   // Specify a initial normal distribution for the samples.
    ///   NormalDistribution density = NormalDistribution();
    /// 
    ///   // Creates a continuous hidden Markov Model with two states organized in a forward
    ///   //  topology and an underlying univariate Normal distribution as probability density.
    ///   var model = new HiddenMarkovModel&lt;NormalDistribution&gt;(new Ergodic(2), density);
    /// 
    ///   // Configure the learning algorithms to train the sequence classifier until the
    ///   // difference in the average log-likelihood changes only by as little as 0.0001
    ///   var teacher = new BaumWelchLearning&lt;NormalDistribution&gt;(model)
    ///   {
    ///       Tolerance = 0.0001,
    ///       Iterations = 0,
    ///   };
    /// 
    ///   // Fit the model
    ///   double likelihood = teacher.Run(sequences);
    /// 
    ///   // See the probability of the sequences learned
    ///   double l1 = model.Evaluate(new[] { 0.1, 5.2, 0.3, 6.7, 0.1, 6.0 }); // 0.87
    ///   double l2 = model.Evaluate(new[] { 0.2, 6.2, 0.3, 6.3, 0.1, 5.0 }); // 1.00
    /// 
    ///   // See the probability of an unrelated sequence
    ///   double l3 = model.Evaluate(new[] { 1.1, 2.2, 1.3, 3.2, 4.2, 1.0 }); // 0.00
    /// </code>
    /// </example>
    /// 
    /// <example>
    ///   In the following example, we will create a Discrete Hidden Markov Model
    ///   using a Generic Discrete Probability Distribution to reproduce the same
    ///   code example given in <seealso cref="HiddenMarkovModel"/> documentation.
    ///   <code>
    ///   // Arbitrary-density Markov Models can operate using any
    ///   // probability distribution, including discrete ones. 
    ///   
    ///   // In the follwing example, we will try to create a
    ///   // Discrete Hidden Markov Model using a discrete
    ///   // distribution to detect if a given sequence starts
    ///   // with a zero and has any number of ones after that.
    ///   
    ///   double[][] sequences = new double[][] 
    ///   {
    ///       new double[] { 0,1,1,1,1,0,1,1,1,1 },
    ///       new double[] { 0,1,1,1,0,1,1,1,1,1 },
    ///       new double[] { 0,1,1,1,1,1,1,1,1,1 },
    ///       new double[] { 0,1,1,1,1,1         },
    ///       new double[] { 0,1,1,1,1,1,1       },
    ///       new double[] { 0,1,1,1,1,1,1,1,1,1 },
    ///       new double[] { 0,1,1,1,1,1,1,1,1,1 },
    ///   };
    ///   
    ///   // Create a new Hidden Markov Model with 3 states and
    ///   //  a generic discrete distribution with two symbols
    ///   var hmm = new HiddenMarkovModel&lt;NormalDistribution&gt;.CreateDiscrete(3, 2);
    ///   
    ///   // We will try to fit the model to the data until the difference in
    ///   //  the average log-likelihood changes only by as little as 0.0001
    ///   var teacher = new BaumWelchLearning&lt;NormalDistribution&gt;(hmm)
    ///   { 
    ///       Tolerance = 0.0001,
    ///       Iterations = 0 
    ///   };
    ///   
    ///   // Begin model training
    ///   double ll = teacher.Run(sequences);
    ///   
    /// 
    ///   // Calculate the probability that the given
    ///   //  sequences originated from the model
    ///   double l1 = hmm.Evaluate(new double[] { 0, 1 });       // 0.999
    ///   double l2 = hmm.Evaluate(new double[] { 0, 1, 1, 1 }); // 0.916
    ///   
    ///   // Sequences which do not start with zero have much lesser probability.
    ///   double l3 = hmm.Evaluate(new double[] { 1, 1 });       // 0.000
    ///   double l4 = hmm.Evaluate(new double[] { 1, 0, 0, 0 }); // 0.000
    ///   
    ///   // Sequences which contains few errors have higher probabability
    ///   //  than the ones which do not start with zero. This shows some
    ///   //  of the temporal elasticity and error tolerance of the HMMs.
    ///   double l5 = hmm.Evaluate(new double[] { 0, 1, 0, 1, 1, 1, 1, 1, 1 }); // 0.034
    ///   double l6 = hmm.Evaluate(new double[] { 0, 1, 1, 1, 1, 1, 1, 0, 1 }); // 0.034
    ///   </code>
    /// </example>
    /// 
    [Serializable]
    public class HiddenMarkovModel<TDistribution> : HiddenMarkovModelBase, IHiddenMarkovModel
        where TDistribution : IDistribution
    {

        // Model is defined as M = (A, B, pi)
        private TDistribution[] B; // emission probabilities

        // The other parameters are defined in HiddenMarkovModelBase
        // private double[,] A; // Transition probabilities
        // private double[] pi; // Initial state probabilities


        private int dimension = 1;
        private bool multivariate;



        //---------------------------------------------


        #region Constructors
        /// <summary>
        ///   Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// 
        /// <param name="topology">
        ///   A <see cref="Topology"/> object specifying the initial values of the matrix of transition 
        ///   probabilities <c>A</c> and initial state probabilities <c>pi</c> to be used by this model.
        /// </param>
        /// <param name="emissions">
        ///   The initial emission probability distribution to be used by each of the states. This
        ///   initial probability distribution will be cloned accross all states.
        /// </param>
        /// 
        public HiddenMarkovModel(ITopology topology, TDistribution emissions)
            : base(topology)
        {
            if (emissions == null)
            {
                throw new ArgumentNullException("emissions");
            }

            // Initialize B using the initial distribution
            B = new TDistribution[States];

            for (int i = 0; i < B.Length; i++)
                B[i] = (TDistribution)emissions.Clone();


            if (emissions is IMultivariateDistribution)
            {
                multivariate = true;
                dimension = ((IMultivariateDistribution)B[0]).Dimension;
            }
        }


        /// <summary>
        ///   Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// 
        /// <param name="topology">
        ///   A <see cref="Topology"/> object specifying the initial values of the matrix of transition 
        ///   probabilities <c>A</c> and initial state probabilities <c>pi</c> to be used by this model.
        /// </param>
        /// <param name="emissions">
        ///   The initial emission probability distributions for each state.
        /// </param>
        /// 
        public HiddenMarkovModel(ITopology topology, TDistribution[] emissions)
            : base(topology)
        {
            if (emissions == null)
            {
                throw new ArgumentNullException("emissions");
            }

            if (emissions.Length != States)
            {
                throw new ArgumentException(
                    "The emission matrix should have the same number of rows as the number of states in the model.",
                    "emissions");
            }

            B = emissions;

            // Assume all emissions have same form
            if (B[0] is IMultivariateDistribution)
            {
                multivariate = true;
                dimension = ((IMultivariateDistribution)B[0]).Dimension;
            }
        }


        /// <summary>
        ///   Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// 
        /// <param name="transitions">The transitions matrix A for this model.</param>
        /// <param name="emissions">The emissions matrix B for this model.</param>
        /// <param name="probabilities">The initial state probabilities for this model.</param>
        /// 
        public HiddenMarkovModel(double[,] transitions, TDistribution[] emissions, double[] probabilities)
            : this(new Custom(transitions, probabilities), emissions) { }


        /// <summary>
        ///   Constructs a new Hidden Markov Model with arbitrary-density state probabilities.
        /// </summary>
        /// 
        /// <param name="states">The number of states for the model.</param>
        /// <param name="emissions">A initial distribution to be copied to all states in the model.</param>
        /// 
        public HiddenMarkovModel(int states, TDistribution emissions)
            : this(new Topology.Ergodic(states), emissions) { }
        #endregion


        //---------------------------------------------


        #region Named constructors
        /// <summary>
        ///   Constructs a new discrete-density Hidden Markov Model.
        /// </summary>
        /// <param name="transitions">The transitions matrix A for this model.</param>
        /// <param name="emissions">The emissions matrix B for this model.</param>
        /// <param name="probabilities">The initial state probabilities for this model.</param>
        /// 
        public static HiddenMarkovModel<GeneralDiscreteDistribution> CreateDiscrete(double[,] transitions, double[,] emissions, double[] probabilities)
        {
            ITopology topology = new Custom(transitions, probabilities);

            if (emissions == null)
            {
                throw new ArgumentNullException("emissions");
            }

            if (emissions.GetLength(0) != topology.States)
            {
                throw new ArgumentException(
                    "The emission matrix should have the same number of rows as the number of states in the model.",
                    "emissions");
            }


            // Initialize B using a discrete distribution
            var B = new GeneralDiscreteDistribution[topology.States];
            for (int i = 0; i < B.Length; i++)
                B[i] = new GeneralDiscreteDistribution(Accord.Math.Matrix.GetRow(emissions, i));

            return new HiddenMarkovModel<GeneralDiscreteDistribution>(topology, B);
        }

        /// <summary>
        ///   Constructs a new Hidden Markov Model with discrete state probabilities.
        /// </summary>
        /// 
        /// <param name="topology">
        ///   A <see cref="Topology"/> object specifying the initial values of the matrix of transition 
        ///   probabilities <c>A</c> and initial state probabilities <c>pi</c> to be used by this model.
        /// </param>
        /// <param name="symbols">The number of output symbols used for this model.</param>
        /// 
        public static HiddenMarkovModel<GeneralDiscreteDistribution> CreateDiscrete(ITopology topology, int symbols)
        {
            if (symbols <= 0)
            {
                throw new ArgumentOutOfRangeException("symbols",
                    "Number of symbols should be higher than zero.");
            }

            // Initialize B with a uniform discrete distribution
            GeneralDiscreteDistribution[] B = new GeneralDiscreteDistribution[topology.States];

            for (int i = 0; i < B.Length; i++)
                B[i] = new GeneralDiscreteDistribution(symbols);

            return new HiddenMarkovModel<GeneralDiscreteDistribution>(topology, B);
        }

        /// <summary>
        ///   Constructs a new Hidden Markov Model with discrete state probabilities.
        /// </summary>
        /// 
        /// <param name="states">The number of states for this model.</param>
        /// <param name="symbols">The number of output symbols used for this model.</param>
        /// 
        public static HiddenMarkovModel<GeneralDiscreteDistribution> CreateDiscrete(int states, int symbols)
        {
            return CreateDiscrete(new Topology.Ergodic(states), symbols);
        }
        #endregion


        //---------------------------------------------


        #region Public Properties
        /// <summary>
        ///   Gets the number of dimensions in the
        ///   probability distributions for the states.
        /// </summary>
        /// 
        public int Dimension
        {
            get { return this.dimension; }
        }

        /// <summary>
        ///   Gets the Emission matrix (B) for this model.
        /// </summary>
        /// 
        public TDistribution[] Emissions
        {
            get { return this.B; }
        }

        #endregion


        //---------------------------------------------


        #region Public Methods

        /// <summary>
        ///   Calculates the most likely sequence of hidden states
        ///   that produced the given observation sequence.
        /// </summary>
        /// 
        /// <remarks>
        ///   Decoding problem. Given the HMM M = (A, B, pi) and  the observation sequence 
        ///   O = {o1,o2, ..., oK}, calculate the most likely sequence of hidden states Si
        ///   that produced this observation sequence O. This can be computed efficiently
        ///   using the Viterbi algorithm.
        /// </remarks>
        /// 
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="probability">The state optimized probability.</param>
        /// <returns>The sequence of states that most likely produced the sequence.</returns>
        /// 
        public int[] Decode(Array observations, out double probability)
        {
            return Decode(observations, false, out probability);
        }

        /// <summary>
        ///   Calculates the most likely sequence of hidden states
        ///   that produced the given observation sequence.
        /// </summary>
        /// 
        /// <remarks>
        ///   Decoding problem. Given the HMM M = (A, B, pi) and  the observation sequence 
        ///   O = {o1,o2, ..., oK}, calculate the most likely sequence of hidden states Si
        ///   that produced this observation sequence O. This can be computed efficiently
        ///   using the Viterbi algorithm.
        /// </remarks>
        /// 
        /// <param name="observations">A sequence of observations.</param>
        /// <param name="probability">The state optimized probability.</param>
        /// <param name="logarithm">True to return the log-likelihood, false to return
        /// the likelihood. Default is false (default is to return the likelihood).</param>
        /// <returns>The sequence of states that most likely produced the sequence.</returns>
        /// 
        public int[] Decode(Array observations, bool logarithm, out double probability)
        {
            if (observations == null)
                throw new ArgumentNullException("observations");

            if (observations.Length == 0)
            {
                probability = 0.0;
                return new int[0];
            }

            if (!(observations is double[][] || observations is double[]))
                throw new ArgumentException("Argument should be either of type " +
                    "double[] (for univariate observation) or double[][] (for " +
                    "multivariate observation).", "observations");


            double[][] x = convert(observations);


            // Viterbi-forward algorithm.
            int T = x.Length;
            int states = States;
            int minState;
            double minWeight;
            double weight;

            double[] pi = Probabilities;
            double[,] A = Transitions;

            int[,] s = new int[states, T];
            double[,] a = new double[states, T];


            // Base
            for (int i = 0; i < states; i++)
                a[i, 0] = -Math.Log(pi[i]) - Math.Log(B[i].ProbabilityFunction(x[0]));

            // Induction
            for (int t = 1; t < T; t++)
            {
                double[] observation = x[t];

                for (int j = 0; j < states; j++)
                {
                    minState = 0;
                    minWeight = a[0, t - 1] - Math.Log(A[0, j]);

                    for (int i = 1; i < states; i++)
                    {
                        weight = a[i, t - 1] - Math.Log(A[i, j]);

                        if (weight < minWeight)
                        {
                            minState = i;
                            minWeight = weight;
                        }
                    }

                    a[j, t] = minWeight - Math.Log(B[j].ProbabilityFunction(observation));
                    s[j, t] = minState;
                }
            }

            // Find minimum value for time T-1
            minState = 0;
            minWeight = a[0, T - 1];

            for (int i = 1; i < states; i++)
            {
                if (a[i, T - 1] < minWeight)
                {
                    minState = i;
                    minWeight = a[i, T - 1];
                }
            }


            // Trackback
            int[] path = new int[T];
            path[T - 1] = minState;

            for (int t = T - 2; t >= 0; t--)
                path[t] = s[path[t + 1], t + 1];


            // Returns the sequence probability as an out parameter
            probability = logarithm ? -minWeight : Math.Exp(-minWeight);

            // Returns the most likely (Viterbi path) for the given sequence
            return path;
        }

        /// <summary>
        ///   Calculates the probability that this model has generated the given sequence.
        /// </summary>
        /// 
        /// <remarks>
        ///   Evaluation problem. Given the HMM  M = (A, B, pi) and  the observation
        ///   sequence O = {o1, o2, ..., oK}, calculate the probability that model
        ///   M has generated sequence O. This can be computed efficiently using the
        ///   either the Viterbi or the Forward algorithms.
        /// </remarks>
        /// 
        /// <param name="observations">
        ///   A sequence of observations.
        /// </param>
        /// <returns>
        ///   The probability that the given sequence has been generated by this model.
        /// </returns>
        /// 
        public double Evaluate(Array observations)
        {
            return Evaluate(observations, false);
        }

        /// <summary>
        ///   Calculates the probability that this model has generated the given sequence.
        /// </summary>
        /// 
        /// <remarks>
        ///   Evaluation problem. Given the HMM  M = (A, B, pi) and  the observation
        ///   sequence O = {o1, o2, ..., oK}, calculate the probability that model
        ///   M has generated sequence O. This can be computed efficiently using the
        ///   either the Viterbi or the Forward algorithms.
        /// </remarks>
        /// 
        /// <param name="observations">
        ///   A sequence of observations.
        /// </param>
        /// <param name="logarithm">
        ///   True to return the log-likelihood, false to return
        ///   the likelihood. Default is false.
        /// </param>
        /// <returns>
        ///   The probability that the given sequence has been generated by this model.
        /// </returns>
        /// 
        public double Evaluate(Array observations, bool logarithm)
        {
            if (observations == null)
                throw new ArgumentNullException("observations");

            if (observations.Length == 0)
                return 0.0;

            if (!(observations is double[][] || observations is double[]))
                throw new ArgumentException("Argument should be either of type " +
                    "double[] (for univariate observation) or double[][] (for " +
                    "multivariate observation).", "observations");


            double[][] obs = convert(observations);

            // Forward algorithm
            double logLikelihood;

            // Compute forward probabilities
            ForwardBackwardAlgorithm.Forward(this, obs, out logLikelihood);

            // Return the sequence probability
            return logarithm ? logLikelihood : Math.Exp(logLikelihood);
        }



        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double[] Predict(double[][] observations)
        {
            if (!multivariate) 
                throw new ArgumentException("Model is univariate.", "observations");

            double probability;
            return Predict(observations, out probability);
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double Predict(double[] observations)
        {
            if (multivariate) 
                throw new ArgumentException("Model is multivariate.", "observations");

            double probability;
            return Predict(observations, out probability);
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double[] Predict(double[][] observations, out double probability)
        {
            if (!multivariate)
                throw new ArgumentException("Model is univariate.", "observations");

            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observation (currently only one ahead is supported).
            double[][] prediction = predict(observations, 1, out probability, out weights);

            return prediction[0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double Predict(double[] observations, out double probability)
        {
            if (multivariate)
                throw new ArgumentException("Model is multivariate.", "observations");

            // Convert to multivariate observations
            double[][] obs = convert(observations);

            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observation (currently only one ahead is supported).
            double[][] prediction = predict(obs, 1, out probability, out weights);

            return prediction[0][0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double[] Predict<TMultivariate>(double[][] observations,
            out double probability, out MultivariateMixture<TMultivariate> probabilities)
            where TMultivariate : DistributionBase, TDistribution, IMultivariateDistribution
        {
            if (!multivariate)
                throw new ArgumentException("Model is univariate.", "observations");

            // Compute the next observation (currently only one ahead is supported)
            double[][] prediction = predict(observations, out probability, out probabilities);

            return prediction[0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double[] Predict<TMultivariate>(double[][] observations, out MultivariateMixture<TMultivariate> probabilities)
            where TMultivariate : DistributionBase, TDistribution, IMultivariateDistribution
        {
            if (!multivariate)
                throw new ArgumentException("Model is univariate.", "observations");

            double probability;

            // Compute the next observation (currently only one ahead is supported)
            double[][] prediction = predict(observations, out probability, out probabilities);

            return prediction[0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double Predict<TUnivariate>(double[] observations, out Mixture<TUnivariate> probabilities)
            where TUnivariate : DistributionBase, TDistribution, IUnivariateDistribution
        {
            if (multivariate)
                throw new ArgumentException("Model is multivariate.", "observations");

            double probability;

            // Compute the next observation (as if it were multidimensional)
            double[] prediction = predict(observations, out probability, out probabilities);

            // Return the first (single) dimension of the next observation.
            return prediction[0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double Predict<TUnivariate>(double[] observations,
            out double probability, out Mixture<TUnivariate> probabilities)
            where TUnivariate : DistributionBase, TDistribution, IUnivariateDistribution
        {
            if (multivariate)
                throw new ArgumentException("Model is multivariate.", "observations");

            // Compute the next observation (as if it were multidimensional)
            double[] prediction = predict(observations, out probability, out probabilities);

            // Return the first (single) dimension of the next observation.
            return prediction[0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double[][] Predict(double[][] observations, int next, out double probability)
        {
            if (!multivariate)
                throw new ArgumentException("Model is univariate.", "observations");

            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observations
            double[][] prediction = predict(observations, next, out probability, out weights);

            return prediction;
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        public double[] Predict(double[] observations, int next, out double probability)
        {
            if (multivariate)
                throw new ArgumentException("Model is multivariate.", "observations");

            // Convert to multivariate observations
            double[][] obs = convert(observations);

            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observations
            double[][] prediction = predict(obs, next, out probability, out weights);

            // Return the first (single) dimension of the next observations.
            return Accord.Math.Matrix.Combine(prediction);
        }
        #endregion


        //---------------------------------------------


        #region Private Methods
        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        private double[][] predict<TMultivariate>(double[][] observations,
            out double probability, out MultivariateMixture<TMultivariate> probabilities)
            where TMultivariate : DistributionBase, TDistribution, IMultivariateDistribution
        {
            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observation (currently only one ahead is supported).
            double[][] prediction = predict(observations, 1, out probability, out weights);

            // Create the mixture distribution defining the model likelihood in
            // assuming the next observation belongs will belong to each state.
            TMultivariate[] b = Array.ConvertAll(B, x => (TMultivariate)x);
            probabilities = new MultivariateMixture<TMultivariate>(weights[1], b);

            return prediction;
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        private double[] predict<TUnivariate>(double[] observations,
            out double probability, out Mixture<TUnivariate> probabilities)
            where TUnivariate : DistributionBase, TDistribution, IUnivariateDistribution
        {
            // Convert to multivariate observations
            double[][] obs = convert(observations);

            // Matrix to store the probabilities in assuming the next
            // observations (prediction) will belong to each state.
            double[][] weights;

            // Compute the next observation (currently only one ahead is supported).
            double[][] prediction = predict(obs, 1, out probability, out weights);

            // Create the mixture distribution defining the model likelihood in
            // assuming the next observation belongs will belong to each state.
            TUnivariate[] b = Array.ConvertAll(B, x => (TUnivariate)x);
            probabilities = new Mixture<TUnivariate>(weights[1], b);

            return prediction[0];
        }

        /// <summary>
        ///   Predicts the next observation occurring after a given observation sequence.
        /// </summary>
        /// 
        private double[][] predict(double[][] observations, int next,
            out double probability, out double[][] future)
        {
            int states = States;
            int T = next;

            double[,] A = Transitions;
            double logLikelihood;

            double[][] prediction = new double[next][];
            double[][] expectation = new double[states][];

            // Compute expectations for each state
            for (int i = 0; i < states; i++)
                expectation[i] = getMode(B[i]);


            // Compute forward probabilities for the given observation sequence.
            double[,] fw0 = ForwardBackwardAlgorithm.Forward(this, observations, out logLikelihood);

            // Create a matrix to store the future probabilities for the prediction
            // sequence and copy the latest forward probabilities on its first row.
            double[][] fwd = new double[T + 1][];
            for (int i = 0; i < fwd.Length; i++)
                fwd[i] = new double[States];


            // 1. Initialization
            for (int i = 0; i < States; i++)
                fwd[0][i] = fw0[observations.Length - 1, i];


            // 2. Induction
            for (int t = 0; t < T; t++)
            {
                double[] weights = fwd[t + 1];

                double scale = 0;
                for (int i = 0; i < weights.Length; i++)
                {
                    double sum = 0.0;
                    for (int j = 0; j < states; j++)
                        sum += fwd[t][j] * A[j, i];

                    scale += weights[i] = sum * B[i].ProbabilityFunction(expectation[i]);
                }

                scale /= Math.Exp(logLikelihood);

                if (scale != 0) // Scaling
                {
                    for (int i = 0; i < weights.Length; i++)
                        weights[i] /= scale;
                }


                // Select most probable value
                double maxWeight = weights[0];
                double sumWeight = maxWeight;
                prediction[t] = expectation[0];
                for (int i = 1; i < states; i++)
                {
                    if (weights[i] > maxWeight)
                    {
                        maxWeight = weights[i];
                        prediction[t] = expectation[i];
                    }
                }

                // Recompute log-likelihood
                logLikelihood = Math.Log(maxWeight);
            }

            // Returns the sequence probability as an out parameter
            probability = Math.Exp(logLikelihood);

            // Returns the future-forward probabilities
            future = fwd;

            return prediction;
        }

        private static double[] getMode(TDistribution dist)
        {
            var uni = dist as IUnivariateDistribution;
            if (uni != null) return new double[] { uni.Mode };

            var multi = dist as IMultivariateDistribution;
            return multi.Mode;
        }

        /// <summary>
        ///   Converts a univariate or multivariate array
        ///   of observations into a two-dimensional jagged array.
        /// </summary>
        private double[][] convert(Array array)
        {
            double[][] multivariate = array as double[][];
            if (multivariate != null) return multivariate;

            double[] univariate = array as double[];
            if (univariate != null) return Accord.Math.Matrix.Split(univariate, Dimension);

            throw new ArgumentException("Invalid array argument type.", "array");
        }
        #endregion

    }
}
