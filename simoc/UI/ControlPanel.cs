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
        private double clLoopPeriodSec = 1;
        private bool plotsFrozen = false;

        // Delegate for cross thread calls
        delegate void GetTextCallback();

        public ControlPanel()
        {
            InitializeComponent();

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
            for (int i =  scatterGraph_Filt.Plots.Count - 1; i >= 0; --i)
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
                new ScatterGraphController(ref scatterGraph_Obs, false);
            filtScatterGraphController =
                new ScatterGraphController(ref scatterGraph_Filt, false);

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
            catch(Exception e)
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
            catch(Exception e)
            {
                MessageBox.Show("Could not update plots for some reason:" + e.Message);
            }
        }

        internal void UpdateProperties()
        {

            if (this.comboBox_ObsAlg.InvokeRequired)
            {
                GetTextCallback d = new GetTextCallback(UpdateProperties);
                this.Invoke(d);
            }
            else
            {
                obsAlg = this.comboBox_ObsAlg.SelectedItem.ToString();
                filtAlg = this.comboBox_FiltAlg.SelectedItem.ToString();
                targetFunc = this.comboBox_Target.SelectedItem.ToString();
                contAlg = this.comboBox_FBAlg.SelectedItem.ToString();
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

    }
}
