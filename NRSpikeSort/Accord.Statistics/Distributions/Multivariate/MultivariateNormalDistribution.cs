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

namespace Accord.Statistics.Distributions.Multivariate
{
    using Accord.Math;
    using Accord.Math.Decompositions;
    using System;
    using Accord.Statistics.Distributions.Fitting;

    /// <summary>
    ///   Multivariate Normal (Gaussian) distribution.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The Gaussian is the most widely used distribution for continuous
    ///   variables. In the case of many variables, it is governed by two
    ///   parameters, the mean vector and the variance-covariance matrix.</para>
    /// <para>
    ///   When a covariance matrix given to the class constructor is not positive
    ///   definite, the distribution is degenerate and this may be an indication
    ///   indication that it may be entirely contained in a r-dimensional subspace.
    ///   Applying a rotation to an orthogonal basis to recover a non-degenerate
    ///   r-dimensional distribution may help in this case.</para>
    ///   
    /// <para>    
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://www.aiaccess.net/English/Glossaries/GlosMod/e_gm_positive_definite_matrix.htm">
    ///       Ai Access. Glossary of Data Modeling. Positive definite matrix. Available on:
    ///       http://www.aiaccess.net/English/Glossaries/GlosMod/e_gm_positive_definite_matrix.htm </a></description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class MultivariateNormalDistribution : MultivariateContinuousDistribution
    {

        // Distribution parameters
        private double[] mean;
        private double[,] covariance;

        private CholeskyDecomposition chol;
        private double lnconstant;

        // Derived measures
        private double[] variance;


        /// <summary>
        ///   Constructs a multivariate Gaussian distribution
        ///   with zero mean vector and identity covariance matrix.
        /// </summary>
        /// 
        /// <param name="dimension">The number of dimensions in the distribution.</param>
        /// 
        public MultivariateNormalDistribution(int dimension)
            : this(dimension, true)
        {
        }

        /// <summary>
        ///   Constructs a multivariate Gaussian distribution
        ///   with zero mean vector and identity covariance matrix.
        /// </summary>
        /// 
        private MultivariateNormalDistribution(int dimension, bool init)
            : base(dimension)
        {
            if (init)
            {
                // init is set to false during cloning
                double[] mean = new double[dimension];
                double[,] cov = Matrix.Identity(dimension);
                CholeskyDecomposition chol = new CholeskyDecomposition(cov, false, true);
                initialize(mean, cov, chol);
            }
        }

        /// <summary>
        ///   Constructs a multivariate Gaussian distribution
        ///   with given mean vector and covariance matrix.
        /// </summary>
        /// 
        /// <param name="mean">The mean of the distribution.</param>
        /// <param name="covariance">The covariance for the distribution.</param>
        /// 
        public MultivariateNormalDistribution(double[] mean, double[,] covariance)
            : base(mean.Length)
        {
            int rows = covariance.GetLength(0);
            int cols = covariance.GetLength(1);

            if (rows != cols)
                throw new DimensionMismatchException("covariance", "Covariance matrix should be square.");

            if (mean.Length != rows)
                throw new DimensionMismatchException("covariance", "Covariance matrix should have the same dimensions as mean vector's length.");

            CholeskyDecomposition chol = new CholeskyDecomposition(covariance, false, true);

            if (!chol.PositiveDefinite)
                throw new NonPositiveDefiniteMatrixException("Covariance matrix is not positive definite.");

            initialize(mean, covariance, chol);
        }

        private void initialize(double[] m, double[,] cov, CholeskyDecomposition cd)
        {
            int k = m.Length;

            this.mean = m;
            this.covariance = cov;
            this.chol = cd;


            // Original code:
            //   double det = chol.Determinant;
            //   double detSqrt = System.Math.Sqrt(System.Math.Abs(det));
            //   constant = 1.0 / (System.Math.Pow(2.0 * System.Math.PI, k / 2.0) * detSqrt);


            // Transforming to log operations, we have:
            double lndet = cd.LogDeterminant;

            // Let lndet = log( abs(det) )
            //
            //    detSqrt = sqrt(abs(det)) = sqrt(abs(exp(log(det))) 
            //            = sqrt(exp(log(abs(det))) = sqrt(exp(lndet)
            //
            //    log(detSqrt) = log(sqrt(exp(lndet)) = (1/2)*log(exp(lndet)) 
            //                 = lndet/2.
            //
            //
            // Let lndetsqrt = log(detsqrt) = lndet/2
            //
            //     constant      =       1 / ( ((2PI)^(k/2)) * detSqrt)
            //
            //     log(constant) =  log( 1 / ( ((2PI)^(k/2)) * detSqrt) ) 
            //                   =  log(1)-log(((2PI)^(k/2)) * detSqrt) )
            //                   = -log(       ((2PI)^(k/2)) * detSqrt) )
            //                   = -log(       ((2PI)^(k/2)) * exp(log(detSqrt))))
            //                   = -log(       ((2PI)^(k/2)) * exp(lndetsqrt)))
            //                   = -log(       ((2PI)^(k/2)) * exp(lndet/2)))
            //                   = -log(       ((2PI)^(k/2))) - log(exp(lndet/2)))
            //                   = -log(       ((2PI)^(k/2))) - lndet/2)
            //                   = -log(         2PI) * (k/2) - lndet/2)
            //                   =(-log(         2PI) *  k    - lndet  ) / 2
            //                   =-(log(         2PI) *  k    + lndet  ) / 2
            //                   =-(log(         2PI) *  k    + lndet  ) / 2
            //                   =-(     LN2PI        *  k    + lndet  ) / 2
            //

            // So the log(constant) could be computed as:
            lnconstant = -(Special.Log2PI * k + lndet) * 0.5;
        }

        /// <summary>
        ///   Gets the Mean vector for the Gaussian distribution.
        /// </summary>
        /// 
        public override double[] Mean
        {
            get { return mean; }
        }

        /// <summary>
        ///   Gets the Variance vector for the Gaussian distribution.
        /// </summary>
        /// 
        public override double[] Variance
        {
            get
            {
                if (variance == null)
                    variance = Matrix.Diagonal(covariance);
                return variance;
            }
        }

        /// <summary>
        ///   Gets the variance-covariance matrix for the Gaussian distribution.
        /// </summary>
        /// 
        public override double[,] Covariance
        {
            get { return covariance; }
        }

        /// <summary>
        ///   This method is not supported.
        /// </summary>
        /// 
        public override double DistributionFunction(params double[] x)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range. For a
        ///   univariate distribution, this should be a single
        ///   double value. For a multivariate distribution,
        ///   this should be a double array.</param>
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
        public override double ProbabilityDensityFunction(params double[] x)
        {
            if (x.Length != Dimension)
                throw new DimensionMismatchException("x", "The vector should have the same dimension as the distribution.");

            double[] z = x.Subtract(mean);
            double[] a = chol.Solve(z);
            double b = a.InnerProduct(z);

            // Original code:
            // double r = constant * System.Math.Exp(-b/2);

            // Let lnconstant = log( constant )
            //
            //     r = constant * exp( b/2 )
            //
            //     r = exp(log(constant) * exp( b/2 )
            //     r = exp(lnconstant) * exp( b/2 )
            //     r = exp( lnconstant + b/2 )
            //

            double r = Math.Exp(lnconstant - b * 0.5);

            return r > 1 ? 1 : r;
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
        public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
        {
            double[] means;
            double[,] cov;

            if (weights != null)
            {
                // Compute weighted mean vector
                means = Statistics.Tools.Mean(observations, weights);

                // Compute weighted covariance matrix
                cov = Statistics.Tools.WeightedCovariance(observations, weights, means);
            }
            else
            {
                // Compute mean vector
                means = Statistics.Tools.Mean(observations);

                // Compute covariance matrix
                cov = Statistics.Tools.Covariance(observations, means);
            }

            CholeskyDecomposition chol = new CholeskyDecomposition(cov, false, true);

            if (options != null)
            {
                // Parse optional estimation options
                NormalOptions o = (NormalOptions)options;
                double regularization = o.Regularization;

                if (regularization > 0)
                {
                    int dimension = observations[0].Length;

                    while (!chol.PositiveDefinite)
                    {
                        for (int i = 0; i < dimension; i++)
                            cov[i, i] += regularization;

                        chol = new CholeskyDecomposition(cov, false, true);
                    }
                }
            }

            if (!chol.PositiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("Covariance matrix is not positive "
                    + "definite. Try specifying a regularization constant in the fitting options.");
            }

            // Become the newly fitted distribution.
            initialize(means, cov, chol);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static MultivariateNormalDistribution Estimate(double[][] observations)
        {
            return Estimate(observations, null, null);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static MultivariateNormalDistribution Estimate(double[][] observations, NormalOptions options)
        {
            return Estimate(observations, null, options);
        }

        /// <summary>
        ///   Estimates a new Normal distribution from a given set of observations.
        /// </summary>
        /// 
        public static MultivariateNormalDistribution Estimate(double[][] observations, double[] weights, NormalOptions options)
        {
            MultivariateNormalDistribution n = new MultivariateNormalDistribution(observations[0].Length);
            n.Fit(observations, weights, options);
            return n;
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
            var clone = new MultivariateNormalDistribution(this.Dimension, false);
            clone.lnconstant = lnconstant;
            clone.covariance = (double[,])covariance.Clone();
            clone.mean = (double[])mean.Clone();
            clone.chol = (CholeskyDecomposition)chol.Clone();

            return clone;
        }


        /// <summary>
        ///   Generates a random vector of observations from the current distribution.
        /// </summary>
        /// 
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        /// 
        public double[][] Generate(int samples)
        {
            var r = new AForge.Math.Random.StandardGenerator();
            double[,] A = chol.LeftTriangularFactor;

            double[][] data = new double[samples][];
            for (int i = 0; i < data.Length; i++)
            {
                double[] sample = new double[Dimension];
                for (int j = 0; j < sample.Length; j++)
                    sample[j] = r.Next();

                data[i] = A.Multiply(sample).Add(Mean);
            }

            return data;
        }

    }
}
