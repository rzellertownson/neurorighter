using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.StimSrv;
using simoc.srv;
using simoc.UI;
using simoc.persistantstate;

namespace simoc.filt2out
{
    /// <summary>
    /// Base clase turning an error signal into a feedback signal for control. Jon Newman.
    /// <author> Jon Newman</author>
    /// </summary>
    internal abstract class Filt2Out
    {
       
        protected NRStimSrv stimSrv;
        protected double currentErrorInt;
        protected ulong loadOffset;
        protected double c0;
        protected double c1;
        protected double c2;
        protected double c3;
        protected double c4;
        protected double c5;
        protected double hardwareSampFreqHz;
        internal int numberOutStreams = 1;
        internal double[] currentFeedbackSignals;
        protected ulong nextAvailableSample;

        public Filt2Out(ref NRStimSrv stimSrv, ControlPanel cp)
        {
            this.stimSrv = stimSrv;
            this.loadOffset = 0;//(ulong)stimSrv.GetBuffSize()*2;
            this.c0 = (double)cp.numericUpDown_ContC0.Value;
            this.c1 = (double)cp.numericUpDown_ContC1.Value;
            this.c2 = (double)cp.numericUpDown_ContC2.Value;
            this.c3 = (double)cp.numericUpDown_ContC3.Value;
            this.c4 = (double)cp.numericUpDown_ContC4.Value;
            this.c5 = (double)cp.numericUpDown_ContC5.Value;
            this.hardwareSampFreqHz = stimSrv.sampleFrequencyHz;
        }

        internal virtual void SendFeedBack(PersistentSimocVar simocVariableStorage)
        {
            // What buffer load are we currently processing?
            ulong currentLoad = stimSrv.AuxOut.GetNumberBuffLoadsCompleted() + 1;
            nextAvailableSample = currentLoad * (ulong)stimSrv.GetBuffSize();

        }

        internal virtual void CalculateError(ref double currentError, double currentTarget, double currentFilt){}

        protected double GetTauSec(double tauMultiplesOfBufferPeriod)
        {
            return tauMultiplesOfBufferPeriod * ((double)stimSrv.GetBuffSize() / (double)stimSrv.sampleFrequencyHz);
        }

        protected double GetTauPeriods(double tauSec)
        {
            return tauSec * ((double)stimSrv.sampleFrequencyHz/(double)stimSrv.GetBuffSize());
        }

        protected void SendEStimOutput(List<StimulusOutEvent> stimOutBuffer)
        {
            if (stimOutBuffer.Count > 0)
                stimSrv.StimOut.WriteToBuffer(stimOutBuffer);
        }

        protected void SendAuxAnalogOutput(List<AuxOutEvent> auxOutBuffer)
        {
            if (auxOutBuffer.Count > 0)
                stimSrv.AuxOut.WriteToBuffer(auxOutBuffer);
        }

        protected void SendAuxDigitalOutput(List<DigitalOutEvent> digOutBuffer)
        {
            if (digOutBuffer.Count > 0)
                stimSrv.DigitalOut.WriteToBuffer(digOutBuffer);
        }
    }
}
