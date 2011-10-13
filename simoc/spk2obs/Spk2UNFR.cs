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
    class Spk2UNFR: Spk2Obs
    {
        // Number of detected units
        private int numberOfDetectedUnits;

        public Spk2UNFR(NRStimSrv stimSrv, NRDataSrv datSrv)
            : base(stimSrv,datSrv)
        {
            numberOfObs = 1;
        }

        internal void SetNumberOfUnits(int numUnits)
        {
            numberOfDetectedUnits = numUnits;
        }

        internal override void MeasureObservable()
        {
            // Estimate the CSDR
            if (numberOfDetectedUnits > 0)
                currentObservation = (double)newSpikes.eventBuffer.Where(x => x.unit != 0).Count() / (double)numberOfDetectedUnits; 
            else
                currentObservation = 0;
        }
    }
}
