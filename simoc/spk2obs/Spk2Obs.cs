using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using simoc.srv;

namespace simoc.spk2obs
{
    /// <summary>
    /// Base clase for creating an obervation stream from the spike input stream.
    /// <author> Jon Newman.</author>
    /// </summary>
    internal abstract class Spk2Obs
    {
        protected EventBuffer<SpikeEvent> newSpikes;
        protected int daqPollingPeriodSamples;
        protected double daqPollingPeriodSec;
        protected int channelCount;
        internal double currentObservation;
        internal int numberOfObs;

        public Spk2Obs(NRDataSrv DatSrv)
        {
            this.daqPollingPeriodSamples = DatSrv.ADCPollingPeriodSamples;
            this.daqPollingPeriodSec = DatSrv.ADCPollingPeriodSec;
            this.channelCount = DatSrv.rawElectrodeSrv.channelCount;
        }

        /// <summary>
        /// Pulls latest spike data from the buffer and populates the newSpikes private buffer
        /// </summary>
        internal void GetNewSpikes(NRDataSrv DatSrv)
        {
            // First, figure out what history of spikes we have
            ulong[] spikeTimeRange = DatSrv.spikeSrv.EstimateAvailableTimeRange();

            // Try to get the number of spikes within the available time range
            ulong[] dataRange = new ulong[2] { spikeTimeRange[1] - (ulong)daqPollingPeriodSamples, spikeTimeRange[1] };
            newSpikes = DatSrv.spikeSrv.ReadFromBuffer(dataRange[0], dataRange[1]);
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
