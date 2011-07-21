using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.StimSrv;
using simoc.srv;
using simoc.UI;

namespace simoc.filt2out
{
    /// <summary>
    /// Base clase turning an error signal into a feedback signal for control. Jon Newman.
    /// <author> Jon Newman</author>
    /// </summary>
    internal abstract class Filt2Out
    {
       
        protected NRStimSrv stimSrv;
        protected double currentError;
        protected ulong loadOffset;
        protected double c0;
        protected double c1;
        protected double c2;
        protected double hardwareSampFreqHz;

        public Filt2Out(ref NRStimSrv stimSrv, ControlPanel cp)
        {
            this.stimSrv = stimSrv;
            this.loadOffset = (ulong)stimSrv.GetBuffSize()*2;
            this.c0 = cp.numericEdit_ContC0.Value;
            this.c1 = cp.numericEdit_ContC1.Value;
            this.c2 = cp.numericEdit_ContC2.Value;
            this.hardwareSampFreqHz = stimSrv.sampleFrequencyHz;

        }

        internal virtual void SendFeedBack()
        {

        }
        internal virtual void SendFeedBack(int chanNo)
        {
 
        }
        internal virtual void SendFeedBack(int[] chanNos)
        {

        }
        internal virtual void SendFeedBack(int chanNoDig, int chanNoAux)
        {

        }
        internal virtual void SendFeedBack(int[] chanNosDig, int[] chanNosAux)
        {

        }

        protected double GetTauSec(double tauMultiplesOfBufferPeriod)
        {
            return tauMultiplesOfBufferPeriod * ((double)stimSrv.GetBuffSize() / (double)stimSrv.sampleFrequencyHz);
        }

        protected double GetTauPeriods(double tauSec)
        {
            return tauSec * ((double)stimSrv.sampleFrequencyHz/(double)stimSrv.GetBuffSize());
        }

        protected internal void CalculateError(SIMOCRawSrv filtSrv)
        {
            // Get the error signal
            ulong[] filterSrvTR = filtSrv.EstimateAvailableTimeRange();
            RawSimocBuffer currentFiltSample = filtSrv.ReadFromBuffer(filterSrvTR[1], filterSrvTR[1]);
            currentError = currentFiltSample.rawMultiChannelBuffer[2][0];
        }

        protected void SendEStimOutput(List<StimulusOutEvent> stimOutBuffer)
        {
            if (stimOutBuffer.Count > 0)
                stimSrv.StimOut.WriteToBuffer(stimOutBuffer);
        }

        protected void SendAuxAnalogOuput(List<AuxOutEvent> auxOutBuffer)
        {
            if (auxOutBuffer.Count > 0)
                stimSrv.AuxOut.WriteToBuffer(auxOutBuffer);
        }

        protected void SendAuxDigitalOuput(List<DigitalOutEvent> digOutBuffer)
        {
            if (digOutBuffer.Count > 0)
                stimSrv.DigitalOut.WriteToBuffer(digOutBuffer);
        }





    }
}
