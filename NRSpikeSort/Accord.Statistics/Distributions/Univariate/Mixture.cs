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

namespace Accord.Statistics.Distributions.Univariate
{
    using System;
    using Accord.Math;
    using Accord.Statistics.Distributions.Fitting;

    /// <summary>
    ///   Mixture of univariate probability distributions.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   A mixture density is a probability density function which is expressed
    ///   as a convex combination (i.e. a weighted sum, with non-negative weights
    ///   that sum to 1) of other probability density functions. The individual
    ///   density functions that are combined to make the mixture density are
    ///   called the mixture components, and the weights associated with each
    ///   component are called the mixture weights.</para>
    ///   
    /// <para>    
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Mixture_density">
    ///       Wikipedia, The Free Encyclopedia. Mixture density. Available on:
    ///       http://en.wikipedia.org/wiki/Mixture_density </a></description></item>
    ///   </list></para>
    /// </remarks>
    ///   
    /// <typeparam name="T">
    ///   The type of the univariate component distributions.</typeparam>
    ///   
    [Serializable]
    public class Mixture<T> : UnivariateContinuousDistribution, IMixture<T>
        where T : IUnivariateDistribution
    {

        // distribution parameters
        private double[] coefficients;
        private T[] components;

        // distributions measures
        private double? mean;
        private double? variance;


        /// <summary>
        ///   Initializes a new instance of the <see cref="Mixture&lt;T&gt;"/> class.
        /// </summary>
        /// 
        /// <param name="components">The mixture distribution components.</param>
        /// 
        public Mixture(params T[] components)
        {
            if (components == null)
                throw new ArgumentNullException("components");

            this.initialize(null, components);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Mixture&lt;T&gt;"/> class.
        /// </summary>
        /// 
        /// <param name="coefficients">The mixture weight coefficients.</param>
        /// <param name="components">The mixture distribution components.</param>
        /// 
        public Mixture(double[] coefficients, params T[] components)
        {
            if (components == null)
                throw new ArgumentNullException("components");

            if (coefficients == null)
                throw new ArgumentNullException("coefficients");

            if (coefficients.Length != components.Length)
                throw new ArgumentException(
                    "The coefficient and component arrays should have the same length.",
                    "components");

            this.initialize(coefficients, components);
        }

        private void initialize(double[] coef, T[] comp)
        {
            if (coef == null)
            {
                coef = new double[comp.Length];
                for (int i = 0; i < coef.Length; i++)
                    coef[i] = 1.0 / coef.Length;
            }

            this.coefficients = coef;
            this.components = comp;

            this.mean = null;
            this.variance = null;
        }

        /// <summary>
        ///   Gets the mixture components.
        /// </summary>
        /// 
        public T[] Components
        {
            get { return components; }
        }

        /// <summary>
        ///   Gets the weight coefficients.
        /// </summary>
        /// 
        public double[] Coefficients
        {
            get { return coefficients; }
        }


        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range.</param>
        /// 
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the current distribution.
        /// </returns>
        /// 
        /// <remarks>
        ///   The Probability Density Function (PDF) describes the
        ///   probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// 
        public override double ProbabilityDensityFunction(double x)
        {
            double r = 0.0;
            for (int i = 0; i < components.Length; i++)
                r = coefficients[i] * components[i].ProbabilityFunction(x);
            return r;
        }

        /// <summary>
        ///   This method is not supported.
        /// </summary>
        /// 
        public override double DistributionFunction(double x)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// 
        /// <param name="observations">The array of observations to fit the model against. The array
        /// elements can be either of type double (for univariate data) or
        /// type double[] (for multivariate data).</param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">Optional arguments which may be used during fitting, such
        /// as regularization constants and additional parameters.</param>
        /// 
        /// <remarks>
        ///   Although both double[] and double[][] arrays are supported,
        ///   providing a double[] for a multivariate distribution or a
        ///   double[][] for a univariate distribution may have a negative
        ///   impact in performance.
        /// </remarks>
        /// 
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            // Estimation parameters
            double threshold = 1e-3;
            IFittingOptions innerOptions = null;

            if (options != null)
            {
                // Process optional arguments
                MixtureOptions o = (MixtureOptions)options;
                threshold = o.Threshold;
                innerOptions = o.InnerOptions;
            }


            // 1. Initialize means, covariances and mixing coefficients
            //    and evaluate the initial value of the log-likelihood

            int N = observations.Length;
            int K = components.Length;

            weights = weights.Multiply(N);

            double[][] gamma = new double[K][];
            for (int k = 0; k < gamma.Length; k++)
                gamma[k] = new double[N];


            double[] pi = (double[])coefficients.Clone();

            T[] pdf = new T[components.Length];
            for (int i = 0; i < components.Length; i++)
                pdf[i] = (T)components[i].Clone();


            double likelihood = logLikelihood(pi, pdf, observations, weights);
            bool converged = false;


            while (!converged)
            {
                // 2. Expectation: Evaluate the responsibilities using the
                //    current parameter values.
                for (int n = 0; n < observations.Length; n++)
                {
                    double den = 0.0;
                    double w = weights[n];
                    double x = observations[n];

                    for (int k = 0; k < gamma.Length; k++)
                        den += gamma[k][n] = pi[k] * pdf[k].ProbabilityFunction(x) * w;

                    if (den != 0)
                    {
                        for (int k = 0; k < gamma.Length; k++)
                            gamma[k][n] /= den;
                    }
                }

                // 3. Maximization: Re-estimate the parameters using the
                //    current responsibilities
                for (int k = 0; k < gamma.Length; k++)
                {
                    double Nk = gamma[k].Sum();
                    double[] w = gamma[k].Divide(Nk);

                    pi[k] = Nk / N;
                    pdf[k].Fit(observations, w, innerOptions);
                }

                // 4. Evaluate the log-likelihood and check for convergence
                double newLikelihood = logLikelihood(pi, pdf, observations, weights);

                if (Double.IsNaN(newLikelihood) || Double.IsInfinity(newLikelihood))
                    throw new ConvergenceException("Fitting did not converge.");

                if (Math.Abs(likelihood - newLikelihood) < threshold * Math.Abs(likelihood))
                    converged = true;

                likelihood = newLikelihood;
            }

            // Become the newly fitted distribution.
            this.initialize(pi, pdf);
        }

        /// <summary>
        ///   Computes the log-likelihood of the distribution
        ///   for a given set of observations.
        /// </summary>
        /// 
        public double LogLikelihood(double[] observations, double[] weights)
        {
            return logLikelihood(coefficients, components, observations, weights);
        }

        /// <summary>
        ///   Computes the log-likelihood of the distribution
        ///   for a given set of observations.
        /// </summary>
        /// 
        public double LogLikelihood(double[] observations)
        {
            double[] weights = new double[observations.Length];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 1.0 / weights.Length;

            return logLikelihood(coefficients, components, observations, weights);
        }

        /// <summary>
        ///   Computes the log-likelihood of the distribution
        ///   for a given set of observations.
        /// </summary>
        /// 
        private static double logLikelihood(double[] pi, T[] pdf, double[] observations, double[] weigths)
        {
            int N = observations.Length;

            double likelihood = 0.0;
            for (int n = 0; n < observations.Length; n++)
            {
                double x = observations[n];
                double w = weigths[n];

                if (w == 0) continue;

                double sum = 0.0;
                for (int k = 0; k < pi.Length; k++)
                    sum += pi[k] * pdf[k].ProbabilityFunction(x) * w;

                if (sum > 0) likelihood += Math.Log(sum);
            }

            return likelihood;
        }

        /// <summary>
        ///   Creates a new object that is a copy of the current instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A new object that is a copy of this instance.
        /// </returns>
        /// 
        public override object Clone()
        {
            // Clone the mixture coefficients
            double[] pi = (double[])coefficients.Clone();

            // Clone the mixture components
            T[] pdf = new T[components.Length];
            for (int i = 0; i < components.Length; i++)
                pdf[i] = (T)components[i].Clone();

            return new Mixture<T>(pi, pdf);
        }


        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get
            {
                if (mean == null)
                {
                    mean = 0.0;
                    for (int i = 0; i < coefficients.Length; i++)
                        mean += coefficients[i] * components[i].Mean;
                }

                return mean.Value;
            }
        }


        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        /// <remarks>
        ///   References: Lidija Trailovic and Lucy Y. Pao, Variance Estimation and
        ///   Ranking of Gaussian Mixture Distributions in Target Tracking
        ///   Applications, Department of Electrical and Computer Engineering
        /// </remarks>
        /// 
        public override double Variance
        {
            get
            {
                if (variance == null)
                {
                    double var = 0.0;
                    for (int i = 0; i < coefficients.Length; i++)
                    {
                        double w = coefficients[i];
                        double m = components[i].Mean;
                        double v = components[i].Variance;
                        var += w * (v + m * m);
                    }

                    variance = var - mean * mean;
                }

                return variance.Value;
            }
        }

