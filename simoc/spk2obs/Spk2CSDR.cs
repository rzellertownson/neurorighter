using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.DatSrv;
using NeuroRighter.StimSrv;

namespace simoc.spk2obs
{
    /// <summary>
    /// Estimate the CSDR from incoming an spike buffer
    /// </summary>
    class Spk2CSDR : Spk2Obs
    {

        public Spk2CSDR(NRStimSrv stimSrv, NRDataSrv datSrv) 
            : base(stimSrv,datSrv)
        {
            numberOfObs = 1;
        }

        internal override void MeasureObservable()
        {
            // Estimate the ASDR
            currentObservation = (((double)newSpikes.Buffer.Count()) / dacPollingPeriodSec / (double)channelCount);
        }


    }
}
