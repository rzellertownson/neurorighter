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

namespace Accord.Statistics.Models.Fields
{
    using System;
    using Accord.Statistics.Models.Fields.Features;
    using Accord.Statistics.Models.Fields.Functions;

    /// <summary>
    ///   Linear-Chain Conditional Random Field (experimental).
    /// </summary>
    /// <remarks>
    ///   <para>A conditional random field (CRF) is a type of discriminative undirected
    ///   probabilistic graphical model. It is most often used for labeling or parsing
    ///   of sequential data, such as natural language text or biological sequences
    ///   and computer vision.</para>
    ///   
    ///   <para>This implementation is currently experimental.</para>
    /// </remarks>
    /// 
    public class ConditionalRandomField
    {

        /// <summary>
        ///   Gets the number of states in this
        ///   linear-chain Conditional Random Field.
        /// </summary>
        /// 
        public int States { get; private set; }

        /// <summary>
        ///   Gets the potential function encompassing
        ///   all feature functions for this model.
        /// </summary>
        /// 
        public IPotentialFunction Function { get; private set; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="ConditionalRandomField"/> class.
        /// </summary>
        /// 
        /// <param name="states">The number of states for the model.</param>
        /// <param name="function">The potential function to be used by the model.</param>
        /// 
        public ConditionalRandomField(int states, IPotentialFunction function)
        {
            this.States = states;
            this.Function = function;
        }

        /// <summary>
        ///   Computes the partition function, as known as Z(x),
        ///   for the specified observations.
        /// </summary>
        /// 
        private double Partition(int[] observations)
        {
            double[,] fwd = ForwardBackwardAlgorithm.Forward(Function, observations);

            int T = observations.Length - 1;

            double z = 0.0;
            for (int i = 0; i < States; i++)
                z += fwd[T, i];

            return z;
        }

        /// <summary>
        ///   Computes the Log of the partition function.
        /// </summary>
        /// 
        private double LogPartition(int[] observations)
        {
            return Math.Log(Partition(observations));
        }


        /// <summary>
        ///   Computes the likelihood of the model for the given observations.
        /// </summary>
        /// 
        public double Likelihood(int[] observations, int[] labels)
        {
            double p = Function.LogCompute(-1, labels[0], observations, 0);
            for (int t = 1; t < observations.Length; t++)
                p += Function.LogCompute(labels[t - 1], labels[t], observations, t);
            p = Math.Exp(p);

#if DEBUG
            if (Double.IsNaN(p) || Double.IsInfinity(p))
                throw new Exception();
#endif

            double z = Partition(observations);

            return p / z;
        }

        /// <summary>
        ///   Computes the log-likelihood of the model for the given observations.
        /// </summary>
        /// 
        public double LogLikelihood(int[] observations, int[] labels)
        {
            double p = Function.LogCompute(-1, labels[0], observations, 0);
            for (int t = 1; t < observations.Length; t++)
                p += Function.LogCompute(labels[t - 1], labels[t], observations, t);

            double z = LogPartition(observations);

            if (p == z)
                return 0;

            if (double.IsInfinity(p))
                return 0;

            if (double.IsInfinity(z))
                return 0;

#if DEBUG
            if (Double.IsNaN(p) || Double.IsInfinity(p))
                throw new Exception();

            if (Double.IsNaN(z) || Double.IsInfinity(z))
                throw new Exception();
#endif

            return p - z;
        }

        /// <summary>
        ///   Computes the most likely state labels for the given observations,
        ///   returning the overall sequence probability for this model.
        /// </summary>
        /// 
        public int[] Compute(int[] observations, out double probability)
        {
            // Viterbi-forward algorithm.
            int T = observations.Length;
            int states = States;
            int maxState;
            double maxWeight;
            double weight;

            int[,] s = new int[states, T];
            double[,] a = new double[states, T];


            // Base
            for (int i = 0; i < states; i++)
                a[i, 0] = Function.Compute(-1, i, observations, 0);

            // Induction
            for (int t = 1; t < T; t++)
            {
                int observation = observations[t];

                for (int j = 0; j < states; j++)
                {
                    maxState = 0;
                    maxWeight = a[0, t - 1] * Function.Compute(0, j, observations, t);

                    for (int i = 1; i < states; i++)
                    {
                        weight = a[i, t - 1] * Function.Compute(i, j, observations, t);

                        if (weight > maxWeight)
                        {
                            maxState = i;
                            maxWeight = weight;
                        }
                    }

                    a[j, t] = maxWeight;
                    s[j, t] = maxState;
                }
            }


            // Find minimum value for time T-1
            maxState = 0;
            maxWeight = a[0, T - 1];

            for (int i = 1; i < states; i++)
            {
                if (a[i, T - 1] > maxWeight)
                {
                    maxState = i;
                    maxWeight = a[i, T - 1];
                }
            }


            // Trackback
            int[] path = new int[T];
            path[T - 1] = maxState;

            for (int t = T - 2; t >= 0; t--)
                path[t] = s[path[t + 1], t + 1];


            // Returns the sequence probability as an out parameter
            probability = maxWeight;

            // Returns the most likely (Viterbi path) for the given sequence
            return path;
        }

        /// <summary>
        ///   Computes the most likely state labels for the given observations,
        ///   returning the overall sequence probability for this model.
        /// </summary>
        /// 
        public double Likelihood(int[][] observations, int[][] labels)
        {
            double ll = 1;
            for (int i = 0; i < observations.Length; i++)
                ll *= Likelihood(observations[i], labels[i]);

            return ll;
        }

        /// <summary>
        ///   Computes the most likely state labels for the given observations,
        ///   returning the overall sequence log-likelihood for this model.
        /// </summary>
        /// 
        public double LogLikelihood(int[][] observations, int[][] labels)
        {
            double ll = 0;
            for (int i = 0; i < observations.Length; i++)
                ll += LogLikelihood(observations[i], labels[i]);

#if DEBUG
            if (Double.IsNaN(ll) || Double.IsInfinity(ll))
                throw new Exception();
#endif

            return ll;
        }

    }



}