        /// <summary>
        ///   This method is not supported.
        /// </summary>
        /// 
        public override double Entropy
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        ///   Estimates a new mixture model from a given set of observations.
        /// </summary>
        /// 
        /// <param name="data">A set of observations.</param>
        /// <param name="components">The initial components of the mixture model.</param>
        /// <returns>Returns a new Mixture fitted to the given observations.</returns>
        /// 
        public static Mixture<T> Estimate(double[] data, params T[] components)
        {
            var mixture = new Mixture<T>(components);
            mixture.Fit(data);
            return mixture;
        }

        /// <summary>
        ///   Estimates a new mixture model from a given set of observations.
        /// </summary>
        /// 
        /// <param name="data">A set of observations.</param>
        /// <param name="coefficients">The initial mixture coefficients.</param>
        /// <param name="components">The initial components of the mixture model.</param>
        /// <returns>Returns a new Mixture fitted to the given observations.</returns>
        /// 
        public static Mixture<T> Estimate(double[] data, double[] coefficients, params T[] components)
        {
            var mixture = new Mixture<T>(coefficients, components);
            mixture.Fit(data);
            return mixture;
        }

        /// <summary>
        ///   Estimates a new mixture model from a given set of observations.
        /// </summary>
        /// 
        /// <param name="data">A set of observations.</param>
        /// <param name="coefficients">The initial mixture coefficients.</param>
        /// <param name="threshold">The convergence threshold for the Expectation-Maximization estimation.</param>
        /// <param name="components">The initial components of the mixture model.</param>
        /// <returns>Returns a new Mixture fitted to the given observations.</returns>
        /// 
        public static Mixture<T> Estimate(double[] data, double threshold, double[] coefficients, params T[] components)
        {
            var mixture = new Mixture<T>(coefficients, components);
            mixture.Fit(data, new MixtureOptions(threshold));
            return mixture;
        }

    }
}
