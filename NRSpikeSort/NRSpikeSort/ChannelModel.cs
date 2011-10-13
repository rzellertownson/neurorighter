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
        
        // Private
        private int projectionDimension;
        private double[][] currentProjection;
        private int maxK; // maximum possible number of units
        private GaussianMixtureModel gmm;
        private PrincipalComponentAnalysis pca;
        private bool[] kVar;
        

        public ChannelModel(int channel, int maxK, int unitStartIndex)
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
        }

        public ChannelModel(int channel, int maxK, int unitStartIndex, int numPCs)
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
        }

        internal void Classify()
        {
            classes = gmm.Classify(currentProjection);
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

            //// Create transposed waveform matrix
            //waveforms = new double[numObs, wavelength];

            //for (int i = 0; i < numObs; ++i)
            //{
            //    for (int j = 0; j < wavelength; ++j)
            //    {
            //        waveforms[i, j] = spikes[i].waveform[j];
            //    }
            //}

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
        }
        

        #region Serialization Constructors/Deconstructors
        public ChannelModel(SerializationInfo info, StreamingContext ctxt)
        {
            this.kVals = (int[])info.GetValue("kVals", typeof(int[]));
            this.logLike = (double[])info.GetValue("logLike", typeof(double[]));
            this.rissanen = (double[])info.GetValue("rissanen", typeof(double[]));
            this.mdl = (double[])info.GetValue("mdl", typeof(double[]));
            this.channelNumber = (int)info.GetValue("channelNumber", typeof(int));
            //this.classes = (int[])info.GetValue("classes", typeof(int[]));
            this.K = (int)info.GetValue("K", typeof(int));
            this.projectionDimension = (int)info.GetValue("projectionDimension",typeof(int));
            this.currentProjection = (double[][])info.GetValue("currentProjection", typeof(double[][]));
            this.maxK = (int)info.GetValue("maxK", typeof(int));
            this.gmm = (GaussianMixtureModel)info.GetValue("gmm", typeof(GaussianMixtureModel));
            this.pca = (PrincipalComponentAnalysis)info.GetValue("pca", typeof(PrincipalComponentAnalysis));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("kVals", this.kVals);
            info.AddValue("logLike", this.logLike);
            info.AddValue("rissanen", this.rissanen);
            info.AddValue("mdl", this.mdl);
            info.AddValue("channelNumber", this.channelNumber);
            //info.AddValue("classes", this.classes);
            info.AddValue("K", this.K);
            info.AddValue("projectionDimension",this.projectionDimension);
            info.AddValue("currentProjection", this.currentProjection);
            info.AddValue("maxK", this.maxK);
            info.AddValue("gmm", this.gmm);
            info.AddValue("pca", this.pca);
        }

        #endregion

        

    }
}
