using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.srv;
using simoc.UI;
using NeuroRighter.DatSrv;
using NeuroRighter.StimSrv;

namespace simoc.obs2filt
{
    /// <summary>
    /// Moving median filter of observation stream. Jon Newman.
    /// </summary>
    class Obs2MM : Obs2Filt
    {
        public Obs2MM(ControlPanel cp, NRStimSrv StimSrv,  bool firstLoop)
            : base(cp, StimSrv, firstLoop)
        {

        }

        internal override void Filter()
        {
            // Find the median of the last filterWidth observations
            var orderedObs = from element in obsFiltBuff.rawMultiChannelBuffer[0]
                             orderby element ascending
                             select element;
            currentFilteredValue = orderedObs.ElementAt(obsFiltBuff.rawMultiChannelBuffer[0].Length / 2 - 1);

        }

    }
}
