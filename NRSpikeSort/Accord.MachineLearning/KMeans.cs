// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//
// Copyright © Antonino Porcino, 2010
// iz8bly at yahoo.it
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

    /// <summary>
    ///   K-Means algorithm.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   In statistics and machine learning, k-means clustering is a method
    ///   of cluster analysis which aims to partition n observations into k 
    ///   clusters in which each observation belongs to the cluster with the
    ///   nearest mean.</para>
    /// <para>
    ///   It is similar to the expectation-maximization algorithm for mixtures
    ///   of Gaussians in that they both attempt to find the centers of natural
    ///   clusters in the data as well as in the iterative refinement approach
    ///   employed by both algorithms.</para> 
    /// 
    /// <para>
    ///   The algorithm is composed of the following steps:
    ///   <list type="number">
    ///     <item><description>
    ///         Place K points into the space represented by the objects that are
    ///         being clustered. These points represent initial group centroids.
    ///     </description></item>
    ///     <item><description>
    ///         Assign each object to the group that has the closest centroid.
    ///     </description></item>
    ///     <item><description>
    ///         When all objects have been assigned, recalculate the positions
    ///         of the K centroids.
    ///     </description></item>
    ///     <item><description>
    ///         Repeat Steps 2 and 3 until the centroids no longer move. This
    ///         produces a separation of the objects into groups from which the
    ///         metric to be minimized can be calculated.
    ///     </description></item>
    ///   </list></para>
    /// 
    /// <para>
    ///   This particular implementation uses the squared euclidean distance
    ///   as a similarity measure in order to form clusters. </para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       Wikipedia, The Free Encyclopedia. K-means clustering. Available on:
    ///       http://en.wikipedia.org/wiki/K-means_clustering </description></item>
    ///     <item><description>
    ///       Matteo Matteucci. A Tutorial on Clustering Algorithms. Available on:
    ///       http://home.dei.polimi.it/matteucc/Clustering/tutorial_html/kmeans.html </description></item>
    ///   </list></para>
    /// </remarks>
    /// <example>
    ///   How to perform clustering with K-Means.
    ///   <code>
    ///   // Declare some observations
    ///   double[][] observations = 
    ///   {
    ///       new double[] { -5, -2, -1 },
    ///       new double[] { -5, -5, -6 },
    ///       new double[] {  2,  1,  1 },
    ///       new double[] {  1,  1,  2 },
    ///       new double[] {  1,  2,  2 },
    ///       new double[] {  3,  1,  2 },
    ///       new double[] { 11,  5,  4 },
    ///       new double[] { 15,  5,  6 },
    ///       new double[] { 10,  5,  6 },
    ///   };
    ///  
    ///   // Create a new K-Means algorithm with 3 clusters 
    ///   KMeans kmeans = new KMeans(3);
    ///  
    ///   // Compute the algorithm, retrieving an integer array
    ///   //  containing the labels for each of the observations
    ///   int[] labels = kmeans.Compute(observations);
    ///  
    ///   // As result, the first two observations should belong to the
    ///   // same cluster (thus having the same label). The same should
    ///   // happen to the next four observations and to the last three.
    ///   </code>
    /// </example>
    ///
    [Serializable]
    public class KMeans
    {

        internal double[] proportions;
        internal double[][] centroids;
        internal double[][,] covariances;

        private KMeansClusterCollection clusters;
        private Func<double[], double[], double> distance;


        /// <summary>
        ///   Gets the clusters found by K-means.
        /// </summary>
        /// 
        public KMeansClusterCollection Clusters
        {
            get { return clusters; }
        }

        /// <summary>
        ///   Gets the number of clusters.
        /// </summary>
        /// 
        public int K
        {
            get { return clusters.Count; }
        }

        /// <summary>
        ///   Gets the dimensionality of the data space.
        /// </summary>
        /// 
        public int Dimension
        {
            get { return centroids[0].Length; }
        }

        /// <summary>
        ///   Gets or sets the distance function used
        ///   as a distance metric between data points.
        /// </summary>
        /// 
        public Func<double[], double[], double> Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        /// <summary>
        ///   Initializes a new instance of K-Means algorithm
        /// </summary>
        /// 
        /// <param name="k">The number of clusters to divide input data.</param>    
        /// 
        public KMeans(int k)
            : this(k, Accord.Math.Distance.SquareEuclidean)
        {
        }

        /// <summary>
        ///   Initializes a new instance of KMeans algorithm
        /// </summary>
        /// 
        /// <param name="k">The number of clusters to divide input data.</param>       
        /// <param name="distance">The distance function to use. Default is to
        /// use the <see cref="Accord.Math.Distance.SquareEuclidean"/> distance.</param>
        /// 
        public KMeans(int k, Func<double[], double[], double> distance)
        {
            if (k <= 0) throw new ArgumentOutOfRangeException("k");
            if (distance == null) throw new ArgumentNullException("distance");

            this.distance = distance;

            // To store centroids of the clusters
            this.proportions = new double[k];
            this.centroids = new double[k][];
            this.covariances = new double[k][,];

            // Create the object-oriented structure to hold
            //  information about the k-means' clusters.
            List<KMeansCluster> clusterList = new List<KMeansCluster>(k);
            for (int i = 0; i < k; i++)
                clusterList.Add(new KMeansCluster(this, i));
            this.clusters = new KMeansClusterCollection(this, clusterList);
        }


        /// <summary>
        ///   Randomizes the clusters inside a dataset.
        /// </summary>
        /// 
        /// <param name="data">The data to randomize the algorithm.</param>
        /// 
        public void Randomize(double[][] data)
        {
            if (data == null) throw new ArgumentNullException("data");

            // pick K unique random indexes in the range 0..n-1
            int[] idx = Accord.Statistics.Tools.Random(data.Length, K);

            // assign centroids from data set
            this.centroids = data.Submatrix(idx);
        }

        /// <summary>
        ///   Divides the input data into K clusters. 
        /// </summary>     
        /// 
        /// <param name="data">The data where to compute the algorithm.</param>
        /// 
        public int[] Compute(double[][] data)
        {
            return Compute(data, 1e-5);
        }

        /// <summary>
        ///   Divides the input data into K clusters. 
        /// </summary>    
        /// 
        /// <param name="data">The data where to compute the algorithm.</param>
        /// <param name="error">
        ///   The average square distance from the
        ///   data points to the clusters' centroids.
        /// </param>
        /// 
        public int[] Compute(double[][] data, out double error)
        {
            return Compute(data, 1e-5, out error);
        }

        /// <summary>
        ///   Divides the input data into K clusters. 
        /// </summary>     
        /// 
        /// <param name="data">The data where to compute the algorithm.</param>
        /// <param name="threshold">The relative convergence threshold
        /// for the algorithm. Default is 1e-5.</param>
        /// 
        public int[] Compute(double[][] data, double threshold)
        {
            // Initial argument checking
            if (data == null)
                throw new ArgumentNullException("data");

            if (threshold < 0)
                throw new ArgumentException("Threshold should be a positive number.", "threshold");


            // TODO: Implement a faster version using the triangle
            // inequality to reduce the number of distance calculations
            //
            //  - http://www-cse.ucsd.edu/~elkan/kmeansicml03.pdf
            //  - http://mloss.org/software/view/48/
            //

            int k = this.K;
            int rows = data.Length;
            int cols = data[0].Length;


            // Perform a random initialization of the clusters
            // if the algorithm has not been initialized before.
            if (this.centroids[0] == null)
            {
                Randomize(data);
            }


            // Initial variables
            int[] count = new int[k];
            int[] labels = new int[rows];
            double[][] newCentroids;


            do // Main loop
            {

                // Reset the centroids and the
                //  cluster member counters'
                newCentroids = new double[k][];
                for (int i = 0; i < k; i++)
                {
                    newCentroids[i] = new double[cols];
                    count[i] = 0;
                }


                // First we will accumulate the data points
                // into their nearest clusters, storing this
                // information into the newClusters variable.

                // For each point in the data set,
                for (int i = 0; i < data.Length; i++)
                {
                    // Get the point
                    double[] point = data[i];

                    // Compute the nearest cluster centroid
                    int c = labels[i] = Nearest(data[i]);

                    // Increase the cluster's sample counter
                    count[c]++;

                    // Accumulate in the corresponding centroid
                    double[] centroid = newCentroids[c];
                    for (int j = 0; j < centroid.Length; j++)
                        centroid[j] += point[j];
                }

                // Next we will compute each cluster's new centroid
                //  by dividing the accumulated sums by the number of
                //  samples in each cluster, thus averaging its members.
                for (int i = 0; i < k; i++)
                {
                    double[] mean = newCentroids[i];
                    double clusterCount = count[i];

                    if (clusterCount != 0)
                    {
                        for (int j = 0; j < cols; j++)
                            mean[j] /= clusterCount;
                    }
                }


                // The algorithm stops when there is no further change in the
                //  centroids (relative difference is less than the threshold).
                if (converged(centroids, newCentroids, threshold)) break;


                // go to next generation
                centroids = newCentroids;

            }
            while (true);


            // Compute cluster information (optional)
            for (int i = 0; i < k; i++)
            {
                // Extract the data for the current cluster
                double[][] sub = data.Submatrix(labels.Find(x => x == i));

                if (sub.Length > 0)
                {
                    // Compute the current cluster variance
                    covariances[i] = Statistics.Tools.Covariance(sub, centroids[i]);
                }
                else
                {
                    // The cluster doesn't have any samples
                    covariances[i] = new double[cols, cols];
                }

                // Compute the proportion of samples in the cluster
                proportions[i] = (double)sub.Length / data.Length;
            }


            // Return the classification result
            return labels;
        }

        /// <summary>
        ///   Divides the input data into K clusters. 
        /// </summary>  
        /// 
        /// <param name="data">The data where to compute the algorithm.</param>
        /// <param name="threshold">The relative convergence threshold
        /// for the algorithm. Default is 1e-5.</param>
        /// 
        /// <param name="error">
        ///   The average square distance from the
        ///   data points to the clusters' centroids.
        /// </param>
        /// 
        public int[] Compute(double[][] data, double threshold, out double error)
        {
            // Initial argument checking
            if (data == null) throw new ArgumentNullException("data");

            // Classify the input data
            int[] labels = Compute(data, threshold);

            // Compute the average error
            error = Error(data, labels);

            // Return the classification result
            return labels;
        }

        /// <summary>
        ///   Returns the closest cluster to an input vector.
        /// </summary>
        /// <param name="point">The input vector.</param>
        /// <returns>
        ///   The index of the nearest cluster
        ///   to the given data point. </returns>
        public int Nearest(double[] point)
        {
            int min_cluster = 0;
            double min_distance = distance(point, centroids[0]);

            for (int i = 1; i < centroids.Length; i++)
            {
                double dist = distance(point, centroids[i]);
                if (dist < min_distance)
                {
                    min_distance = dist;
                    min_cluster = i;
                }
            }

            return min_cluster;
        }

        /// <summary>
        ///   Returns the closest clusters to an input vector array.
        /// </summary>
        /// 
        /// <param name="points">The input vector array.</param>
        /// 
        /// <returns>
        ///   An array containing the index of the nearest cluster
        ///   to the corresponding point in the input array.</returns>
        ///   
        public int[] Nearest(double[][] points)
        {
            return points.Apply(p => Nearest(p));
        }

        /// <summary>
        ///   Calculates the average square distance from the data points
        ///   to the clusters' centroids.
        /// </summary>
        /// 
        /// <remarks>
        ///   The average distance from centroids can be used as a measure
        ///   of the "goodness" of the clusterization. The more the data
        ///   are aggregated around the centroids, the less the average
        ///   distance.
        /// </remarks>
        /// 
        /// <returns>
        ///   The average square distance from the data points to the
        ///   clusters' centroids.
        /// </returns>
        /// 
        public double Error(double[][] data)
        {
            return Error(data, Nearest(data));
        }

        /// <summary>
        ///   Calculates the average square distance from the data points
        ///   to the clusters' centroids.
        /// </summary>
        /// 
        /// <remarks>
        ///   The average distance from centroids can be used as a measure
        ///   of the "goodness" of the clusterization. The more the data
        ///   are aggregated around the centroids, the less the average
        ///   distance.
        /// </remarks>
        /// 
        /// <returns>
        ///   The average square distance from the data points to the
        ///   clusters' centroids.
        /// </returns>
        /// 
        public double Error(double[][] data, int[] labels)
        {
            double error = 0.0;

            for (int i = 0; i < data.Length; i++)
                error += distance(data[i], centroids[labels[i]]);

            return error / (double)data.Length;
        }

        /// <summary>
        ///   Determines if the algorithm has converged by comparing the
        ///   centroids between two consecutive iterations.
        /// </summary>
        /// 
        /// <param name="centroids">The previous centroids.</param>
        /// <param name="newCentroids">The new centroids.</param>
        /// <param name="threshold">A convergence threshold.</param>
        /// 
        /// <returns>Returns <see langword="true"/> if all centroids had a percentage change
        ///    less than <see param="threshold"/>. Returns <see langword="false"/> otherwise.</returns>
        ///    
        private static bool converged(double[][] centroids, double[][] newCentroids, double threshold)
        {
            for (int i = 0; i < centroids.Length; i++)
            {
                double[] centroid = centroids[i];
                double[] newCentroid = newCentroids[i];

                for (int j = 0; j < centroid.Length; j++)
                {
                    if ((System.Math.Abs((centroid[j] - newCentroid[j]) / centroid[j])) >= threshold)
                        return false;
                }
            }
            return true;
        }

    }

    /// <summary>
    ///   K-means' Cluster
    /// </summary>
    /// 
    [Serializable]
    public class KMeansCluster
    {
        private KMeans owner;
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
        ///   Gets the cluster's centroid.
        /// </summary>
        /// 
        public double[] Mean
        {
            get { return owner.centroids[index]; }
        }

        /// <summary>
        ///   Gets the cluster's variance-covariance matrix.
        /// </summary>
        /// 
        public double[,] Covariance
        {
            get { return owner.covariances[index]; }
        }

        /// <summary>
        ///   Gets the proportion of samples in the cluster.
        /// </summary>
        /// 
        public double Proportion
        {
            get { return owner.proportions[index]; }
        }

        internal KMeansCluster(KMeans owner, int index)
        {
            this.owner = owner;
            this.index = index;
        }
    }

    /// <summary>
    ///   K-means Cluster Collection.
    /// </summary>
    /// 
    [Serializable]
    public class KMeansClusterCollection : ReadOnlyCollection<KMeansCluster>
    {
        private KMeans owner;


        /// <summary>
        ///   Gets the clusters' variance-covariance matrices.
        /// </summary>
        /// 
        /// <value>The clusters' variance-covariance matrices.</value>
        /// 
        public double[][,] Covariances
        {
            get { return owner.covariances; }
        }

        /// <summary>
        ///   Gets or sets the clusters' centroids.
        /// </summary>
        /// 
        /// <value>The clusters' centroids.</value>
        /// 
        public double[][] Centroids
        {
            get { return owner.centroids; }
            set
            {
                if (value == owner.centroids)
                    return;

                if (value == null)
                    throw new ArgumentNullException("value");

                int k = owner.K;

                if (value.Length != k)
                    throw new ArgumentException("The number of centroids should be equal to K.", "value");

                // Make a deep copy of the
                // input centroids vector.
                for (int i = 0; i < k; i++)
                    owner.centroids[i] = (double[])value[i].Clone();

                // Reset derived information
                owner.covariances = new double[k][,];
                owner.proportions = new double[k];
            }
        }

        /// <summary>
        ///   Gets the proportion of samples in each cluster.
        /// </summary>
        /// 
        public double[] Proportions
        {
            get { return owner.proportions; }
        }

        internal KMeansClusterCollection(KMeans owner, IList<KMeansCluster> list)
            : base(list)
        {
            this.owner = owner;
        }
    }

}
