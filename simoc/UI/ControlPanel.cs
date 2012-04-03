using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NationalInstruments.UI;
using simoc.plotting;
using simoc.srv;
using simoc.filewriting;
using NeuroRighter.Output;
using NeuroRighter.DataTypes;
using NeuroRighter;
using System.IO;
using System.Threading;
using simoc.persistantstate;

namespace simoc.UI
{
    public partial class ControlPanel : Form
    {

        // To stop the experiment
        internal bool stopButtonPressed = false;
        internal bool startButtonPressed = false;

        // The comboBox selections
        internal string obsAlg;
        internal string filtAlg;
        internal string targetFunc;
        internal string contAlg;
        internal string tuningAlg;

        // The two scatter graph objects
        private ScatterGraphController obsScatterGraphController;
        private ScatterGraphController filtScatterGraphController;

        // CL loop speed
        private double clLoopPeriodSec;

        // Lock object
        private static readonly object lockObject = new object();

        // Stuff for handling Relay experiment part of GUI
        public delegate void ResetRelayFBEstimateEventHander();
        public event ResetRelayFBEstimateEventHander ResetRelayFBEstimateEvent;
        private double ultimatePeriodSec;
        private double ultimateGain;
        bool fbParametersLocked = false;
        public PersistentSimocVar simocState;

        // Stuff for telling simoc that we switched target functions
        public delegate void TargetFunctionSwitchedEventHander(object sender, TargetEventArgs targetEventArgs);
        public event TargetFunctionSwitchedEventHander TargetFunctionSwitchedEvent;

        // Stuff for telling simoc that we switched observation type
        public delegate void ObservableSwitchedEventHander();
        public event ObservableSwitchedEventHander ObservableSwitchedEvent;

        // GUI parameters
        private delegate void GetTextCallback();
        private double obsHistorySec;
        private double numUnits;
        private bool trackTarget;

        private double filterWidthSec;
        private double filterC0;
        private double filterC1;
        private double filterC2;

        private double targetMean;
        private double targetFreqHz;
        private double targetStd;
        private double targetMultiplier;

        private double controllerC0;
        private double controllerC1;
        private double controllerC2;
        private double controllerC3;
        private double controllerC4;
        private double controllerC5;

        public ControlPanel(double pollingPeriodSec)
        {
            InitializeComponent();

            // What is the loop time
            clLoopPeriodSec = pollingPeriodSec;

            // Take care of obs scatter graphs
            // Remove existing plots
            for (int i = scatterGraph_Obs.Plots.Count - 1; i >= 0; --i)
            {
                scatterGraph_Obs.Plots.RemoveAt(i);
            }
            // Initialize the aux data scatter graph with a plot for each aux Analog channel
            for (int i = 0; i < 1; ++i)
            {
                ScatterPlot p = new ScatterPlot();
                scatterGraph_Obs.Plots.Add(p);
            }

            // Take care of filt scatter graphs
            // Remove existing plots
            for (int i = scatterGraph_Filt.Plots.Count - 1; i >= 0; --i)
            {
                scatterGraph_Filt.Plots.RemoveAt(i);
            }
            // Initialize the aux data scatter graph with a plot for each aux Analog channel
            for (int i = 0; i < 3; ++i)
            {
                ScatterPlot p = new ScatterPlot();
                scatterGraph_Filt.Plots.Add(p);
            }

            // Create the scatter graph controllers
            obsScatterGraphController =
                new ScatterGraphController(ref scatterGraph_Obs);
            filtScatterGraphController =
                new ScatterGraphController(ref scatterGraph_Filt);

            // Set the default properties
            SetDefaultProperties();

        }

