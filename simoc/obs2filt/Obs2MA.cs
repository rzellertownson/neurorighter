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
    class Obs2MA: Obs2Filt
    {
        /// <summary>
        /// Moving average filter of observation stream. Jon Newman.
        /// </summary>
        public Obs2MA(ControlPanel cp, NRDataSrv DatSrv, bool firstLoop)
            : base(cp, DatSrv, firstLoop)
        {

        }

        internal override void Filter()
        {
            currentFilteredValue = obsFiltBuff.rawMultiChannelBuffer[0].Average();
        }
    }
}
