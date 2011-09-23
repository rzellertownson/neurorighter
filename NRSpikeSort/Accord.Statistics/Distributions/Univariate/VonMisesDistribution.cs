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
    ///   von-Mises (Circular Normal) distribution.
    /// </summary>
    /// 
    /// <remarks>
    ///   <para>The von Mises distribution (also known as the circular normal distribution
    ///   or Tikhonov distribution) is a continuous probability distribution on the circle.
    ///   It may be thought of as a close approximation to the wrapped normal distribution,
    ///   which is the circular analogue of the normal distribution.</para>
    ///   
    ///   <para>The wrapped normal distribution describes the distribution of an angle that
    ///   is the result of the addition of many small independent angular deviations, such as
    ///   target sensing, or grain orientation in a granular material. The von Mises distribution
    ///   is more mathematically tractable than the wrapped normal distribution and is the
    ///   preferred distribution for many applications.</para>
    ///   
    /// <para>    
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Von_Mises_distribution">
    ///       Wikipedia, The Free Encyclopedia. Von-Mises distribution. Available on:
    ///       http://en.wikipedia.org/wiki/Von_Mises_distribution </a></description></item>
    ///     <item><description><a href="http://www.kyb.mpg.de/publications/attachments/vmfnote_7045%5B0%5D.pdf">
    ///       Suvrit Sra, "A short note on parameter approximation for von Mises-Fisher distributions:
    ///       and a fast implementation of $I_s(x)$". (revision of Apr. 2009). Computational Statistics (2011).
    ///       Available on: http://www.kyb.mpg.de/publications/attachments/vmfnote_7045%5B0%5D.pdf </a></description></item>
    ///     <item><description>
    ///       Zheng Sun. M.Sc. Comparing measures of fit for circular distributions. Master thesis, 2006.
    ///       Available on: https://dspace.library.uvic.ca:8443/bitstream/handle/1828/2698/zhengsun_master_thesis.pdf </description></item>
    ///   </list></para>
    /// </remarks>
    /// 
    [Serializable]
    public class VonMisesDistribution : UnivariateContinuousDistribution
    {
        // Distribution parameters
        private double mean;
        private double kappa;

        // Distribution measures
        private double? variance;
        private double? entropy;

        // Derived measures
        private double constant;


        /// <summary>
        ///   Constructs a multivariate Gaussian distribution
        ///   with zero mean vector and unitary variance matrix.
        /// </summary>
        /// 
        /// <param name="mean">The mean of the distribution.</param>
        /// <param name="concentration">The concentration value (kappa) for the distribution.</param>
        /// 
        public VonMisesDistribution(double mean, double concentration)
        {
            initialize(mean, concentration);
        }

        private VonMisesDistribution()
        {
        }

        private void initialize(double m, double k)
        {
            this.mean = m;
            this.kappa = k;

            this.variance = null;
            this.constant = 1.0 / (2.0 * Math.PI * Special.BesselI0(kappa));
        }

        /// <summary>
        ///   Gets the mean for this distribution.
        /// </summary>
        /// 
        public override double Mean
        {
            get { return mean; }
        }

        /// <summary>
        ///   Gets the concentration (the kappa value) for this distribution.
        /// </summary>
        /// 
        /// <value>The concentration.</value>
        /// 
        public double Concentration
        {
            get { return kappa; }
        }

        /// <summary>
        ///   Gets the variance for this distribution.
        /// </summary>
        /// 
        public override double Variance
        {
            get
            {
                if (!variance.HasValue)
                {
                    double i1 = Special.BesselI(1, kappa);
                    double i0 = Special.BesselI0(kappa);
                    double a = i1 / i0;
                    variance = 1.0 - a;
                }

                return variance.Value;
            }
        }

        /// <summary>
        ///   Gets the entropy for this distribution.
        /// </summary>
        /// 
        public override double Entropy
        {
            get
            {
                if (!entropy.HasValue)
                {
                    double i1 = Special.BesselI(1, kappa);
                    double i0 = Special.BesselI0(kappa);
                    double a = i1 / i0;
                    entropy = -kappa * a + Math.Log(2 * Math.PI * i0);
                }

                return entropy.Value;
            }
        }

        /// <summary>
        ///   Not supported. The distribution function for the
        ///   von-Mises distribution is not analytic and no
        ///   approximation has been provided yet.
        /// </summary>
        /// 
        public override double DistributionFunction(double x)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///   Gets the probability density function (pdf) for
        ///   this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">A single point in the distribution range.</param>
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
            return constant * Math.Exp(kappa * Math.Cos(x - mean));
        }


        /// <summary>
        ///   Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// 
        /// <param name="observations">The array of observations to fit the model against. The array
        ///   elements can be either of type double (for univariate data) or
        ///   type double[] (for multivariate data).</param>
        /// 
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
            double m, k;

            if (weights != null)
            {
                m = Circular.WeightedMean(observations, weights);
                k = Circular.WeightedConcentration(observations, weights, m);
            }
            else
            {
                m = Circular.Mean(observations);
                k = Circular.Concentration(observations, m);
            }

            if (options != null)
            {
                // Parse optional estimation options
                VonMisesOptions o = (VonMisesOptions)options;
                if (o.UseBiasCorrection)
                {
                    double N = observations.Length;
                    if (k < 2)
                    {
                        k = System.Math.Max(k - 1.0 / (2.0 * (N * k)), 0);
                    }
                    else
                    {
                        double Nm1 = N - 1;
                        k = (Nm1 * Nm1 * Nm1 * k) / (N * N * N + N);
                    }
                }
            }

            initialize(m, k);
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
            return new VonMisesDistribution(mean, kappa);
        }


        /// <summary>
        ///   Estimates a new von-Mises distribution from a given set of angles.
        /// </summary>
        /// 
        public static VonMisesDistribution Estimate(double[] angles)
        {
            return Estimate(angles, null, null);
        }

        /// <summary>
        ///   Estimates a new von-Mises distribution from a given set of angles.
        /// </summary>
        /// 
        public static VonMisesDistribution Estimate(double[] angles, VonMisesOptions options)
        {
            return Estimate(angles, null, options);
        }

        /// <summary>
        ///   Estimates a new von-Mises distribution from a given set of angles.
        /// </summary>
        /// 
        public static VonMisesDistribution Estimate(double[] angles, double[] weights, VonMisesOptions options)
        {
            VonMisesDistribution vonMises = new VonMisesDistribution();
            vonMises.Fit(angles, weights, options);
            return vonMises;
        }


    }
}
