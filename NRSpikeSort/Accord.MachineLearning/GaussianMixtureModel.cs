// Accord Machine Learning Library
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

namespace Accord.MachineLearning
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Accord.Math;
    using Accord.Statistics.Distributions.Fitting;
    using Accord.Statistics.Distributions.Multivariate;

    /// <summary>
    ///   Gaussian Mixture Model Clustering.
    /// </summary>
    /// 
    /// <remarks>
    ///   Gaussian Mixture Models are one of most widely used model-based 
    ///   clustering methods. This specialized class provides a wrap-up
    ///   around the
    ///   <see cref="Statistics.Distributions.Multivariate.MultivariateMixture{Normal}">
    ///   Mixture&lt;NormalDistribution&gt;</see> distribution and provides
    ///   mixture initialization using the K-Means clustering algorithm.
    /// </remarks>
    /// 
    /// <example>
    ///   <code>
    ///   // Create a new Gaussian Mixture Model with 2 components
    ///   GaussianMixtureModel gmm = new GaussianMixtureModel(2);
    ///   
    ///   // Compute the model (estimate)
    ///   gmm.Compute(samples, 0.0001);
    ///   
    ///   // Classify a single sample
    ///   int c = gmm.Classify(sample);
    ///   </code>
    /// </example>
    /// 
    [Serializable]
    public class GaussianMixtureModel
    {
        // the underlying mixture distribution
        internal MultivariateMixture<MultivariateNormalDistribution> model;
        private GaussianClusterCollection clusters;


        /// <summary>
        ///   Gets the Gaussian components of the mixture model.
        /// </summary>
        /// 
        public GaussianClusterCollection Gaussians
        {
            get { return clusters; }
        }

        /// <summary>
        ///   Gets a mixture distribution modeled
        ///   by this Gaussian Mixture Model.
        /// </summary>
        /// 
        public MultivariateMixture<MultivariateNormalDistribution> GetMixtureDistribution()
        {
            return (MultivariateMixture<MultivariateNormalDistribution>)model.Clone();
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GaussianMixtureModel"/> class.
        /// </summary>
        /// 
        /// <param name="components">
        ///   The number of clusters in the clusterization problem. This will be
        ///   used to set the number of components in the mixture model.</param>
        ///   
        public GaussianMixtureModel(int components)
        {
            if (components <= 0)
            {
                throw new ArgumentOutOfRangeException("components",
                    "The number of components should be greater than zero.");
            }

            // Create the object-oriented structure to hold
            //   information about mixture model components.
            var clusterList = new List<GaussianCluster>(components);
            for (int i = 0; i < components; i++)
                clusterList.Add(new GaussianCluster(this, i));

            // Initialize the model using the created objects.
            this.clusters = new GaussianClusterCollection(clusterList);
        }

        /// <summary>
        ///   Initializes the model with initial values obtained 
        ///   throught a run of the K-Means clustering algorithm.
        /// </summary>
        /// 
        public double Initialize(double[][] data, double threshold)
        {
            double error = double.MaxValue;
            double errorNew;

            KMeans kmeans = new KMeans(clusters.Count);
            for (int i = 0; i < 100; ++i)
            {
                // Create a new K-Means algorithm
                KMeans _kmeans = new KMeans(clusters.Count);

                // Compute the K-Means
                _kmeans.Compute(data, threshold, out errorNew);

                // is this the best model so far?
                if (errorNew < error || i == 0)
                {
                    error = errorNew;
                    kmeans = _kmeans;
                }
                
            }
            // Initialize the model with K-Means
            Initialize(kmeans);
            return error;
        }

        /// <summary>
        ///   Initializes the model with initial values obtained 
        ///   throught a run of the K-Means clustering algorithm.
        /// </summary>
        /// 
        public void Initialize(KMeans kmeans)
        {
            int components = clusters.Count;

            if (kmeans.K != components)
                throw new ArgumentException("The number of clusters does not match.", "kmeans");

            // Initialize the Mixture Model with data from K-Means
            var proportions = kmeans.Clusters.Proportions;
            var distributions = new MultivariateNormalDistribution[components];

            for (int i = 0; i < components; i++)
            {
                double[] mean = kmeans.Clusters.Centroids[i];
                double[,] covariance = kmeans.Clusters.Covariances[i];

                if (!covariance.IsPositiveDefinite())
                    covariance = Matrix.Identity(kmeans.Dimension);

                distributions[i] = new MultivariateNormalDistribution(mean, covariance);
            }

            this.model = new MultivariateMixture<MultivariateNormalDistribution>(proportions, distributions);
        }

        /// <summary>
        ///   Initializes the model with initial values.
        /// </summary>
        /// 
        public void Initialize(MultivariateNormalDistribution[] distributions)
        {
            int components = clusters.Count;

            if (distributions.Length != components)
                throw new ArgumentException("The number distributions and clusters does not match.", "distributions");

            this.model = new MultivariateMixture<MultivariateNormalDistribution>(distributions);
        }

        /// <summary>
        ///   Divides the input data into K clusters modeling each
        ///   cluster as a multivariate Gaussian distribution. 
        /// </summary>     
        /// 
        public double Compute(double[][] data)
        {
            var options = new GaussianMixtureModelOptions();

            return Compute(data, options);
        }

        /// <summary>
        ///   Divides the input data into K clusters modeling each
        ///   cluster as a multivariate Gaussian distribution. 
        /// </summary>     
        /// 
        public double Compute(double[][] data, double threshold)
        {
            var options = new GaussianMixtureModelOptions
            {
                Threshold = threshold
            };

            return Compute(data, options);
        }

        /// <summary>
        ///   Divides the input data into K clusters modeling each
        ///   cluster as a multivariate Gaussian distribution. 
        /// </summary>     
        /// 
        public double Compute(double[][] data, double threshold, double regularization)
        {
            var options = new GaussianMixtureModelOptions()
            {
                Threshold = threshold,
                NormalOptions = new NormalOptions() { Regularization = regularization }
            };

            return Compute(data, options);
        }

        /// <summary>
        ///   Divides the input data into K clusters modeling each
        ///   cluster as a multivariate Gaussian distribution. 
        /// </summary>     
        /// 
        public double Compute(double[][] data, GaussianMixtureModelOptions options)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int components = this.clusters.Count;


            if (model == null)
            {
                // TODO: Perform K-Means multiple times to avoid
                //  a poor Gaussian Mixture model initialization.
                double error = Initialize(data, options.Threshold);
            }

            // Fit a multivariate Gaussian distribution
            var mixtureOptions = new MixtureOptions()
            {
                Threshold = options.Threshold,
                InnerOptions = options.NormalOptions,
            };

            model.Fit(data, mixtureOptions);


            // Return the log-likelihood as a measure of goodness-of-fit
            return model.LogLikelihood(data);
        }

        /// <summary>
        ///   Returns the most likely clusters of an observation.
        /// </summary>
        /// 
        /// <param name="observation">An input observation.</param>
        /// 
        /// <returns>
        ///   The index of the most likely cluster
        ///   of the given observation. </returns>
        ///   
        public int Classify(double[] observation)
        {
            if (observation == null)
                throw new ArgumentNullException("observation");

            int imax = 0;
            double max = model.ProbabilityDensityFunction(0, observation);

            for (int i = 1; i < model.Components.Length; i++)
            {
                double p = model.ProbabilityDensityFunction(i, observation);

                if (p > max)
                {
                    max = p;
                    imax = i;
                }
            }

            return imax;
        }

        /// <summary>
        ///   Returns the most likely clusters of an observation subject to the constraint that
        ///   the highest probability of the data point is higher than minProbability. In this case, 
        ///   the classifier will return -1.
        /// </summary>
        /// 
        /// <param name="observation">An input observation.</param>
        /// <param name=" minProbability">highest probability of the data point is higher
        /// than minProbability to be classified.</param>
        /// <returns>
        ///   The index of the most likely cluster
        ///   of the given observation. </returns>
        ///   
        public int Classify(double[] observation, double minProbability)
        {
            if (observation == null)
                throw new ArgumentNullException("observation");

            int imax = 0;
            double max = model.ProbabilityDensityFunction(0, observation);

            for (int i = 1; i < model.Components.Length; i++)
            {
                double p = model.ProbabilityDensityFunction(i, observation);

                if (p > max)
                {
                    max = p;
                    imax = i;
                }
            }

            if (max > minProbability)
                return imax;
            else
                return -1;
        }

        /// <summary>
        ///   Returns the most likely clusters of an observation.
        /// </summary>
        /// 
        /// <param name="observation">An input observation.</param>
        /// <param name="responses">The likelihood responses for each cluster.</param>
        /// 
        /// <returns>
        ///   The index of the most likely cluster
        ///   of the given observation. </returns>
        ///   
        public int Classify(double[] observation, out double[] responses)
        {
            if (observation == null)
                throw new ArgumentNullException("observation");

            responses = new double[model.Components.Length];

            for (int i = 0; i < responses.Length; i++)
                responses[i] = model.ProbabilityDensityFunction(i, observation);

            int imax;
            responses.Max(out imax);
            return imax;
        }

        /// <summary>
        ///   Returns the most likely clusters for an array of observations.
        /// </summary>
        /// 
        /// <param name="observations">An set of observations.</param>
        /// 
        /// <returns>
        ///   An array containing the index of the most likely cluster
        ///   for each of the given observations. </returns>
        ///   
        public int[] Classify(double[][] observations)
        {
            if (observations == null)
                throw new ArgumentNullException("observations");

            int[] result = new int[observations.Length];
            for (int i = 0; i < observations.Length; i++)
                result[i] = Classify(observations[i]);
            return result;
        }

        /// <summary>
        ///   Returns the most likely clusters for an array of observation subject to the constraint that
        ///   the highest probability a data point is higher than minProbability. In this case, 
        ///   the classifier will return -1 as the classification.
        /// </summary>
        /// 
        /// <param name="observations">An set of observations.</param>
        ///<param name=" minProbability">highest probability of the data point is higher
        /// than minProbability to be classified.</param>
        /// 
        /// <returns>
        ///   An array containing the index of the most likely cluster
        ///   for each of the given observations. </returns>
        ///   
        public int[] Classify(double[][] observations, double minProbability)
        {
            if (observations == null)
                throw new ArgumentNullException("observations");

            int[] result = new int[observations.Length];
            for (int i = 0; i < observations.Length; i++)
                result[i] = Classify(observations[i], minProbability);
            return result;
        }

    }

    /// <summary>
    ///   Options for Gaussian Mixture Model fitting.
    /// </summary>
    /// 
    public class GaussianMixtureModelOptions
    {

        /// <summary>
        ///   Gets or sets the convergence criterion for the
        ///   Expectation-Maximization algorithm. Default is 1e-3.
        /// </summary>
        /// 
        /// <value>The convergence threshold.</value>
        /// 
        public double Threshold { get; set; }

        /// <summary>
        ///   Gets or sets the fitting options for the component
        ///   Gaussian distributions of the mixture model.
        /// </summary>
        /// 
        /// <value>The fitting options for inner Gaussian distributions.</value>
        /// 
        public NormalOptions NormalOptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianMixtureModelOptions"/> class.
        /// </summary>
        /// 
        public GaussianMixtureModelOptions()
        {
            Threshold = 1e-3;
        }

    }

    /// <summary>
    ///   Gaussian Mixture Model Cluster
    /// </summary>
    /// 
    [Serializable]
    public class GaussianCluster
    {
        private GaussianMixtureModel owner;
        private int index;

        /// <summary>
        ///   Gets the label for this cluster.
        /// </summary>
        /// 
        public int Index
        {
            get { return this.index; }
        }

        /// <summary>
        ///   Gets the cluster's mean.
        /// </summary>
        /// 
        public double[] Mean
        {
            get
            {
                if (owner.model == null) return null;
                return owner.model.Components[index].Mean;
            }
        }

        /// <summary>
        ///   Gets the cluster's variance-covariance matrix.
        /// </summary>
        /// 
        public double[,] Covariance
        {
            get
            {
                if (owner.model == null) return null;
                return owner.model.Components[index].Covariance;
            }
        }

        /// <summary>
        ///   Gets the mixture coefficient for the cluster distribution.
        /// </summary>
        /// 
        public double Proportion
        {
            get
            {
                if (owner.model == null) return 0;
                return owner.model.Coefficients[index];
            }
        }

        /// <summary>
        ///   Gets the probability density function of the
        ///   underlying Gaussian probability distribution 
        ///   evaluated in point <c>x</c>.
        /// </summary>
        /// 
        /// <param name="x">An observation.</param>
        /// 
        /// <returns>
        ///   The probability of <c>x</c> occurring
        ///   in the weighted Gaussian distribution.
        /// </returns>
        /// 
        public double Probability(double[] x)
        {
            if (owner.model == null) return 0;
            return owner.model.ProbabilityDensityFunction(index, x);
        }

        /// <summary>
        ///   Gets the normal distribution associated with this cluster.
        /// </summary>
        /// 
        public MultivariateNormalDistribution GetDistribution()
        {
            if (owner.model == null)
                throw new InvalidOperationException("The model has not been initialized.");
            return (MultivariateNormalDistribution)owner.model.Components[index].Clone();
        }

        internal GaussianCluster(GaussianMixtureModel owner, int index)
        {
            this.owner = owner;
            this.index = index;
        }
    }

    /// <summary>
    ///   Gaussian Mixture Model Cluster Collection.
    /// </summary>
    /// 
    [Serializable]
    public class GaussianClusterCollection : ReadOnlyCollection<GaussianCluster>
    {
        internal GaussianClusterCollection(IList<GaussianCluster> list)
            : base(list)
        {
        }
    }

}
