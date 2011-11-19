using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Math;
using Accord.Statistics.Distributions.Multivariate;
using NeuroRighter.DataTypes;
using Accord.Statistics.Analysis;
using Accord.MachineLearning;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NRSpikeSort.Extensions;

namespace NRSpikeSort
{
    [Serializable()]
    public class ChannelModel : ISerializable
    {
        /// <summary>
        /// The values of K that are going to be optimized over during training
        /// </summary>
        public int[] kVals;

        /// <summary>
        /// Log likelihood as a goodness of fit for this channels gmm
        /// </summary>
        public double[] logLike; // Log-likelihood estimate for a given model and training set

        /// <summary>
        /// Rissanens for different values of K
        /// </summary>
        public double[] rissanen; // Rissanen estimate for a given model and training set

        /// <summary>
        /// Minimumd description lengths for different values of K
        /// </summary>
        public double[] mdl; // Minimum description length for a given model and training set

        /// <summary>
        /// Number of units detected on this channel
        /// </summary>
        public int K;// number of subclasses

        /// <summary>
        /// The index to get an absolute unit numbers for units detected by this channel model. 
        /// This is unit + unitStartIndex + 1.
        /// </summary>
        public int unitStartIndex;

        /// <summary>
        /// What channel is this model being used for?
        /// </summary>
        public int channelNumber; // The number of the channel this model is being used for

        /// <summary>
        /// classifiction of the current projection
        /// </summary>
        internal int[] classes;

        internal bool trained;

        // Private
        private int projectionDimension;
        private double[][] currentProjection;
        private int maxK; // maximum possible number of units
        private GaussianMixtureModel gmm;
        private PrincipalComponentAnalysis pca;
        // The minimum probability of an observation to be pulled from any
        // of the Gaussian distriubtions making up the mixture for it to be 
        // considered a member of any class.
        private double numSTD = 10;
        private double minWaveformsForClass = 0.1;
        private int[] dimToUse;
        private bool[] kVar;

        public ChannelModel(int channel, int maxK, int unitStartIndex, double numSTD)
        {
            // Parameterize this channel model
            this.channelNumber = channel;
            this.maxK = maxK;
            this.kVals = new int[maxK];
            this.unitStartIndex = unitStartIndex;
            for (int i = 0; i < maxK; ++i)
            {
                this.kVals[i] = maxK - i;
            }
            this.projectionDimension = 1;
            this.numSTD = numSTD;
            this.trained = false;
        }

        public ChannelModel(int channel, int maxK, int unitStartIndex, double numSTD, int numPCs)
        {
            // Parameterize this channel model
            this.channelNumber = channel;
            this.maxK = maxK;
            this.kVals = new int[maxK];
            this.unitStartIndex = unitStartIndex;
            for (int i = 0; i < maxK; ++i)
            {
                this.kVals[i] = maxK - i;
            }
            this.projectionDimension = numPCs;
            this.numSTD = numSTD;
            this.trained = false;
        }

        internal void Classify()
        {
            classes = gmm.Classify(currentProjection);
        }

        internal void ClassifyThresh()
        {
            classes = gmm.ClassifyThresh(currentProjection);
        }

        internal void MaxInflectProject(List<SpikeEvent> spikes, int maxInflectionIndex)
        {

            // Create waveform matrix
            int numObs = spikes.Count;
            currentProjection = new double[numObs][];
            for (int i = 0; i < numObs; ++i)
            {
                currentProjection[i] = new double[1];
                currentProjection[i][0] = 10000000000 * spikes[i].waveform[maxInflectionIndex];
            }
        }