        internal void UpdateGraphSliders()
        {
            try
            {
                lock (lockObject)
                {
                    // Put sliders into the correct range
                    slide_ObsPlotScale.Range = new Range(0.1, slide_PlotRange.Value);
                    slide_ObsPlotShift.Range = new Range(-slide_PlotRange.Value, slide_PlotRange.Value);

                    slide_FiltPlotScale.Range = new Range(0.1, slide_PlotRange.Value);
                    slide_FiltPlotShift.Range = new Range(-slide_PlotRange.Value, slide_PlotRange.Value);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not update sliders for some reason: " + e.Message);
            }

        }

        internal void UpdateGraphs(SIMOCRawSrv obsSrv, SIMOCRawSrv filtSrv)
        {
            try
            {
                lock (lockObject)
                {
                    // Update the plots
                    obsScatterGraphController.UpdateScatterGraph(obsSrv,
                            slide_PlotWidth.Value,
                            slide_ObsPlotScale.Value,
                            slide_ObsPlotShift.Value);

                    filtScatterGraphController.UpdateScatterGraph(filtSrv,
                            slide_PlotWidth.Value,
                            slide_FiltPlotScale.Value,
                            slide_FiltPlotShift.Value);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not update plots for some reason: " + e.Message);
            }
        }

        internal void SetControllerResultText(double Tu, double Ku)
        {
            ultimatePeriodSec = Tu;
            ultimateGain = Ku;
        }

        internal void UpdateControllerTextbox()
        {
            if (!fbParametersLocked)
            {

                if (this.textBox_Ku.InvokeRequired)
                {
                    GetTextCallback d = new GetTextCallback(UpdateControllerTextbox);
                    this.BeginInvoke(d);
                }
                else
                {
                    textBox_Tu.Text = Convert.ToString(ultimatePeriodSec);
                    textBox_Ku.Text = Convert.ToString(ultimateGain);

                    switch (tuningAlg)
                    {
                        case "Ziegler-Nichols-PI":
                            {
                                textBox_K.Text = Convert.ToString(0.4 * ultimateGain / 10);
                                textBox_Ti.Text = Convert.ToString(0.8 * ultimatePeriodSec);
                                textBox_Td.Text = "0.0";
                            }
                            break;
                        case "Ziegler-Nichols-PID":
                            {
                                textBox_K.Text = Convert.ToString(0.6 * ultimateGain / 10);
                                textBox_Ti.Text = Convert.ToString(0.5 * ultimatePeriodSec);
                                textBox_Td.Text = Convert.ToString(0.125 * ultimatePeriodSec);
                            }
                            break;
                    }


                }
            }

        }
        
        internal void CloseSIMOC()
        {
            if (this.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(CloseSIMOC);
                this.Invoke(d);
            }
            else
            {
                this.Close();
                this.Dispose();
            }
        }

        internal void GetStateVariables(ref PersistentSimocVar simocPersistantState)
        {
            // Simoc state class
            simocState = simocPersistantState;
        }
        
        private void SetDefaultProperties()
        {

            if (this.comboBox_ObsAlg.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(SetDefaultProperties);
                this.Invoke(d);
            }
            else
            {
                this.comboBox_ObsAlg.SelectedIndex = 1;
                this.comboBox_FiltAlg.SelectedIndex = 1;
                this.comboBox_Target.SelectedIndex = 0;
                this.comboBox_FBAlg.SelectedIndex = 0;
                this.comboBox_TuningType.SelectedIndex = 0;
            }
        }

        # region Button Event Handlers
        private void button_StartSIMOC_Click(object sender, EventArgs e)
        {
            numericEdit_ObsBuffHistorySec.Enabled = false;

            // Set the max width of the plots
            slide_PlotWidth.Range = new Range(clLoopPeriodSec * 2, numericEdit_ObsBuffHistorySec.Value);

            button_StopSIMOC.Enabled = true;
            button_StartSIMOC.Enabled = false;
            startButtonPressed = true;
        }

        private void button_StopSIMOC_Click(object sender, EventArgs e)
        {
            stopButtonPressed = true;
        }

        //private void checkBox_FreezePlots_CheckedChanged(object sender, EventArgs e)
        //{
        //    plotsFrozen = checkBox_FreezePlots.Checked;
        //}

        //private void pictureBox5_Click(object sender, EventArgs e)
        //{
        //    checkBox_FreezePlots_CheckedChanged(null, null);
        //}

        private void button_ResetEstimates_Click(object sender, EventArgs e)
        {
            if (!fbParametersLocked)
            {
                ResetRelayFBEstimateEvent();
            }
        }

        private void button_LockEstimates_Click(object sender, EventArgs e)
        {
            if (fbParametersLocked)
            {
                fbParametersLocked = false;
                button_LockEstimates.Text = "Lock Estimates";
            }
            else
            {
                fbParametersLocked = true;
                button_LockEstimates.Text = "UnLock Estimates";
            }
        }

        private void button_SendEstimates_Click(object sender, EventArgs e)
        {
            try
            {
                switch (contAlg)
                {
                    case "PID Irrad":
                        {
                            numericUpDown_ContC0.Value = Convert.ToDecimal(textBox_K.Text);
                            numericUpDown_ContC1.Value = Convert.ToDecimal(textBox_Ti.Text);
                            numericUpDown_ContC2.Value = Convert.ToDecimal(textBox_Td.Text);
                        }
                        break;
                    case "PID DutyCycle":
                        {
                            numericUpDown_ContC0.Value = Convert.ToDecimal(textBox_K.Text);
                            numericUpDown_ContC1.Value = Convert.ToDecimal(textBox_Ti.Text);
                            numericUpDown_ContC2.Value = Convert.ToDecimal(textBox_Td.Text);
                        }
                        break;
                    case "PID Power":
                        {
                            numericUpDown_ContC0.Value = Convert.ToDecimal(textBox_K.Text);
                            numericUpDown_ContC1.Value = Convert.ToDecimal(textBox_Ti.Text);
                            numericUpDown_ContC2.Value = Convert.ToDecimal(textBox_Td.Text);
                        }
                        break;
                }
            }
            catch
            {
                MessageBox.Show("One of the tuning parameters was invalid");
            }
        }

        # endregion

        #region public access methods

        /// <summary>
        /// Observation history in seconds to store
        /// </summary>
        public double ObsHistorySec
        {
            get
            {
                return obsHistorySec;
            }
        }

        /// <summary>
        /// Number of units detected
        /// </summary>
        public double NumUnits
        {
            get
            {
                return numUnits;
            }
        }

        /// <summary>
        /// Get the filter window width.
        /// </summary>
        public double FilterWidthSec
        {
            get
            {
                return filterWidthSec;
            }
        }

        /// <summary>
        /// Get the first filter coefficient
        /// </summary>
        public double FilterC0
        {
            get
            {
                return filterC0;
            }
        }

        /// <summary>
        /// Get the second filter coefficient
        /// </summary>
        public double FilterC1
        {
            get
            {
                return filterC0;
            }
        }

        /// <summary>
        /// Get the third filter coefficient
        /// </summary>
        public double FilterC2
        {
            get
            {
                return filterC0;
            }
        }

        /// <summary>
        /// Get the DC part of the target
        /// </summary>
        public double TargetMean
        {
            get
            {
                return targetMean;
            }
        }

        /// <summary>
        /// Get the target frequency
        /// </summary>
        public double TargetFreqHz
        {
            get
            {
                return targetFreqHz;
            }
        }

        /// <summary>
        /// Get the target standard deviation
        /// </summary>
        public double TargetStd
        {
            get
            {
                return targetStd;
            }
        }

        /// <summary>
        /// Get the target standard deviation
        /// </summary>
        public double TargetMultiplier
        {
            get
            {
                return targetMultiplier;
            }
        }

        /// <summary>
        /// Get the first controller coefficient
        /// </summary>
        public double ControllerC0
        {
            get
            {
                return controllerC0;
            }
        }

        /// <summary>
        /// Get the second controller coefficient
        /// </summary>
        public double ControllerC1
        {
            get
            {
                return controllerC1;
            }
        }

        /// <summary>
        /// Get the third controller coefficient
        /// </summary>
        public double ControllerC2
        {
            get
            {
                return controllerC2;
            }
        }

        /// <summary>
        /// Get the foruth controller coefficient
        /// </summary>
        public double ControllerC3
        {
            get
            {
                return controllerC3;
            }
        }

        /// <summary>
        /// Get the fifth controller coefficient
        /// </summary>
        public double ControllerC4
        {
            get
            {
                return controllerC4;
            }
        }

        /// <summary>
        /// Get the sixth controller coefficient
        /// </summary>
        public double ControllerC5
        {
            get
            {
                return controllerC5;
            }
        }

        # endregion

        # region private value changed event handlers

        private void numericUpDown_TargetMean_ValueChanged(object sender, EventArgs e)
        {
            targetMean = (double)this.numericUpDown_TargetMean.Value;
        }

        private void comboBox_FiltAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            filtAlg = this.comboBox_FiltAlg.SelectedItem.ToString();
        }

        private void comboBox_Target_SelectedIndexChanged(object sender, EventArgs e)
        {
            targetFunc = this.comboBox_Target.SelectedItem.ToString();
            if (TargetFunctionSwitchedEvent != null)
                OnTargetFunctionSwitchedEvent(new TargetEventArgs(true));
        }

        private void comboBox_TuningType_SelectedIndexChanged(object sender, EventArgs e)
        {
            tuningAlg = comboBox_TuningType.SelectedItem.ToString();
            UpdateControllerTextbox();
        }

        private void numericEdit_FiltWidthSec_AfterChangeValue(object sender, AfterChangeNumericValueEventArgs e)
        {
            filterWidthSec = (double)this.numericEdit_FiltWidthSec.Value;
        }

        private void numericEdit_FiltC0_AfterChangeValue(object sender, AfterChangeNumericValueEventArgs e)
        {
            filterC0 = this.numericEdit_FiltC0.Value;
        }

        private void numericEdit_FiltC1_AfterChangeValue(object sender, AfterChangeNumericValueEventArgs e)
        {
            filterC1 = this.numericEdit_FiltC1.Value;
        }

        private void numericEdit_FiltC2_AfterChangeValue(object sender, AfterChangeNumericValueEventArgs e)
        {
            filterC2 = this.numericEdit_FiltC2.Value;
        }

        private void numericUpDown_TargetSD_ValueChanged(object sender, EventArgs e)
        {
            targetStd = (double)this.numericUpDown_TargetSD.Value;
        }

        private void numericUpDown_TargetFreq_ValueChanged(object sender, EventArgs e)
        {
            targetFreqHz = (double)this.numericUpDown_TargetFreq.Value;
        }

        private void comboBox_ObsAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            obsAlg = this.comboBox_ObsAlg.SelectedItem.ToString();
            if (ObservableSwitchedEvent != null)
                ObservableSwitchedEvent();
        }

