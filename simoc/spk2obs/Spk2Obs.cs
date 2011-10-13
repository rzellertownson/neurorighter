using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using NeuroRighter.StimSrv;
using simoc.srv;
using simoc.persistantstate;

namespace simoc.spk2obs
{
    /// <summary>
    /// Base clase for creating an obervation stream from the spike input stream.
    /// <author> Jon Newman.</author>
    /// </summary>
    internal abstract class Spk2Obs
    {
        protected EventBuffer<SpikeEvent> newSpikes;
        protected int dacPollingPeriodSamples;
        protected double dacPollingPeriodSec;
        protected ulong adcSamplesInDacPoll;
        protected int channelCount;
        internal double currentObservation;
        internal int numberOfObs;
        protected double numSecondInCurrentRead;


        public Spk2Obs(NRStimSrv stimSrv,NRDataSrv datSrv)
        {
            this.dacPollingPeriodSec = stimSrv.DACPollingPeriodSec;
            this.channelCount = datSrv.rawElectrodeSrv.channelCount;
            this.dacPollingPeriodSamples = stimSrv.DACPollingPeriodSamples;
            this.adcSamplesInDacPoll = (ulong)Math.Round(datSrv.spikeSrv.sampleFrequencyHz * dacPollingPeriodSec);
            this.newSpikes = new EventBuffer<SpikeEvent>(datSrv.spikeSrv.sampleFrequencyHz);
            this.numSecondInCurrentRead = adcSamplesInDacPoll;
        }

        /// <summary>
        /// Pulls latest spike data from the buffer and populates the newSpikes private buffer
        /// </summary>
        internal void GetNewSpikes(NRDataSrv DatSrv, PersistentSimocVar simocPersistentState)
        {
            // First, figure out what history of spikes we have
            ulong[] spikeTimeRange = DatSrv.spikeSrv.EstimateAvailableTimeRange();

            // Do is there any new data yet?
            if (spikeTimeRange[1] > simocPersistentState.LastSampleRead)
            {
                // Try to get the number of spikes within the available time range
                newSpikes = DatSrv.spikeSrv.ReadFromBuffer(simocPersistentState.LastSampleRead, spikeTimeRange[1]);

                // How many seconds is this data taken from
                numSecondInCurrentRead = (double)(spikeTimeRange[1] - simocPersistentState.LastSampleRead) / DatSrv.spikeSrv.sampleFrequencyHz;

                // Update the last sample read
                simocPersistentState.LastSampleRead = spikeTimeRange[1];
                //for (int i = 0; i < newSpikes.eventBuffer.Count(); ++i)
                //{
                //    if (newSpikes.eventBuffer[i].sampleIndex >
                //    simocPersistentState.LastSampleRead = newSpikes.eventBuffer.;
            }

        }

        internal virtual void MeasureObservable()
        {
            // Use this to set which types of outputs are going to be sent
        }

        internal void PopulateObsSrv(ref SIMOCRawSrv obsSrv)
        {
            double[,] datum = new double[1,1];
            datum[0,0] = currentObservation;
            obsSrv.WriteToBuffer(datum);
        }

    }
}
