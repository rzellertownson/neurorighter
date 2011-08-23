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
using simoc.filewriting;
using simoc.persistantstate;

namespace simoc
{

    /// <summary>
    /// Spike-input, multiple-output controller (SIMOC). Written by Jon Newman, Georgia Tech.
    /// </summary>
    public partial class Simoc : ClosedLoopExperiment
    {
        // The GUI
        private ControlPanel controlPanel;
        private delegate void getControlPanelValue();
        
        // Intially we are not done with our first run method and the actual CL protocol is not running
        private bool finishedWithRun = false;
        private bool simocStarted = false;
        private bool firstLoop = true;
        private bool startTimeSet = false;

        // Number of observables
        private int numberOfObs = 1;

        // Current state variables
        private double currentTarget;
        private ulong numTargetSamplesGenerated = 0;
        private double startTime;
        private double currentTime;
        private double currentObs;
        private double currentFilt;
        private double currentError;
        private double[] currentFeedBack;

        // Variables Everyone needs
        private double DACPollingPeriodSec;
        private double ADCPollingPeriodSec;
        private PersistentSimocVar simocVariableStorage;

        // Make a raw data server for CSDR estimate and the desired ASDR
        private SIMOCRawSrv obsSrv;
        private SIMOCRawSrv filtSrv;

        // File writer
        private FileWriter simocOut;

        protected override void Run()
        {

            if (!finishedWithRun)
            {
                // Run the control panel on its own thread
                StartControlPanel();

                // Let it set up
                System.Threading.Thread.Sleep(2000);

                // Tell the assembly some parameters of the I/O 
                DACPollingPeriodSec = StimSrv.DACPollingPeriodSec;
                ADCPollingPeriodSec = DatSrv.ADCPollingPeriodSec;

                // Set up servers
                obsSrv = new SIMOCRawSrv
                    (1 / DatSrv.ADCPollingPeriodSec, numberOfObs, controlPanel.numericEdit_ObsBuffHistorySec.Value, 1, 1);
                filtSrv = new SIMOCRawSrv
                    (1 / DatSrv.ADCPollingPeriodSec, 3 * numberOfObs, controlPanel.numericEdit_ObsBuffHistorySec.Value, 1, 1);

                // Set up persistant internal varaible storage
                simocVariableStorage = new PersistentSimocVar();

                // Create file writer
                if (NRRecording)
                    simocOut = new FileWriter(NRFilePath + ".simoc",14,DatSrv.spikeSrv.sampleFrequencyHz);
                
                // Set up closed loop algorithm
                startTime = StimSrv.DigitalOut.GetTime()/1000;
                Console.WriteLine("SIMOC starting out at time " + startTime.ToString() + " seconds.");

                // Tell buffer loader that we are done setting up
                finishedWithRun = true;
            }

            // Infinite loop until stop is pressed or something explodes
            while (Running && !controlPanel.stopButtonPressed)
            {
                System.Threading.Thread.Sleep(1000);
                simocStarted = controlPanel.startButtonPressed;
                if (simocStarted && !startTimeSet)
                {
                    startTimeSet = true;
                    simocVariableStorage.SimocStartSample = StimSrv.DigitalOut.GetCurrentSample();
                }
            }

            // Close the file stream
            if (simocOut != null)
                simocOut.Close();

            // Set up closed loop algorithm
            double stopTime = StimSrv.DigitalOut.GetTime()/1000;
            Console.WriteLine("SIMOC stopped out at time " + stopTime.ToString() + " seconds.");


            // Release resources
            Running = false;

            // Allow last loop to finish
            Thread.Sleep(100);

            // Close the GUI
            controlPanel.CloseSIMOC();
        }

        protected override void BuffLoadEvent(object sender, EventArgs e)
        {
            if (Running && finishedWithRun && simocStarted)
            {
                try
                {
                    // Invoke thread-safe access to form properties
                    controlPanel.UpdateProperties();

                    // Update the clock
                    UpdateClock();

                    // First, we grab the new spike data and estimate the chosen observable
                    MakeObservation();

                    // Next, we get the target value
                    GetTargetValue();

                    // Next, we filter the data
                    FilterObservation(firstLoop);

                    // Next, we make the feedback signal
                    CreateFeedback();

                    // If the user has selected to do so, write to file
                    Write2File();

                    // Finally, we update the GUI
                    UpdateGUI();

                    // No longer the first loop
                    firstLoop = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("In BuffLoadEvent: \r\r" + ex.Message);
                }

            }
        }
    }
}