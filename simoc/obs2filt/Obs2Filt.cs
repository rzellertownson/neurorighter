using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.UI;
using simoc.srv;
using NeuroRighter.DatSrv;
using simoc;
using simoc.persistantstate;

namespace simoc.obs2filt
{
    /// <summary>
    /// Base clase for creating an filter stream from the observation stream.
    /// <author> Jon Newman.</author>
    /// </summary>
    internal abstract class Obs2Filt
    {

        protected double filterWidthSec;
        protected int filterWidth;
        protected double c0;
        protected double c1;
        protected double c2;
        internal double currentFilteredValue = 0;
        protected double daqPollingPeriodSeconds;
        protected RawSimocBuffer obsFiltBuff;
        protected bool firstFilt;

        public Obs2Filt(ControlPanel cp, NRDataSrv DatSrv, bool firstLoop)
        {
            // Grab parameters off the form
            this.daqPollingPeriodSeconds = DatSrv.ADCPollingPeriodSec;
            this.c0 = cp.FilterC0;
            this.c1 = cp.FilterC1;
            this.c2 = cp.FilterC2;
            this.filterWidthSec = cp.FilterWidthSec;
            this.filterWidth = (int)(filterWidthSec/daqPollingPeriodSeconds);
            this.firstFilt = firstLoop;
        }

        internal virtual void Filter() { }

        internal virtual void Filter(PersistentSimocVar persistantState) {}

        protected internal void GetObsBuffer(SIMOCRawSrv obsSrv)
        {
            // Get the most current indicies in the obsSrv Buffer back to the filter width
            ulong[] currentInd = obsSrv.EstimateAvailableTimeRange();

            // Get a hunk of the buffer to filter
            if ((ulong)filterWidth > currentInd[1])
            {
                obsFiltBuff = obsSrv.ReadFromBuffer(0, currentInd[1]);
            }
            else
            {
                obsFiltBuff = obsSrv.ReadFromBuffer(currentInd[1] - (ulong)filterWidth, currentInd[1]);
            }
        }

        protected internal void GetObsBufferSingleSample(SIMOCRawSrv obsSrv)
        {
            // Get the most current indicies in the obsSrv Buffer back to the filter width
            ulong[] currentInd = obsSrv.EstimateAvailableTimeRange();

            // Get a single sample of the buffer to filter
            obsFiltBuff = obsSrv.ReadFromBuffer(currentInd[1], currentInd[1]);
        }

        internal void PopulateFiltSrv(ref SIMOCRawSrv filtSrv, double currentTargetValue)
        {
            double[,] datum = new double[3,1];
            datum[0, 0] = currentFilteredValue; // The filtered measurement
            datum[1, 0] = currentTargetValue; // The target
            datum[2, 0] = currentTargetValue - currentFilteredValue; // The error signal
            filtSrv.WriteToBuffer(datum);
        }



    }
}