        internal void PCCompute(List<SpikeEvent> spikes)
        {
            // Matrix dimensions
            int numObs = spikes.Count;
            int wavelength = spikes[0].waveform.Length;

            // Create waveform matrix
            double[,] waveforms = new double[numObs, wavelength];

            for (int i = 0; i < numObs; ++i)
            {
                for (int j = 0; j < wavelength; ++j)
                {
                    waveforms[i, j] = spikes[i].waveform[j];
                }
            }


            // Make PCA object
            pca = new PrincipalComponentAnalysis(waveforms, AnalysisMethod.Standardize);

            // PC Decomp.
            pca.Compute();

            // Project
            currentProjection = new double[numObs][];
            double[,] tmp = pca.Transform(waveforms);
            for (int i = 0; i < tmp.GetLength(0); ++i)
            {
                currentProjection[i] = new double[projectionDimension];
                for (int j = 0; j < projectionDimension; ++j)
                    currentProjection[i][j] = tmp[i, j];
            }

            //// Create projection matrix
            //double maxPC = double.MinValue;

            //currentProjection = new double[numObs][];
            //for (int i = 0; i < numObs; ++i)
            //{
            //    currentProjection[i] = new double[projectionDimension];

            //    for (int j = 0; j < projectionDimension; ++j)
            //    {
            //        currentProjection[i][j] = pca.ComponentMatrix[i, j];
            //        if (currentProjection[i][j] > maxPC)
            //        {
            //            maxPC = currentProjection[i][j];
            //        }
            //    }
            //}

            //// Normalize projection
            //for (int i = 0; i < numObs; ++i)
            //{
            //    for (int j = 0; j < projectionDimension; ++j)
            //    {
            //        currentProjection[i][j] = 10000 * (currentProjection[i][j] / maxPC);
            //    }
            //}
        }

        internal void PCProject(List<SpikeEvent> spikes)
        {
            // Matrix dimensions
            int numObs = spikes.Count;
            int wavelength = spikes[0].waveform.Length;

            // Create waveform matrix
            double[,] waveforms = new double[numObs, wavelength];

            for (int i = 0; i < numObs; ++i)
            {
                for (int j = 0; j < wavelength; ++j)
                {
                    waveforms[i, j] = spikes[i].waveform[j];
                }
            }

            // Project
            currentProjection = new double[numObs][];
            double[,] tmp = pca.Transform(waveforms);
            for (int i = 0; i < tmp.GetLength(0); ++i)
            {
                currentProjection[i] = new double[projectionDimension];
                for (int j = 0; j < projectionDimension; ++j)
                    currentProjection[i][j] = tmp[i, j];
            }
        }

        internal void HaarProject(List<SpikeEvent> spikes)
        {
            // Intialize storage
            int numObs = spikes.Count;
            currentProjection = new double[numObs][];

            // Create current projection
            for (int i = 0; i < numObs; ++i)
            {
                // Project into spike wavelet space
                double[] tmp = HaarProject1D(spikes[i].waveform);

                currentProjection[i] = new double[projectionDimension];
                for (int j = 0; j < projectionDimension; ++j)
                    currentProjection[i][j] = tmp[dimToUse[j]];
            }
        }

        internal void HaarCompute(List<SpikeEvent> spikes)
        {

            // Intialize storage
            int numObs = spikes.Count;

            currentProjection = new double[numObs][];

            // Create current projection
            for (int i = 0; i < numObs; ++i)
            {
                // Project into spike wavelet space
                double[] tmp = HaarProject1D(spikes[i].waveform);

                // Use the whole projection this time
                currentProjection[i] = tmp;

            }

            // Get the KS-Stats for each possible wavelet dimension
            List<double> ksStats = new List<double>();
            int[] ksIndex = new int[currentProjection[0].Length];
            for (int i = 0; i < ksIndex.Length; ++i)
            {
                // Make a frequency stats table out of a column of the projection matrix
                double[] singleProjDim = Matrix.GetColumn(currentProjection, i);

                // Remove outliers
                List<double> auxProjDim = RemoveVectorOutliers(singleProjDim);

                // Get KS-stats for that column
                if (auxProjDim.Count > 10)
                    ksStats.Add(KolmogorovSmirnovTestStatistic(auxProjDim));
                else
                    ksStats.Add(0.0);

                // Iterate
                ksIndex[i] = i;

            }

            // Find the largets projectionDim ksStats and their indicies. These are the dimensions of the
            // reduced projection
            Array.Sort<int>(ksIndex, (a, b) => ksStats[a].CompareTo(ksStats[b]));
            dimToUse = ksIndex.ToList().GetRange(ksIndex.Length - projectionDimension, projectionDimension).ToArray();

            // Most Gaussian coefficients
            dimToUse = dimToUse.ToArray();

            // Least Gaussian coefficients
            //dimToUse = dimToUse.Reverse().ToArray(); 

            // Create current projection
            for (int i = 0; i < numObs; ++i)
            {
                // Project into spike wavelet space
                double[] tmp = HaarProject1D(spikes[i].waveform, 5);

                currentProjection[i] = new double[projectionDimension];
                for (int j = 0; j < projectionDimension; ++j)
                    currentProjection[i][j] = tmp[dimToUse[j]];
            }
        }

