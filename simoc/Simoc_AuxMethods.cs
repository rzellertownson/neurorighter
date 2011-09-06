using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NeuroRighter.Output;
using NeuroRighter.DataTypes;
using NeuroRighter.DatSrv;
using System.Threading;
using NationalInstruments.DAQmx;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.UI;
using simoc.plotting;
using simoc.UI;
using simoc.spk2obs;
using simoc.targetfunc;
using simoc.srv;
using simoc.obs2filt;
using simoc.filt2out;
using simoc.extensionmethods;

namespace simoc
{
    /// <summary>
    /// Methods used by Spike-input, multiple-output controller (SIMOC). Written by Jon Newman, Georgia Tech.
    /// </summary>
    public partial class Simoc : ClosedLoopExperiment
    {
        // current data points

        private void UpdateClock()
        {
            currentTime = StimSrv.DigitalOut.GetTime() / 1000;
        }

        private void MakeObservation()
        {
            // reset the current observation
            currentObs = 0;

            try
            {
                switch (controlPanel.obsAlg)
                {
                    case "ASDR":
                        {
                            Spk2ASDR observer = new Spk2ASDR(StimSrv,DatSrv);
                            numberOfObs = observer.numberOfObs;
                            observer.GetNewSpikes(DatSrv,simocVariableStorage);
                            observer.MeasureObservable();
                            observer.PopulateObsSrv(ref obsSrv);
                            currentObs = observer.currentObservation;
                        }
                        break;
                    case "CSDR":
                        {
                            Spk2CSDR observer = new Spk2CSDR(StimSrv,DatSrv);
                            numberOfObs = observer.numberOfObs;
                            observer.GetNewSpikes(DatSrv, simocVariableStorage);
                            observer.MeasureObservable();
                            observer.PopulateObsSrv(ref obsSrv);
                            currentObs = observer.currentObservation;
                        }
                        break;
                }
            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at MakeObservation(): \r\r" + sEx.Message);
            }


        }

        private void GetTargetValue()
        {
            try
            {
                switch (controlPanel.targetFunc)
                {
                    case "Constant":
                        {
                            Constant target = new Constant(controlPanel, DACPollingPeriodSec, numTargetSamplesGenerated, ref StimSrv);
                            target.GetTargetValue(ref currentTarget, simocVariableStorage);
                        }
                        break;
                    case "Sine Wave":
                        {
                            SineWave target = new SineWave(controlPanel, DACPollingPeriodSec, numTargetSamplesGenerated, ref StimSrv);
                            target.GetTargetValue(ref currentTarget, simocVariableStorage);
                        }
                        break;
                    case "Custom 1":
                        {
                            CustomTarget1 target = new CustomTarget1(controlPanel, DACPollingPeriodSec, numTargetSamplesGenerated,ref StimSrv);
                            target.GetTargetValue(ref currentTarget,simocVariableStorage);
                        }
                        break;
                }
                numTargetSamplesGenerated++;
            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at GetTargetValue(): \r\r" + sEx.Message);
            }

        }

        private void FilterObservation(bool firstLoop)
        {

            try
            {
                switch (controlPanel.filtAlg)
                {
                    case "None":
                        {
                            Obs2Obs filter = new Obs2Obs(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                        }
                        break;
                    case "Moving Average":
                        {
                            Obs2MA filter = new Obs2MA(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                        }
                        break;
                    case "Moving Median":
                        {
                            Obs2MM filter = new Obs2MM(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                        }
                        break;
                    case "Exponential Moving Average":
                        {
                            Obs2EMA filter = new Obs2EMA(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                        }
                        break;
                }
            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at FilterObservation(): \r\r" + sEx.Message);
            }
        }

        private void CreateFeedback()
        {
            // reset the currenFeedback array
            currentFeedBack = new double[10];

            try
            {
                switch (controlPanel.contAlg)
                {
                    case "Filt2PropFreqFB":
                        {
                            Filt2PropFreqFB controller = new Filt2PropFreqFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                        }
                        break;
                    case "Filt2PropPowerFB":
                        {
                            Filt2PropPowerFB controller = new Filt2PropPowerFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                        }
                        break;
                }


            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at CreateFeedback(): \r\r" + sEx.Message);
            }
        }

        private void Write2File()
        {
            if (NRRecording)
            {
                // Make the datum array
                double[] datum = new double[simocOut.numStreams];

                datum[0] = currentObs;
                datum[1] = currentFilt;
                datum[2] = currentTarget;
                datum[3] = currentError;
                for (int i = 4; i < currentFeedBack.Length + 4; ++i)
                {
                    datum[i] = currentFeedBack[i - 4];
                }

                // Write data to file
                simocOut.WriteData(currentTime, datum);

            }
        }

        private void UpdateGUI()
        {
            try
            {
                // Update graph controls
                controlPanel.UpdateGraphSliders();
                controlPanel.UpdateGraphs(obsSrv, filtSrv);

            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at UpdateGUI(): \r\r" + sEx.Message);
            }

        }

        private void StartControlPanel()
        {
            // Start the control panel on its own thread
            new Thread(
                new ThreadStart(
                    (System.Action)delegate
                    {
                        Application.Run(controlPanel = new ControlPanel());
                    }
                   )
            ).Start();
        }

    }
}
