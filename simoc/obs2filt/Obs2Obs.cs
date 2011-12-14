using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.srv;
using simoc.UI;
using NeuroRighter.DatSrv;
using simoc.extensionmethods;
using NeuroRighter.StimSrv;

namespace simoc.obs2filt
{
    class Obs2Obs : Obs2Filt
    {
        /// <summary>
        /// No filtering of observation stream. Jon Newman.
        /// </summary>
        public Obs2Obs(ControlPanel cp, NRStimSrv StimSrv, bool firstLoop)
            : base(cp, StimSrv,firstLoop)
        {

        }

        internal override void Filter()
        {
            currentFilteredValue = obsFiltBuff.rawMultiChannelBuffer[0][obsFiltBuff.rawMultiChannelBuffer[0].Length-1];
        }
    }
}
