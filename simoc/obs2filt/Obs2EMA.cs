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
    class Obs2EMA : Obs2Filt
    {
        /// <summary>
        /// Exponential Moving average filter of observation stream. Jon Newman.
        /// </summary>
        public Obs2EMA(ControlPanel cp, NRDataSrv DatSrv, bool firstLoop)
            : base(cp, DatSrv, firstLoop)
        {

        }

        internal override void Filter()
        {
            if (!firstFilt)
            {
                currentFilteredValue = c0 * obsFiltBuff.rawMultiChannelBuffer[0].Average() + (1 - c0) * currentFilteredValue;
            }
            else
            {
                currentFilteredValue = obsFiltBuff.rawMultiChannelBuffer[0].Average();
            }
        }
    }
}