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

        // The two scatter graph objects
        private ScatterGraphController obsScatterGraphController;
        private ScatterGraphController filtScatterGraphController;

        // CL loop speed
        private double clLoopPeriodSec;
        private bool plotsFrozen = false;

        // Delegate for cross thread calls
        delegate void GetTextCallback();
        string controllerResultText = "";

        // GUI parameters
        private double obsHistorySec;
        private double numUnits;

        private double filterWidthSec;
        private double filterC0;
        private double filterC1;
        private double filterC2;
        
        private double targetMean;
        public double targetFreqHz;
        public double targetStd;

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
                // Put sliders into the correct range
                slide_ObsPlotScale.Range = new Range(0.1, slide_PlotRange.Value);
                slide_ObsPlotShift.Range = new Range(-slide_PlotRange.Value, slide_PlotRange.Value);

                slide_FiltPlotScale.Range = new Range(0.1, slide_PlotRange.Value);
                slide_FiltPlotShift.Range = new Range(-slide_PlotRange.Value, slide_PlotRange.Value);

            }
            catch (Exception e)
            {
                MessageBox.Show("Could not update sliders for some reason:" + e.Message);
            }

        }

        internal void UpdateGraphs(SIMOCRawSrv obsSrv, SIMOCRawSrv filtSrv)
        {
            try
            {
                // Update the plots
                obsScatterGraphController.updateScatterGraph(obsSrv,
                        slide_PlotWidth.Value,
                        slide_ObsPlotScale.Value,
                        slide_ObsPlotShift.Value);

                filtScatterGraphController.updateScatterGraph(filtSrv,
                        slide_PlotWidth.Value,
                        slide_FiltPlotScale.Value,
                        slide_FiltPlotShift.Value);
            }
            catch (Exception e)
            {
                MessageBox.Show("Could not update plots for some reason:" + e.Message);
            }
        }

        internal void SetControllerResultText(string ctlResultsText)
        {
            controllerResultText = ctlResultsText;
        }
        internal void UpdateControllerTextbox()
        {
            if (this.textBox_ControllerResults.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(UpdateControllerTextbox);
                this.BeginInvoke(d);
            }
            else
            {
                textBox_ControllerResults.Clear();
                textBox_ControllerResults.Text = controllerResultText;
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

        private void SetDefaultProperties()
        {

            if (this.comboBox_ObsAlg.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(SetDefaultProperties);
                this.Invoke(d);
            }
            else
            {
                this.comboBox_ObsAlg.SelectedIndex = 0;
                this.comboBox_FiltAlg.SelectedIndex = 0;
                this.comboBox_Target.SelectedIndex = 0;
                this.comboBox_FBAlg.SelectedIndex = 0;
            }
        }

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

        private void checkBox_FreezePlots_CheckedChanged(object sender, EventArgs e)
        {
            plotsFrozen = checkBox_FreezePlots.Checked;
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            checkBox_FreezePlots_CheckedChanged(null, null);
        }

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
        }

        private void numericUpDown_NumUnits_ValueChanged(object sender, EventArgs e)
        {
            numUnits = (double)this.numericUpDown_NumUnits.Value;
        }

        private void comboBox_FBAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            contAlg = this.comboBox_FBAlg.SelectedItem.ToString();
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

        private void numericEdit_ObsBuffHistorySec_AfterChangeValue(object sender, AfterChangeNumericValueEventArgs e)
        {
            obsHistorySec = this.numericEdit_ObsBuffHistorySec.Value;
        }

        # endregion

    }
}