        private double[] HaarProject1D(double[] inputVector, int level)
        {
            double[] inVec = inputVector.DeepCopy();
            int w = inVec.Length;
            double[] vecp = new double[w];
            double[] vecTrans = new double[w];

            if (level < 0)
                throw new InvalidOperationException("Level arguement to HaarProject1D must be a strictly postivie integer");

            int l = 0;
            while (w > 1 && l < level)
            {
                w /= 2;
                for (int i = 0; i < w; ++i)
                {
                    vecp[i] = (inVec[2 * i] + inVec[2 * i + 1]) / Extensions.ExtensionMethods.Sqrt2;
                    vecp[i + w] = (inVec[2 * i] - inVec[2 * i + 1]) / Extensions.ExtensionMethods.Sqrt2;
                }

                for (int i = 0; i < w; ++i)
                {
                    inVec[i] = vecp[i];
                    vecTrans[i + w] = vecp[i + w];
                }

                l++;

            }

            // Last elements
            for (int i = 0; i < w; ++i)
                vecTrans[i] = vecp[i];

            return vecTrans;
        }

        private double[] HaarProject1D(double[] inputVector)
        {
            double[] inVec = inputVector.DeepCopy();
            int w = inVec.Length;
            double[] vecp = new double[w];
            double[] vecTrans = new double[w];

            while (w > 1)
            {
                w /= 2;
                for (int i = 0; i < w; ++i)
                {
                    vecp[i] = (inVec[2 * i] + inVec[2 * i + 1]) / Extensions.ExtensionMethods.Sqrt2;
                    vecp[i + w] = (inVec[2 * i] - inVec[2 * i + 1]) / Extensions.ExtensionMethods.Sqrt2;
                }

                for (int i = 0; i < w; ++i)
                {
                    inVec[i] = vecp[i];
                    vecTrans[i + w] = vecp[i + w];
                }
            }

            // Last element
            vecTrans[0] = vecp[0];

            return vecTrans;
        }

        /// <summary>
        /// KS test against normal distribution with unknown mean and variance. Provides
        /// the KS statistic as on output.
        /// </summary>
        /// <param name="dataVector"> The data vector that is punatively pull from a normal distribution with unknown mean and variance</param>
        /// <returns></returns>
        private double KolmogorovSmirnovTestStatistic(List<double> dataVector)
        {
            // 1. Sort the data vector in decending order and remove duplicates
            dataVector.Sort();
            dataVector.Distinct();
            int dataLength = dataVector.Count;

            // 2. Calculate the mean and variance of the data vector
            double mu = dataVector.Average();
            double sigma = dataVector.StandardDeviation();

            // 2. Create Expnential CDF, calculate z-score of data vector, create theoretical cdf
            double[] yExpCDF = new double[dataLength + 1];
            double[] zScores = new double[dataLength];
            double[] erfVal = new double[dataLength];
            double[] theoCDF = new double[dataLength];

            for (int i = 0; i <= dataLength; ++i)
            {
                yExpCDF[i] = Convert.ToDouble(i) / dataLength;
            }

            for (int i = 0; i < dataLength; ++i)
            {
                zScores[i] = (dataVector[i] - mu) / sigma;
                erfVal[i] = (-zScores[i] / Extensions.ExtensionMethods.Sqrt2).ErrorFunction();
                theoCDF[i] = 0.5 * (1 - erfVal[i]);
            }


            // 3. Calculate the KS statistic
            double[] delta = new double[2 * dataLength];
            for (int i = 0; i < dataLength; ++i)
            {
                delta[i] = Math.Abs(yExpCDF[i] - theoCDF[i]);
                delta[2 * i] = Math.Abs(yExpCDF[i + 1] - theoCDF[i]);
            }

            return delta.Max();
        }

        /// <summary>
        /// Removes data points from a vector that are more than three standard deviations
        /// from the mean value of that vector
        /// </summary>
        /// <param name="dataVector">Input vector</param>
        /// <returns>Data vector with outliers removed</returns>
        private List<double> RemoveVectorOutliers(double[] dataVector)
        {
            // Find mean and std of input data
            double mu = dataVector.Average();
            double sigma = dataVector.StandardDeviation();

            // Remove outliers
            List<double> dataVectorList = dataVector.ToList();
            dataVectorList = dataVectorList.Where(x => Math.Abs(x - mu) < (3 * sigma)).ToList();

            // Return vector
            return dataVectorList;

        }

