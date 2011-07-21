using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    /// Spike-input, multiple-output controller (SIMOC). Written by Jon Newman, Georgia Tech.
    /// </summary>
    public class simoc : ClosedLoopExperiment
    {
        // The GUI
        private ControlPanel controlPanel;
        private delegate void getControlPanelValue();

        // Intially we are not done with our first run method and the actual CL protocol is not running
        private bool finishedWithRun = false;
        private bool simocStarted = false;

        // Number of observables
        private int numberOfObs = 1;

        // Current target value
        private double currentTarget;

        // Make a raw data server for CSDR estimate and the desired ASDR
        SIMOCRawSrv obsSrv;
        SIMOCRawSrv filtSrv;

        protected override void Run()
        {

            if (!finishedWithRun)
            {
                // Run the control panel on its own thread
                StartControlPanel();

                // Let it set up
                System.Threading.Thread.Sleep(2000);

                // Set up servers
                obsSrv = new SIMOCRawSrv
                    (1 / DatSrv.ADCPollingPeriodSec, numberOfObs, controlPanel.numericEdit_ObsBuffHistorySec.Value, 1, 1);
                filtSrv = new SIMOCRawSrv
                    (1 / DatSrv.ADCPollingPeriodSec, 3 * numberOfObs, controlPanel.numericEdit_ObsBuffHistorySec.Value, 1, 1);

                // Set up closed loop algorithm
                double startTime = StimSrv.DigitalOut.GetTime();
                Console.WriteLine("SIMOC starting out at time " + startTime.ToString());

                // Tell buffer loader that we are done setting up
                finishedWithRun = true;
            }

            // Infinite loop until stop is pressed or something explodes
            while (Running && !controlPanel.stopButtonPressed)
            {
                System.Threading.Thread.Sleep(1000);
                simocStarted = controlPanel.startButtonPressed;
            }

            // Set up closed loop algorithm
            double stopTime = StimSrv.DigitalOut.GetTime();
            Console.WriteLine("SIMOC stopped out at time " + stopTime.ToString());

            // Release resources
            //Dispose();
            Running = false;
        }

        protected override void BuffLoadEvent(object sender, EventArgs e)
        {
            if (Running && finishedWithRun && simocStarted)
            {
                try
                {
                    // Inoke thread-safe access to form properties
                    controlPanel.UpdateProperties();

                    // First, we grab the new spike data and estimate the chosen observable
                    MakeObservation();

                    // Next, we get the target value
                    GetTargetValue();

                    // Next, we filter the data
                    FilterObservation();

                    // Next, we make the feedback signal
                    CreateFeedback();

                    // Finally, we update the GUI
                    UpdateGUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("In BuffLoadEvent: \r\r" + ex.Message);
                }

            }
        }

        private void MakeObservation()
        {
            try
            {
                switch (controlPanel.obsAlg)
                {
                    case "ASDR":
                        {
                            Spk2ASDR observer = new Spk2ASDR(DatSrv);
                            numberOfObs = observer.numberOfObs;
                            observer.GetNewSpikes(DatSrv);
                            observer.MeasureObservable();
                            observer.PopulateObsSrv(ref obsSrv);
                        }
                        break;
                    case "CSDR":
                        {
                            Spk2CSDR observer = new Spk2CSDR(DatSrv);
                            numberOfObs = observer.numberOfObs;
                            observer.GetNewSpikes(DatSrv);
                            observer.MeasureObservable();
                            observer.PopulateObsSrv(ref obsSrv);
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
                            Constant target = new Constant(controlPanel);
                            target.GetTargetValue(ref currentTarget);
                        }
                        break;
                }
            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at GetTargetValue(): \r\r" + sEx.Message);
            }

        }

        private void FilterObservation()
        {
            try
            {
                switch (controlPanel.filtAlg)
                {
                    case "None":
                        {
                            Obs2Obs filter = new Obs2Obs(controlPanel,DatSrv);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                        }
                        break;
                    case "Moving Average":
                        {
                            Obs2MA filter = new Obs2MA(controlPanel,DatSrv);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
                        }
                        break;
                    case "Moving Median":
                        {
                            Obs2MM filter = new Obs2MM(controlPanel,DatSrv);
                            filter.GetObsBuffer(obsSrv);
                            filter.Filter();
                            filter.PopulateFiltSrv(ref filtSrv, currentTarget);
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
            try
            {
                switch (controlPanel.contAlg)
                {
                    case "Filt2P0_Prop_FreqOf1MSecPulse":
                        {
                            Filt2P0_Prop_FreqOf1MSecPulse controller = new Filt2P0_Prop_FreqOf1MSecPulse(ref StimSrv, controlPanel);
                            controller.CalculateError(filtSrv);
                            controller.SendFeedBack();
                        }
                        break;
                    case "Filt2P0and1_Prop_FreqOf1MSecPulse":
                        {
                            Filt2P0and1_Prop_FreqOf1MSecPulse controller = new Filt2P0and1_Prop_FreqOf1MSecPulse(ref StimSrv, controlPanel);
                            controller.CalculateError(filtSrv);
                            controller.SendFeedBack();
                        }
                        break;
                    case "Filt2A0and1_Prop_AmpOf_C0MSec_Pulses_At_C1Hz":
                        {
                            Filt2A0and1_Prop_AmpOf_C0MSec_Pulses_At_C1Hz controller = new Filt2A0and1_Prop_AmpOf_C0MSec_Pulses_At_C1Hz(ref StimSrv, controlPanel);
                            controller.CalculateError(filtSrv);
                            controller.SendFeedBack();
                        }
                        break;
                        
                }
            }
            catch (Exception sEx)
            {
                MessageBox.Show("SIMOC Failed at CreateFeedback(): \r\r" + sEx.Message);
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

        //private void Dispose()
        //{
        //    controlPanel.
        //}

    }
}