        private void numericUpDown_NumUnits_ValueChanged(object sender, EventArgs e)
        {
            numUnits = (double)this.numericUpDown_NumUnits.Value;
        }

        private void comboBox_FBAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            contAlg = this.comboBox_FBAlg.SelectedItem.ToString();

            switch (contAlg)
            {
                case "None":
                    {
                        label_CtlType.Text = "Controller 0";
                    }
                    break;
                case "Relay Irrad":
                    {
                        label_ContC0.Text = "Vi (0-5)";
                        label_ContC1.Text = "dError";
                        label_ContC2.Text = "";
                        label_ContC3.Text = "Freq (Hz)";
                        label_ContC4.Text = "Pulse Width";
                        label_ContC5.Text = "";
                        label_CtlType.Text = "Controller 3";
                    }
                    break;
                case "PID Irrad":
                    {
                        label_ContC0.Text = "K";
                        label_ContC1.Text = "Ti";
                        label_ContC2.Text = "Td";
                        label_ContC3.Text = "Freq (Hz)";
                        label_ContC4.Text = "Pulse Width (msec)";
                        label_ContC5.Text = "";
                        label_CtlType.Text = "Controller 4";
                    }
                    break;
                case "Relay DutyCycle":
                    {
                        label_ContC0.Text = "U_max (0-1)";
                        label_ContC1.Text = "dError";
                        label_ContC2.Text = "";
                        label_ContC3.Text = "Fmax (Hz)";
                        label_ContC4.Text = "PWmax (mSec)";
                        label_ContC5.Text = "Vi (0-5)";

                        label_CtlType.Text = "Controller 5";

                        numericUpDown_ContC0.Value = 1.0m;
                        numericUpDown_ContC1.Value = 0.0m;
                        numericUpDown_ContC2.Value = 0.0m;
                        numericUpDown_ContC3.Value = 29.0m;
                        numericUpDown_ContC4.Value = 5.0m;
                        numericUpDown_ContC5.Value = 5.0m;
                    }
                    break;
                case "PID DutyCycle":
                    {
                        label_ContC0.Text = "K";
                        label_ContC1.Text = "Ti";
                        label_ContC2.Text = "Td";
                        label_ContC3.Text = "Fmax (Hz)";
                        label_ContC4.Text = "PWmax (mSec)";
                        label_ContC5.Text = "Vi (0-5)";

                        label_CtlType.Text = "Controller 6";

                        numericUpDown_ContC0.Value = 1.0m;
                        numericUpDown_ContC1.Value = 1.0m;
                        numericUpDown_ContC2.Value = 100.0m;
                        numericUpDown_ContC3.Value = 29.0m;
                        numericUpDown_ContC4.Value = 5.0m;
                        numericUpDown_ContC5.Value = 5.0m;
                    }
                    break;
                case "Relay Power":
                    {
                        label_ContC0.Text = "U_max (0-1)";
                        label_ContC1.Text = "dError";
                        label_ContC2.Text = "";
                        label_ContC3.Text = "Fmax (Hz)";
                        label_ContC4.Text = "PWmax (mSec)";
                        label_ContC5.Text = "Vimax (0-5)";

                        label_CtlType.Text = "Controller 5";

                        numericUpDown_ContC0.Value = 1.0m;
                        numericUpDown_ContC1.Value = 0.0m;
                        numericUpDown_ContC2.Value = 0.0m;
                        numericUpDown_ContC3.Value = 29.0m;
                        numericUpDown_ContC4.Value = 5.0m;
                        numericUpDown_ContC5.Value = 5.0m;
                    }
                    break;
                case "PID Power":
                    {
                        label_ContC0.Text = "K";
                        label_ContC1.Text = "Ti";
                        label_ContC2.Text = "Td";
                        label_ContC3.Text = "Fmax (Hz)";
                        label_ContC4.Text = "PWmax (mSec)";
                        label_ContC5.Text = "Vimax (0-5)";

                        label_CtlType.Text = "Controller 8";

                        numericUpDown_ContC0.Value = 0.25m;
                        numericUpDown_ContC1.Value = 2.0m;
                        numericUpDown_ContC2.Value = 0.1m;
                        numericUpDown_ContC3.Value = 20.0m;
                        numericUpDown_ContC4.Value = 5.0m;
                        numericUpDown_ContC5.Value = 5.0m;
                    }
                    break;
                case "PID Power Multimodal":
                    {
                        label_ContC0.Text = "K";
                        label_ContC1.Text = "Ti";
                        label_ContC2.Text = "Td";
                        label_ContC3.Text = "Fmax (Hz)";
                        label_ContC4.Text = "PWmax (mSec)";
                        label_ContC5.Text = "Vimax (0-5)";

                        label_CtlType.Text = "Controller 8";

                        numericUpDown_ContC0.Value = 0.25m;
                        numericUpDown_ContC1.Value = 2.0m;
                        numericUpDown_ContC2.Value = 0.1m;
                        numericUpDown_ContC3.Value = 20.0m;
                        numericUpDown_ContC4.Value = 5.0m;
                        numericUpDown_ContC5.Value = 5.0m;
                    }
                    break;
                case "Integral Bang-Bang":
                    {
                        label_ContC0.Text = "K";
                        label_ContC1.Text = "Ti";
                        label_ContC2.Text = "";
                        label_ContC3.Text = "Fmax (Hz)";
                        label_ContC4.Text = "PWmax (mSec)";
                        label_ContC5.Text = "Vimax (0-5)";

                        label_CtlType.Text = "Controller 8";

                        numericUpDown_ContC0.Value = 1.0m;
                        numericUpDown_ContC1.Value = 1.0m;
                        numericUpDown_ContC2.Value = 100.0m;
                        numericUpDown_ContC3.Value = 10.0m;
                        numericUpDown_ContC4.Value = 5.0m;
                        numericUpDown_ContC5.Value = 5.0m;
                    }
                    break;
                default:
                    {
                        label_ContC0.Text = "c0";
                        label_ContC1.Text = "c1";
                        label_ContC2.Text = "c2";
                        label_ContC3.Text = "c3";
                        label_ContC4.Text = "c4";
                        label_ContC5.Text = "c5";
                        label_CtlType.Text = "Controller X";
                    }
                    break;
            }

