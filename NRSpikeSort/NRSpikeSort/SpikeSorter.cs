//RapidSort - A fast, unsupervised spike sorting program
//Copyright (C) 2011  Jonathan Newman

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU Lesser General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU Lesser General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NRSpikeSort
{
    [Serializable()]
    public class SpikeSorter : ISerializable
    {
        /// <summary>
        /// Says whether the classifiers have been trained yet.
        /// </summary>
        public bool trained;

        /// <summary>
        /// List of Gaussian Mixture models used to classify spikes. One for each electrode.
        /// </summary>
        public List<ChannelModel> channelModels;

        /// <summary>
        /// The data used to train the spike sorter.
        /// </summary>
        public EventBuffer<SpikeEvent> trainingSpikes = new EventBuffer<SpikeEvent>(25000);

        /// <summary>
        /// The total number of units found on the electrode array
        /// </summary>
        public int totalNumberOfUnits = 0;

        /// <summary>
        /// This hash allows one to switch back and forth between the absolute unit number 
        /// and the unit number for a given channel. (e.g. unit 78 => unit 2 on chan 28).
        /// </summary>
        public Hashtable unitDictionary;

        /// <summary>
        /// Maximum number of units that could be detected per channel.
        /// </summary>
        public int maxK; // maximum number of subclasses

        /// <summary>
        /// Minimum number of training spikes collected on a channel to creat a sorter for 
        /// that channel.
        /// </summary>
        public int minSpikes; // minimum number of spikes required to attempt sorting

        /// <summary>
        /// The channels to be sorted
        /// </summary>
        public List<int> channelsToSort; // The channels that had enough data to sort

        /// <summary>
        /// Number of spikes detected for training on each channel.
        /// </summary>
        public Hashtable spikesCollectedPerChannel;

        /// <summary>
        /// The type of projection to produce sortable pixels.
        /// "Maximum Voltage Inflection" - peak aligned maximum voltage from spike waveforms
        /// "PCA" - Principle component projection of spike waveforms
        /// "Haar" - Haar Wavelet decomposition
        /// </summary>
        public string projectionType;

        /// <summary>
        /// The number of dimensions in the coordinate system into which spike data will be projected.
        /// </summary>
        public int projectionDimension;

        /// <summary>
        /// The maximum number of waveforms used for training a GMM on each channel
        /// </summary>
        public int maxTrainingSpikesPerChannel = 150;

        /// <summary>
        /// Minimum probability nessesary to classify a spike
        /// </summary>
        public double numSTD = 10;

        /// <summary>
        /// The sample at which the peak of the spike waveform occurs
        /// </summary>
        public int inflectionSample = 14; // The sample that spike peaks occur at

        // Private
        private int numberChannels;
        
        /// <summary>
        /// NeuroRighter's spike sorter.
        /// </summary>
        /// <param name="numberChannels">Number of channels to make sorters for</param>
        /// <param name="maxK">Maximum number of units to consider per channel</param>
        /// <param name="minSpikes">Minimum number of detected training spikes to create a sorter for a given channel</param>
        public SpikeSorter(int numberChannels, int maxK, int minSpikes, double numSTD)
        {
            this.numberChannels = numberChannels;
            this.maxK = maxK;
            this.minSpikes = minSpikes;
            this.channelModels = new List<ChannelModel>(numberChannels);
            this.spikesCollectedPerChannel = new Hashtable();
            for (int i = 0; i < numberChannels; ++i)
            {
                spikesCollectedPerChannel.Add(i + 1, 0);
            }
            this.projectionType = "MaxInflection";
            this.projectionDimension = 1;
            this.numSTD = numSTD;
        }

        /// <summary>
        /// NeuroRighter's spike sorter.
        /// </summary>
        /// <param name="numberChannels">Number of channels to make sorters for</param>
        /// <param name="maxK">Maximum number of units to consider per channel</param>
        /// <param name="minSpikes">Minimum number of detected training spikes to create a sorter for a given channel</param>
        /// <param name="projectionDim">Dimension of projection if using PCA</param>
        public SpikeSorter(int numberChannels, int maxK, int minSpikes, double numSTD, int projectionDim)
        {
            this.numberChannels = numberChannels;
            this.maxK = maxK;
            this.minSpikes = minSpikes;
            this.channelModels = new List<ChannelModel>(numberChannels);
            this.spikesCollectedPerChannel = new Hashtable();
            for (int i = 0; i < numberChannels; ++i)
            {
                spikesCollectedPerChannel.Add(i + 1, 0);
            }
            this.projectionType = "PCA";
            this.projectionDimension = projectionDim;
            this.numSTD = numSTD;
        }

        /// <summary>
        /// Hoard spikes to populate the buffer of spikes that will be used to train the classifiers
        /// </summary>
        /// <param name="newSpikes"> An EventBuffer contain spikes to add to the training buffer</param>
        public void HoardSpikes(EventBuffer<SpikeEvent> newSpikes)
        {
            for (int i = 0; i < newSpikes.eventBuffer.Count; ++i)
            {
                if (!((int)spikesCollectedPerChannel[newSpikes.eventBuffer[i].channel + 1] >= maxTrainingSpikesPerChannel))
                {
                    spikesCollectedPerChannel[newSpikes.eventBuffer[i].channel + 1] = (int)spikesCollectedPerChannel[newSpikes.eventBuffer[i].channel + 1] + 1;
                    trainingSpikes.eventBuffer.Add(newSpikes.eventBuffer[i]);
                }
            }
        }

        /// <summary>
        /// Trains a classifier for each channel so long as (int)minSpikes worth of spikes have been collected
        /// for that channel in the training buffer
        /// </summary>
        /// <param name="peakSample"> The sample within a spike snippet that corresponds to the aligned peak.</param>
        public void Train(int peakSample)
        {
            // Clear old channel models
            channelsToSort = new List<int>();
            channelModels.Clear();
            trained = false;
            totalNumberOfUnits = 0;

            // Clear old unit dictionary
            unitDictionary = new Hashtable();

            // Add the zero unit to the dictionary
            unitDictionary.Add(0, 0);

            // Set the inflection sample
            inflectionSample = peakSample;

            // Make sure we have something in the training matrix
            if (trainingSpikes.eventBuffer.Count == 0)
            {
                throw new InvalidOperationException("The training data set was empty");
            }

            for (int i = 0; i < numberChannels; ++i)
            {
                // Current channel
                int currentChannel = i;

                // Get the spikes that belong to this channel
                List<SpikeEvent> spikesOnChan = trainingSpikes.eventBuffer.Where(x => x.channel == currentChannel).ToList();

                // Project channel data
                if (spikesOnChan.Count >= minSpikes)
                {

                    // Train a channel model for this channel
                    ChannelModel thisChannelModel = new ChannelModel(currentChannel, maxK, totalNumberOfUnits, numSTD);

                    // Project Data
                    thisChannelModel.MaxInflectProject(spikesOnChan.ToList(), inflectionSample);

                    // Train Classifier
                    thisChannelModel.Train();

                    // If there was a training failure (e.g. convergence)
                    if (!thisChannelModel.trained)
                        continue;

                    // Note that we have to sort spikes on this channel
                    channelsToSort.Add(currentChannel);

                    // Update the unit dicationary and increment the total number of units
                    for (int k = 1; k <= thisChannelModel.K; ++k)
                    {
                        unitDictionary.Add(totalNumberOfUnits + k, k);
                    }

                    totalNumberOfUnits += thisChannelModel.K;

                    // Add the channel model to the list
                    channelModels.Add(thisChannelModel);
                }
            }

            // All finished
            trained = true;
        }

        /// <summary>
        /// Trains a classifier for each channel so long as (int)minSpikes worth of spikes have been collected
        /// for that channel in the training buffer
        /// </summary>
        public void Train()
        {
            // Clear old channel models
            channelsToSort = new List<int>();
            channelModels.Clear();
            trained = false;
            totalNumberOfUnits = 0;

            // Clear old unit dictionary
            unitDictionary = new Hashtable();

            // Add the zero unit to the dictionary
            unitDictionary.Add(0, 0);

            // Make sure we have something in the training matrix
            if (trainingSpikes.eventBuffer.Count == 0)
            {
                throw new InvalidOperationException("The training data set was empty");
            }

            for (int i = 0; i < numberChannels; ++i)
            {
                // Current channel
                int currentChannel = i;

                // Get the spikes that belong to this channel
                List<SpikeEvent> spikesOnChan = trainingSpikes.eventBuffer.Where(x => x.channel == currentChannel).ToList();

                // Project channel data
                if (spikesOnChan.Count >= minSpikes)
                {
                    // Train a channel model for this channel
                    ChannelModel thisChannelModel = new ChannelModel(currentChannel, maxK, totalNumberOfUnits, numSTD, projectionDimension);

                    // Project Data
                    if (projectionType == "PCA")
                        thisChannelModel.PCCompute(spikesOnChan.ToList());
                    else
                        thisChannelModel.HaarCompute(spikesOnChan.ToList());

                    // Train Classifier
                    thisChannelModel.Train();

                    // If there was a training failure (e.g. convergence)
                    if (!thisChannelModel.trained)
                        continue;

                    // Note that we have to sort spikes on this channel
                    channelsToSort.Add(currentChannel);

                    // Update the unit dicationary and increment the total number of units
                    for (int k = 1; k <= thisChannelModel.K; ++k)
                    {
                        unitDictionary.Add(totalNumberOfUnits + k, k);
                    }

                    totalNumberOfUnits += thisChannelModel.K;

                    // Add the channel model to the list
                    channelModels.Add(thisChannelModel);
                }
            }

            // All finished
            trained = true;
        }

        /// <summary>
        /// After the channel models (gmm's) have been created and trained, this function
        /// is used to classifty newly detected spikes for which a valide channel model exisits.
        /// </summary>
        /// <param name="newSpikes"> An EventBuffer conataining spikes to be classified</param>
        public void Classify(ref EventBuffer<SpikeEvent> newSpikes)
        {

            // Make sure the channel models are trained.
            if (!trained)
            {
                throw new InvalidOperationException("The channel models were not yet trained so classification is not possible.");
            }

            // Sort the channels that need sorting
            for (int i = 0; i < channelsToSort.Count; ++i)
            {
                // Current channel
                int currentChannel = channelsToSort[i];

                // Get the spikes that belong to this channel 
                List<SpikeEvent> spikesOnChan = newSpikes.eventBuffer.Where(x => x.channel == currentChannel).ToList();

                // If there are no spikes on this channel
                if (spikesOnChan.Count == 0)
                    continue;

                // Get the channel model for this channel
                ChannelModel thisChannelModel = channelModels[channelsToSort.IndexOf(currentChannel)];

                // Project the spikes
                if (this.projectionType == "Maximum Voltage Inflection")
                    thisChannelModel.MaxInflectProject(spikesOnChan.ToList(), inflectionSample);
                else if (this.projectionType == "PCA")
                    thisChannelModel.PCProject(spikesOnChan.ToList());
                else if (this.projectionType == "Haar Wavelet")
                    thisChannelModel.HaarProject(spikesOnChan.ToList());

                // Sort the spikes
                thisChannelModel.ClassifyThresh();

                // Update the newSpikes buffer 
                for (int j = 0; j < spikesOnChan.Count; ++j)
                {
                    if (thisChannelModel.classes[j] < 0)
                        spikesOnChan[j].SetUnit((Int16)0);
                    else
                        spikesOnChan[j].SetUnit((Int16)(thisChannelModel.classes[j] + thisChannelModel.unitStartIndex + 1));
                }

            }

        }

        /// <summary>
        /// After the channel models (gmm's) have been created and trained, this function
        /// is used to classifty newly detected spikes for which a valide channel model exisits.
        /// </summary>
        /// <param name="newSpikes"> An EventBuffer conataining spikes to be classified</param>
        public void Classify(ref EventBuffer<SpikeEvent> newSpikes, ref List<double[]> projections2D)
        {

            // Make sure the channel models are trained.
            if (!trained)
            {
                throw new InvalidOperationException("The channel models were not yet trained so classification is not possible.");
            }

            // Sort the channels that need sorting
            for (int i = 0; i < channelsToSort.Count; ++i)
            {
                // Current channel
                int currentChannel = channelsToSort[i];

                // Get the spikes that belong to this channel 
                List<SpikeEvent> spikesOnChan = newSpikes.eventBuffer.Where(x => x.channel == currentChannel).ToList();

                // If there are no spikes on this channel
                if (spikesOnChan.Count == 0)
                    continue;

                // Get the channel model for this channel
                ChannelModel thisChannelModel = channelModels[channelsToSort.IndexOf(currentChannel)];

                // Project the spikes
                if (this.projectionType == "Maximum Voltage Inflection")
                    thisChannelModel.MaxInflectProject(spikesOnChan.ToList(), inflectionSample);
                else if (this.projectionType == "PCA")
                    thisChannelModel.PCProject(spikesOnChan.ToList());
                else if (this.projectionType == "Haar Wavelet")
                    thisChannelModel.HaarProject(spikesOnChan.ToList());

                // Get the projection
                projections2D = thisChannelModel.Return2DProjection();

                // Sort the spikes
                thisChannelModel.ClassifyThresh();

                // Update the newSpikes buffer 
                for (int j = 0; j < spikesOnChan.Count; ++j)
                {
                    if (thisChannelModel.classes[j] < 0)
                        spikesOnChan[j].SetUnit((Int16)0);
                    else
                        spikesOnChan[j].SetUnit((Int16)(thisChannelModel.classes[j] + thisChannelModel.unitStartIndex + 1));
                }

            }

        }

        #region Serialization Constructors/Deconstructors
        public SpikeSorter(SerializationInfo info, StreamingContext ctxt)
        {
            this.trained = (bool)info.GetValue("trained", typeof(bool));
            this.numberChannels = (int)info.GetValue("numberChannels", typeof(int));
            this.minSpikes = (int)info.GetValue("minSpikes", typeof(int));
            this.maxK = (int)info.GetValue("maxK", typeof(int));
            this.inflectionSample = (int)info.GetValue("inflectionSample", typeof(int));
            this.trainingSpikes = (EventBuffer<SpikeEvent>)info.GetValue("trainingSpikes", typeof(EventBuffer<SpikeEvent>));
            this.channelsToSort = (List<int>)info.GetValue("channelsToSort", typeof(List<int>));
            this.channelModels = (List<ChannelModel>)info.GetValue("channelModels", typeof(List<ChannelModel>));
            this.unitDictionary = (Hashtable)info.GetValue("unitDictionary", typeof(Hashtable));
            this.projectionType = (string)info.GetValue("projectionType", typeof(string));
            this.projectionDimension = (int)info.GetValue("projectionDimension", typeof(int));
            this.maxTrainingSpikesPerChannel = (int)info.GetValue("maxTrainingSpikesPerChannel", typeof(int));
            this.spikesCollectedPerChannel = (Hashtable)info.GetValue("spikesCollectedPerChannel", typeof(Hashtable));
            this.totalNumberOfUnits = (int)info.GetValue("totalNumberOfUnits", typeof(int));
            this.numSTD = (double)info.GetValue("numSTD", typeof(double));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("trained", this.trained);
            info.AddValue("numberChannels", this.numberChannels);
            info.AddValue("minSpikes", this.minSpikes);
            info.AddValue("maxK", this.maxK);
            info.AddValue("inflectionSample", this.inflectionSample);
            info.AddValue("trainingSpikes", this.trainingSpikes);
            info.AddValue("channelsToSort", this.channelsToSort);
            info.AddValue("channelModels", this.channelModels);
            info.AddValue("unitDictionary", this.unitDictionary);
            info.AddValue("projectionType", this.projectionType);
            info.AddValue("projectionDimension", this.projectionDimension);
            info.AddValue("maxTrainingSpikesPerChannel", this.maxTrainingSpikesPerChannel);
            info.AddValue("spikesCollectedPerChannel", this.spikesCollectedPerChannel);
            info.AddValue("totalNumberOfUnits", this.totalNumberOfUnits);
            info.AddValue("numSTD", this.numSTD);
        }

        #endregion

    }
}
