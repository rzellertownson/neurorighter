using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using simoc.srv;
using simoc.UI;
using NeuroRighter.DatSrv;
using simoc.extensionmethods;

namespace simoc.obs2filt
{
    class Obs2Obs : Obs2Filt
    {
        /// <summary>
        /// No filtering of observation stream. Jon Newman.
        /// </summary>
        public Obs2Obs(ControlPanel cp, NRDataSrv DatSrv)
            : base(cp, DatSrv)
        {

        }

        internal override void Filter()
        {
            currentFilteredValue = obsFiltBuff.rawMultiChannelBuffer[0][0];
        }
    }
}
