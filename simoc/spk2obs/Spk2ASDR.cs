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
    /// Estimate the ASDR from incoming an spike buffer
    /// </summary>
    class Spk2ASDR : Spk2Obs
    {

        public Spk2ASDR(NRDataSrv DatSrv)
            : base(DatSrv)
        {
            numberOfObs = 1;
        }

        internal override void MeasureObservable()
        {
            // Estimate the ASDR
            currentObservation = (((double)newSpikes.eventBuffer.Count()-1) / daqPollingPeriodSec);
        }


    }
}
