using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace NR_CL_Examples
{
    public partial class SeizureControlPanel : Form
    {
        // Plot Buffer
        double sampleRateHz;
        double sampleperiodSec;
        double plotHistorySec;

        // UI states
        bool fbEngaged;
        string statSelection;
        double thresholdK;
        bool retrainRequired = false;
        double stimFreqHz;
        double stimTimeSec;
        double stimAmpVolts;
        double maxUpdateRate;

        // Random
        const int CP_NOCLOSE_BUTTON = 0x200; // to get rid of close button

        public SeizureControlPanel(double sampleRateHz, double plotHistorySec, string selectedStat)
        {
            InitializeComponent();
            this.MaximizeBox = false;
            this.sampleRateHz = sampleRateHz;
            this.sampleperiodSec = 1 / sampleRateHz;
            this.plotHistorySec = plotHistorySec;
            this.statSelection = selectedStat;
            this.thresholdK = (double)numericUpDown_TreshK.Value;
            this.stimFreqHz = (double)numericUpDown_StimFreqHz.Value;
            this.stimTimeSec = (double)numericUpDown_StimTimeSec.Value;
            this.stimAmpVolts = (double)numericUpDown_StimAmpVolts.Value;

            // Update stat label
            label_SelectedStat.Text = selectedStat;
            label_SelectedStat.ForeColor = Color.ForestGreen;


        }

        internal void UpdatePlots(List<LFPChannel> lfp, bool stimulationNow, double maxUpdateRate)
        {
            double[][] newBuffer = new double[lfp.Count][];
            double[] newThresh = new double[lfp.Count];

            for (int i = 0; i < newThresh.Length; i++)
            {
                newBuffer[i] = lfp[i].stat.Buffer.ToArray();
                newThresh[i] = lfp[i].statStandard.BaseStat * lfp[i].ThesholdCoefficient;
            }

            // Run the bw
            PlotData pd = new PlotData(newBuffer, newThresh.Average(),stimulationNow);

            if (!backgroundWorker_UpdateUI.IsBusy)
                backgroundWorker_UpdateUI.RunWorkerAsync(pd);
        }

        private void backgroundWorker_UpdateUI_DoWork(object sender, DoWorkEventArgs e)
        {

            try
            {
                // Get data
                PlotData pd = (PlotData)e.Argument;

                // Update the plot
                // Create GraphPanes
                zgc.GraphPane.CurveList.Clear();
                GraphPane pane = zgc.GraphPane;

                if (pd.StimulationNow)
                {
                    SetMinorPlotParametersStim(ref pane);
                }
                else
                {
                    List<LineItem> myCurves = new List<LineItem>();

                    // Plot the spikes
                    List<PointPairList> ppl = new List<PointPairList>();
                    PointPairList pplThresh = new PointPairList();
                    for (int i = 0; i < pd.PlotPoints.Length; i++)
                    {
                        ppl.Add(new PointPairList());
                        for (int j = 0; j < pd.PlotPoints[i].Length; j++)
                        {
                            ppl[i].Add((double)j * sampleperiodSec, pd.PlotPoints[i][j]);
                        }
                    }

                    pplThresh.Add(0, pd.PlotThreshold);
                    pplThresh.Add(plotHistorySec, pd.PlotThreshold);

                    // Add data tp plot
                    for (int i = 0; i < pd.PlotPoints.Length; i++)
                    {
                        myCurves.Add(pane.AddCurve("Chan. " + i, ppl[i], Color.RoyalBlue, SymbolType.None));
                    }
                    myCurves.Add(pane.AddCurve("Mean Threshold", pplThresh, Color.Red, SymbolType.None));

                    // Set plot parameteters
                    SetMinorPlotParametersNormal(ref pane);
                }

                // Show data
                zgc.AxisChange();
                zgc.Invalidate();
            }
            catch
            {
                return;
            }

        }

        private void SetMinorPlotParametersNormal(ref GraphPane gp)
        {
            // Set Axis titles
            gp.Title.Text = statSelection + " and Threshold";
            gp.XAxis.Title.Text = "Time (sec)";
            gp.YAxis.Title.Text = statSelection;

            // Display the Y zero line
            gp.Legend.IsVisible = false;
            gp.YAxis.MajorGrid.IsZeroLine = false;

            // Align the Y axis labels so they are flush to the axis
            gp.YAxis.Scale.Align = AlignP.Inside;

            // Manually set the axis range
            gp.YAxis.Scale.Min = 0;
            gp.XAxis.Scale.Min = 0;
            gp.XAxis.Scale.Max = plotHistorySec;

            // Fill the axis background with a gradient
            gp.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
        }

        private void SetMinorPlotParametersStim(ref GraphPane gp)
        {
            // Text settings
            gp.Legend.IsVisible = false;
            gp.Title.IsVisible = false;
            gp.XAxis.Title.IsVisible = false;
            gp.YAxis.Title.IsVisible = false;

            //turn off the opposite tics so the Y tics don't show up on the Y2 axis
            gp.XAxis.IsVisible = true;
            gp.YAxis.IsVisible = true;

            // Display the Y zero line
            gp.YAxis.MajorGrid.IsZeroLine = false;
            gp.YAxis.MinorGrid.IsVisible = false;

            // Align the Y axis labels so they are flush to the axis
            gp.YAxis.Scale.Align = AlignP.Inside;

            // Manually set the axis range
            gp.YAxis.Scale.Min = 0;
            gp.XAxis.Scale.Min = 0;
            gp.XAxis.Scale.Max = plotHistorySec;

            // Fill the axis background with a gradient
            gp.Chart.Fill = new Fill(Color.Purple, Color.Red, 45.0f);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        #region Public Accessors

        /// <summary>
        /// Is there actual feedback being output
        /// </summary>
        public bool FBEngaged
        {
            get
            {
                return fbEngaged;
            }
        }

        /// <summary>
        /// Get the current threshold coefficient
        /// </summary>
        public double ThresholdK
        {
            get
            {
                return thresholdK;
            }
        }

        /// <summary>
        /// Get the current stimulation rate
        /// </summary>
        public double StimFreqHz
        {
            get
            {
                return stimFreqHz;
            }
        }

        /// <summary>
        /// Get the current stimuation time
        /// </summary>
        public double StimTimeSec
        {
            get
            {
                return stimTimeSec;
            }
        }

        /// <summary>
        /// The current stimuation amplitude (one direction for biphasic stimuli)
        /// </summary>
        public double StimAmpVolts
        {
            get
            {
                return stimAmpVolts;
            }
        }

        /// <summary>
        /// Do we need to update the threshold
        /// </summary>
        public bool RetrainRequired
        {
            get
            {
                return retrainRequired;
            }
            set
            {
                retrainRequired = value;
            }
        }

        #endregion

        #region Form event Handlers

        private void button_RetrainThresh_Click(object sender, EventArgs e)
        {
            retrainRequired = true;
        }

        private void button_EngageFB_Click(object sender, EventArgs e)
        {
            if (fbEngaged)
            {
                fbEngaged = false;
                button_EngageFB.Text = "Engage Feedback";
                button_EngageFB.ForeColor = Color.Black;
            }
            else
            {
                fbEngaged = true;
                button_EngageFB.Text = "Disengage Feedback";
                button_EngageFB.ForeColor = Color.Green;
            }
        }

        private void numericUpDown_TreshK_ValueChanged(object sender, EventArgs e)
        {
            thresholdK = (double)numericUpDown_TreshK.Value;
        }

        private void numericUpDown_StimFreqHz_ValueChanged(object sender, EventArgs e)
        {
            stimFreqHz = (double)numericUpDown_StimFreqHz.Value;
        }

        private void numericUpDown_StimTimeSec_ValueChanged(object sender, EventArgs e)
        {
            stimTimeSec = (double)numericUpDown_StimTimeSec.Value;
        }

        private void numericUpDown_StimAmpVolts_ValueChanged(object sender, EventArgs e)
        {
            stimAmpVolts = (double)numericUpDown_StimAmpVolts.Value;
        }
        #endregion
    }
}
