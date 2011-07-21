using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;

namespace simoc.spk2obs
{
    /// <summary>
    /// Estimate the CSDR from incoming an spike buffer
    /// </summary>
    class Spk2CSDR : Spk2Obs
    {

        public Spk2CSDR(NRDataSrv DatSrv) 
            : base(DatSrv)
        {
            numberOfObs = 1;
        }

        internal override void MeasureObservable()
        {
            // Estimate the ASDR
            currentObservation = ((double)newSpikes.eventBuffer.Count() / daqPollingPeriodSec / (double)channelCount);
        }


    }
}