            if (checkBox_GetTuneParams.Checked)
            {
                button_SendEstimates_Click(null, null);
            }
        }

        private void numericUpDown_ContC0_ValueChanged(object sender, EventArgs e)
        {
            controllerC0 = (double)this.numericUpDown_ContC0.Value;
        }

        private void numericUpDown_ContC1_ValueChanged(object sender, EventArgs e)
        {
            controllerC1 = (double)this.numericUpDown_ContC1.Value;
        }

        private void numericUpDown_ContC2_ValueChanged(object sender, EventArgs e)
        {
            controllerC2 = (double)this.numericUpDown_ContC2.Value;
        }

        private void numericUpDown_ContC3_ValueChanged(object sender, EventArgs e)
        {
            controllerC3 = (double)this.numericUpDown_ContC3.Value;
        }

        private void numericUpDown_ContC4_ValueChanged(object sender, EventArgs e)
        {
            controllerC4 = (double)this.numericUpDown_ContC4.Value;
        }

        private void numericUpDown_ContC5_ValueChanged(object sender, EventArgs e)
        {
            controllerC5 = (double)this.numericUpDown_ContC5.Value;
        }

        private void numericUpDown_TargetMultiplier_ValueChanged(object sender, EventArgs e)
        {
            targetMultiplier = (double)this.numericUpDown_TargetMultiplier.Value;
        }

        private void numericEdit_ObsBuffHistorySec_AfterChangeValue(object sender, AfterChangeNumericValueEventArgs e)
        {

            obsHistorySec = this.numericEdit_ObsBuffHistorySec.Value;

            try
            {
                lock (lockObject)
                {
                  // Set the range of the history slider
                    slide_PlotWidth.Range = new Range(1.0, obsHistorySec);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update history slider for some reason: " + ex.Message);
            }

            
        }

        private void checkBox_AllowTargetTracking_CheckedChanged(object sender, EventArgs e)
        {
            simocState.TrackTarget = checkBox_AllowTargetTracking.Checked;
        }

        # endregion

        # region Event Handling


        internal void TriggerTargetSwitch(bool resetTargetTime)
        {
            OnTargetFunctionSwitchedEvent(new TargetEventArgs(resetTargetTime));
        }

        protected virtual void OnTargetFunctionSwitchedEvent(TargetEventArgs e)
        {
            if (TargetFunctionSwitchedEvent != null) TargetFunctionSwitchedEvent(this, e);
        }


        # endregion




    }
}
