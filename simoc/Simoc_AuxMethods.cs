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

        private void UpdateClock()
        {
            currentTime = StimSrv.DigitalOut.GetTime() / 1000;
        }

        private void UpdatePersistantState()
        {
            simocVariableStorage.NumberOfLoopsCompleted++;
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
                    case "UNFR":
                        {
                            Spk2UNFR observer = new Spk2UNFR(StimSrv, DatSrv);
                            numberOfObs = observer.numberOfObs;
                            observer.SetNumberOfUnits((int)controlPanel.numericUpDown_NumUnits.Value);
                            observer.GetNewSpikes(DatSrv, simocVariableStorage);
                            observer.MeasureObservable();
                            observer.PopulateObsSrv(ref obsSrv);
                            currentObs = observer.currentObservation;
                        }
                        break;
                }

                // Update running average
                simocVariableStorage.UpdateRunningObsAverage(currentObs);

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
                    case "Custom 2":
                        {
                            CustomTarget2 target = new CustomTarget2(controlPanel, DACPollingPeriodSec, numTargetSamplesGenerated, ref StimSrv);
                            target.GetTargetValue(ref currentTarget, simocVariableStorage);
                        }
                        break;
                    case "5 Minute Steps":
                        {
                            FiveMinuteSteps target = new FiveMinuteSteps(controlPanel, DACPollingPeriodSec, numTargetSamplesGenerated, ref StimSrv);
                            target.GetTargetValue(ref currentTarget, simocVariableStorage);
                        }
                        break;
                    case "Multiple of Average Observable":
                        {
                            MulipleOfAverageObs target = new MulipleOfAverageObs(controlPanel, DACPollingPeriodSec, numTargetSamplesGenerated, ref StimSrv);
                            target.GetTargetValue(ref currentTarget, simocVariableStorage);
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
                            currentFilterType = 0;
                        }
                        break;
                    case "Moving Average":
                        {
                            Obs2MA filter = new Obs2MA(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                            currentFilterType = 1;
                        }
                        break;
                    case "Moving Median":
                        {
                            Obs2MM filter = new Obs2MM(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                            currentFilterType = 2;
                        }
                        break;
                    case "Exponential Moving Average":
                        {
                            Obs2EMA filter = new Obs2EMA(controlPanel, DatSrv, firstLoop);
                            filter.GetObsBufferSingleSample(obsSrv);
                            filter.Filter(simocVariableStorage);
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                            currentFilt = filter.currentFilteredValue;
                            currentFilterType = 3;
                        }
                        break;
                }

                // Store the current filtered value
                simocVariableStorage.LastFilteredObs = currentFilt;

            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at FilterObservation(): \r\r" + sEx.Message);
            }
        }

        private void CreateFeedback()
        {
            // reset the currenFeedback array
            currentFeedBack = new double[8];

            try
            {
                switch (controlPanel.contAlg)
                {
                    case "None":
                        {
                            Filt2None controller = new Filt2None(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 0;
                        }
                        break;
                    case "Filt2PropFreqFB":
                        {
                            Filt2PropFreqFB controller = new Filt2PropFreqFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 1;
                        }
                        break;
                    case "Filt2PropPowerFB":
                        {
                            Filt2PropPowerFB controller = new Filt2PropPowerFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 2;
                        }
                        break;
                    case "Filt2RelayIrradFB":
                        {
                            Filt2RelayIrradFB controller = new Filt2RelayIrradFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 3;
                            controlPanel.SetControllerResultText(simocVariableStorage.UltimatePeriodEstimate, simocVariableStorage.UltimateGainEstimate);
                            controlPanel.UpdateControllerTextbox();
                        }
                        break;
                    case "Filt2PIDIrradFB":
                        {
                            Filt2PIDIrradFB controller = new Filt2PIDIrradFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 4;
                        }
                        break;
                    case "Filt2RelayDutyCycleFB":
                        {
                            Filt2RelayDutyCycleFB controller = new Filt2RelayDutyCycleFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 5;
                            controlPanel.SetControllerResultText(simocVariableStorage.UltimatePeriodEstimate, simocVariableStorage.UltimateGainEstimate);
                            controlPanel.UpdateControllerTextbox();
                        }
                        break;
                    case "Filt2PIDutyCycleFB":
                        {
                            Filt2PIDutyCycleFB controller = new Filt2PIDutyCycleFB(ref StimSrv, controlPanel);
                            controller.CalculateError(ref currentError, currentTarget, currentFilt);
                            controller.SendFeedBack(simocVariableStorage);
                            for (int i = 0; i < controller.numberOutStreams; ++i)
                                currentFeedBack[i] = controller.currentFeedbackSignals[i];
                            currentControllerType = 6;
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
                datum[4] = currentFilterType;
                datum[5] = currentControllerType;

                for (int i = 6; i < currentFeedBack.Length + 6; ++i)
                {
                    datum[i] = currentFeedBack[i - 6];
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
                        Application.Run(controlPanel = new ControlPanel(StimSrv.DACPollingPeriodSec));
                    }
                   )
            ).Start();
        }

    }
}