        internal List<double[]> Return2DProjection()
        {
            List<double[]> twoDProjection = new List<double[]>();
            if (projectionDimension == 1)
            {
                for (int i = 0; i < currentProjection.Length; ++i)
                {
                    double[] tmp = new double[2];
                    tmp[0] = currentProjection[i][0];
                    tmp[1] = 0;
                    twoDProjection.Add(tmp);
                }
            }
            else
            {
                for (int i = 0; i < currentProjection.Length; ++i)
                {
                    double[] tmp = new double[2];
                    tmp[0] = currentProjection[i][0];
                    tmp[1] = currentProjection[i][1];
                    twoDProjection.Add(tmp);
                }
            }

            return twoDProjection;
        }

        internal void Train()
        {
            logLike = new double[maxK]; // Log-likelihood estimate for a given model and training set
            rissanen = new double[maxK]; // Rissanen estimate for a given model and training set
            mdl = new double[maxK]; // Minimum description length for a given model and training set
            kVar = new bool[maxK];
            for (int i = 0; i < maxK; ++i)
            {
                // Step 1: Make a GMM with K subclasses
                gmm = new GaussianMixtureModel(kVals[i]);

                // Step 2: fit the gmm to the projection data
                logLike[i] = gmm.Compute(currentProjection, 1e-3, 1.0);

                // Step 3: perform a classification to detect spurious classification
                kVar[i] = (kVals[i] == gmm.Classify(currentProjection).Distinct().ToArray().Length);

                // Step 4: Calculate the MDL for this K
                double L = (double)(kVals[i] * 3 - 1);
                rissanen[i] = 0.5 * L * Math.Log(currentProjection.Length);
                mdl[i] = -logLike[i] + rissanen[i];
            }

            // Which value of K supported the MDL:
            int ind = Array.IndexOf(mdl, mdl.Min());

            // Find the value of K that supports the lowest mdl and is verified 
            K = kVals[ind];
            while (!kVar[ind])
            {
                K = kVals[ind];
                ++ind;
            }

            // Recreate the gmm with the trained value of K
            gmm = new GaussianMixtureModel(K);
            double LL = gmm.Compute(currentProjection, 1e-3, 1.0);

            // Is this a functional classifier?
            if (gmm != null)
                trained = true;

            // Set the STD ellipsoid
            gmm.SetStdEllipsoid(numSTD);
        }

        #region Serialization Constructors/Deconstructors
        public ChannelModel(SerializationInfo info, StreamingContext ctxt)
        {
            this.kVals = (int[])info.GetValue("kVals", typeof(int[]));
            this.logLike = (double[])info.GetValue("logLike", typeof(double[]));
            this.rissanen = (double[])info.GetValue("rissanen", typeof(double[]));
            this.mdl = (double[])info.GetValue("mdl", typeof(double[]));
            this.channelNumber = (int)info.GetValue("channelNumber", typeof(int));
            this.K = (int)info.GetValue("K", typeof(int));
            this.projectionDimension = (int)info.GetValue("projectionDimension", typeof(int));
            this.currentProjection = (double[][])info.GetValue("currentProjection", typeof(double[][]));
            this.maxK = (int)info.GetValue("maxK", typeof(int));
            this.gmm = (GaussianMixtureModel)info.GetValue("gmm", typeof(GaussianMixtureModel));
            this.pca = (PrincipalComponentAnalysis)info.GetValue("pca", typeof(PrincipalComponentAnalysis));
            this.unitStartIndex = (int)info.GetValue("unitStartIndex", typeof(int));
            this.numSTD = (double)info.GetValue("numSTD",typeof(double));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("kVals", this.kVals);
            info.AddValue("logLike", this.logLike);
            info.AddValue("rissanen", this.rissanen);
            info.AddValue("mdl", this.mdl);
            info.AddValue("channelNumber", this.channelNumber);
            info.AddValue("K", this.K);
            info.AddValue("projectionDimension", this.projectionDimension);
            info.AddValue("currentProjection", this.currentProjection);
            info.AddValue("maxK", this.maxK);
            info.AddValue("gmm", this.gmm);
            info.AddValue("pca", this.pca);
            info.AddValue("unitStartIndex", this.unitStartIndex);
            info.AddValue("numSTD", this.numSTD);
        }

        #endregion

    }
}
