using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.srv;
using simoc.UI;
using NeuroRighter.DatSrv;
using simoc.extensionmethods;
using simoc.persistantstate;
using NeuroRighter.StimSrv;

namespace simoc.obs2filt
{
    class Obs2EMA : Obs2Filt
    {
        /// <summary>
        /// Exponential Moving average filter of observation stream. Jon Newman.
        /// </summary>
        public Obs2EMA(ControlPanel cp, NRStimSrv StimSrv, bool firstLoop)
            : base(cp, StimSrv, firstLoop)
        {

        }

        internal override void Filter(PersistentSimocVar persistantState)
        {
            if (!firstFilt)
            {
                double a = 2.0 / (double)(filterWidth + 1);
                currentFilteredValue = a * obsFiltBuff.rawMultiChannelBuffer[0][0] + (1 - a) * persistantState.LastFilteredObs;
            }
            else
            {
                currentFilteredValue = obsFiltBuff.rawMultiChannelBuffer[0][0];
            }
        }
    }
}