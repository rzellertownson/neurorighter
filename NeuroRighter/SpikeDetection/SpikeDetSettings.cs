using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using rawType = System.Double;
using NRSpikeSort;
using ZedGraph;
using ExtensionMethods;

namespace NeuroRighter.SpikeDetection
{
    [Serializable]
    sealed internal partial class SpikeDetSettings : Form
    {

        // Parameters passed in from NR interface
        private int spikeBufferLength;
        private int numChannels;

        // Detector
        internal SpikeDetectorParameters detectorParameters = new SpikeDetectorParameters();
        internal SpikeDetector spikeDetector;
        internal SpikeDetector spikeDetectorRaw;
        internal SpikeDetector spikeDetectorSalpa;
        private int numPre; // num smaples to save pre-spike
        private int numPost; // num samples to save post-spike
        internal int spikeDetectionLag; // number of samples that spike detector will cause buffers to lag
        internal int detectorType = 0;

        // Spike sorter
        internal SpikeSorter spikeSorter;
        private bool isHoarding = false;
        private bool isTrained = false;
        private bool isEngaged = false;
        internal bool hasData;
        private BackgroundWorker sorterTrainer;

        // Delegates for informing mainform of settings change
        internal delegate void resetSpkDetSettingsHandler(object sender, EventArgs e);
        internal event resetSpkDetSettingsHandler SettingsHaveChanged;
        private delegate void SetTextCallback();

        public SpikeDetSettings(int spikeBufferLength, int numChannels)
        {
            this.spikeBufferLength = spikeBufferLength;
            this.numChannels = numChannels;

            InitializeComponent();

            // Save window state in application data folder
            spkDetpersistWindowComponent.XMLFilePath = Properties.Settings.Default.persistWindowPath;

            //Default spike det. algorithm is fixed RMS
            this.comboBox_noiseEstAlg.SelectedIndex = 0;
            this.comboBox_spikeDetAlg.SelectedIndex = 0;
            this.numPre = (int)((double)numPreSamples.Value / 1e6 * Properties.Settings.Default.RawSampleFrequency);
            this.numPost = (int)((double)numPostSamples.Value / 1e6 * Properties.Settings.Default.RawSampleFrequency);

            // Set the pre/post sample coversion label
            label_PreSampConv.Text =
                numPre + " samples"; ;
            label_PostSampConv.Text =
                numPost + " samples"; ;

            // Set up the spike sorter's BW
            sorterTrainer = new BackgroundWorker();
            sorterTrainer.DoWork +=
                new DoWorkEventHandler(sorterTrainer_trainSS);
            sorterTrainer.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(sorterTrainer_DoneTraining);

            // Set projection type
            comboBox_ProjectionType.SelectedIndex = 0;

            // Copy default settings to the parameter object
            detectorParameters.NoiseAlgType = comboBox_noiseEstAlg.SelectedIndex;
            detectorParameters.DetectorType = comboBox_spikeDetAlg.SelectedIndex;
            detectorParameters.Threshold = thresholdMultiplier.Value;
            detectorParameters.DeadTime = numericUpDown_DeadTime.Value;
            detectorParameters.MaxSpikeAmp = numericUpDown_MaxSpkAmp.Value;
            detectorParameters.MinSpikeSlope = numericUpDown_MinSpikeSlope.Value;
            detectorParameters.MinSpikeWidth = numericUpDown_MinSpikeWidth.Value;
            detectorParameters.MaxSpikeWidth = numericUpDown_MaxSpikeWidth.Value;
            detectorParameters.NumPre = numPreSamples.Value;
            detectorParameters.NumPost = numPostSamples.Value;

            Flush();
        }

        internal void SetSpikeDetector(int spkBufferLength)
        {
            this.spikeBufferLength = spkBufferLength;

            int detectionDeadTime = (int)Math.Round(Convert.ToDouble(Properties.Settings.Default.RawSampleFrequency) *
                (double)numericUpDown_DeadTime.Value / 1.0e6);
            int minSpikeWidth = (int)Math.Floor(Convert.ToDouble(Properties.Settings.Default.RawSampleFrequency) *
                (double)numericUpDown_MinSpikeWidth.Value / 1.0e6);
            int maxSpikeWidth = (int)Math.Round(Convert.ToDouble(Properties.Settings.Default.RawSampleFrequency) *
                (double)numericUpDown_MaxSpikeWidth.Value / 1.0e6);
            double maxSpikeAmp = (double)numericUpDown_MaxSpkAmp.Value / 1.0e6;
            double minSpikeSlope = (double)numericUpDown_MinSpikeSlope.Value / 1.0e6;

            // Repopulate conversion table
            label_deadTimeSamp.Text = detectionDeadTime + " sample(s)";
            label_MinWidthSamp.Text = minSpikeWidth + " sample(s)";
            label_MaxWidthSamp.Text = maxSpikeWidth + " sample(s)";

            // Reset the number of samples
            this.numPre = (int)((double)numPreSamples.Value / 1e6 * Properties.Settings.Default.RawSampleFrequency);
            this.numPost = (int)((double)numPostSamples.Value / 1e6 * Properties.Settings.Default.RawSampleFrequency);
            if (numPre == 0)
                numPre = 1;
            if (numPost == 0)
                numPost = 1;

            // Set the pre/post sample coversion label
            label_PreSampConv.Text =
                numPre + " samples"; ;
            label_PostSampConv.Text =
                numPost + " samples"; ;

            // Half a millisecond to determine spike polarity
            int spikeIntegrationTime = (int)Math.Ceiling(Convert.ToDouble(Properties.Settings.Default.RawSampleFrequency) / 1000);

            switch (comboBox_noiseEstAlg.SelectedIndex)
            {
                case 0:  //RMS Fixed
                    spikeDetector = new RMSThresholdFixed(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime, minSpikeWidth, maxSpikeWidth,
                        maxSpikeAmp, minSpikeSlope, spikeIntegrationTime, Properties.Settings.Default.ADCPollingPeriodSec);

                    spikeDetectorRaw = spikeDetector.DeepClone();
                    spikeDetectorSalpa = spikeDetector.DeepClone(); 

                    break;
                case 1:  //RMS Adaptive
                    spikeDetector = new AdaptiveRMSThreshold(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime, minSpikeWidth, maxSpikeWidth,
                        maxSpikeAmp, minSpikeSlope, spikeIntegrationTime, Properties.Settings.Default.ADCPollingPeriodSec);

                    spikeDetectorRaw = spikeDetector.DeepClone();
                    spikeDetectorSalpa = spikeDetector.DeepClone(); 

                    break;
                case 2:  //Limada
                    spikeDetector = new LimAda(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime, minSpikeWidth, maxSpikeWidth,
                        maxSpikeWidth, minSpikeSlope, spikeIntegrationTime, Convert.ToInt32(Properties.Settings.Default.RawSampleFrequency));

                    spikeDetectorRaw = spikeDetector.DeepClone();
                    spikeDetectorSalpa = spikeDetector.DeepClone(); 

                    break;
                default:
                    break;
            }

            switch (comboBox_spikeDetAlg.SelectedIndex)
            {
                case 0:  //auto-aligner
                    numericUpDown_MaxSpikeWidth.Enabled = true;
                    numericUpDown_MinSpikeWidth.Enabled = true;
                    detectorType = 0;
                    break;
                case 1:  //simple
                    numericUpDown_MaxSpikeWidth.Enabled = false;
                    numericUpDown_MinSpikeWidth.Enabled = false;
                    detectorType = 1;
                    break;
                default:
                    break;
            }

            spikeDetectionLag = spikeDetector.serverLag;
        }

        internal void UpdateCollectionBar()
        {
            if (spikeSorter != null)
            {
                if (this.textBox_Results.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(UpdateCollectionBar);
                    this.Invoke(d);
                }
                else
                {
                    int spikesCollected = spikeSorter.trainingSpikes.Buffer.Count;
                    this.label_NumSpikesCollected.Text = spikesCollected.ToString();
                    UpdateSpikeCollectionPlot();
                }
            }
        }

        private void Flush()
        {
            // Update the UI to reflect the state of things
            button_TrainSorter.Enabled = false;
            button_EngageSpikeSorter.Enabled = false;
            label_SorterEngaged.Text = "Sorter is not engaged";
            label_SorterEngaged.ForeColor = Color.Red;
            label_Trained.Text = "Spike sorter is not trained.";
            label_Trained.ForeColor = Color.Red;
            button_HoardSpikes.Text = "Hoard";
            UpdateCollectionBar();

            // Reset the plot
            RefreshSpikeCollectionPlot();
        }

        internal void DisableFileMenu()
        {
            saveDetectorToolStripMenuItem.Enabled = false;
        }

        internal void EnableFileMenu()
        {
            saveDetectorToolStripMenuItem.Enabled = true;
        }

        private void ReportTrainingResults()
        {
            this.UseWaitCursor = true;
            this.Cursor = Cursors.WaitCursor;

            textBox_Results.Clear();
            textBox_Results.Text += "NEURORIGHTER SPIKE SORTER - TRAINING STATS\r\n";
            textBox_Results.Text += "------------------------------------------\r\n";
            textBox_Results.Text += "PARAMETERS " + "\r\n";
            textBox_Results.Text += " Projection Method: " + spikeSorter.projectionType + "\r\n";
            textBox_Results.Text += " Projection Dimension: " + spikeSorter.projectionDimension + "\r\n";
            textBox_Results.Text += " Min. Spikes to Train: " + spikeSorter.minSpikes + "\r\n";
            textBox_Results.Text += " Max. Spikes to Train: " + spikeSorter.maxTrainingSpikesPerChannel + "\r\n";
            textBox_Results.Text += " Quantile for Outliers: " + (1.0 - spikeSorter.pValue) + "\r\n\r\n";

            textBox_Results.Text += "ACROSS CHANNELS " + "\r\n";
            textBox_Results.Text += " # channels to sort on: " + spikeSorter.channelsToSort.Count + " / " + numChannels.ToString() + "\r\n";
            textBox_Results.Text += " # of units identified: " + spikeSorter.totalNumberOfUnits.ToString() + "\r\n\r\n";


            for (int i = 0; i < numChannels; ++i)
            {
                List<ChannelModel> tmpCM = spikeSorter.channelModels.Where(x => x.channelNumber == i + 1).ToList();

                if (tmpCM.Count > 0)
                {
                    textBox_Results.Text += "CHANNEL " + ((short)tmpCM[0].channelNumber).ToString() + "\r\n";
                    textBox_Results.Text += " Number of training spikes: " + spikeSorter.spikesCollectedPerChannel[(short)(tmpCM[0].channelNumber + 1)].ToString() + " / " + spikeSorter.maxTrainingSpikesPerChannel.ToString() + "\r\n";
                    textBox_Results.Text += " Units Detected: " + tmpCM[0].K.ToString() + "\r\n";
                    textBox_Results.Text += " Clustering Results:\r\n";

                    for (int k = 0; k < spikeSorter.maxK; ++k)
                    {
                        textBox_Results.Text += "  K=" + tmpCM[0].kVals[k].ToString() + "\r\n";
                        if (tmpCM[0].kVals[k] == tmpCM[0].K)
                        {
                            textBox_Results.Text += "   **Log-likelihood= " + tmpCM[0].logLike[k].ToString() + "\r\n";
                            textBox_Results.Text += "   **Rissanen= " + tmpCM[0].mdl[k].ToString() + "\r\n";
                        }
                        else
                        {
                            textBox_Results.Text += "   Rissanen= " + tmpCM[0].mdl[k].ToString() + "\r\n";
                            textBox_Results.Text += "   Log-likelihood= " + tmpCM[0].logLike[k].ToString() + "\r\n";
                        }
                    }

                    textBox_Results.Text += "\r\n";
                }
                else
                {
                    textBox_Results.Text += "CHANNEL " + (i + 1).ToString() + "\r\n";
                    textBox_Results.Text += " No Sorting" + "\r\n\r\n";
                }

            }

            this.UseWaitCursor = false;
            this.Cursor = Cursors.Default;
        }

        private void ManageProjectionChange()
        {
            switch (spikeSorter.projectionType)
            {
                case "Maximum Voltage Inflection":
                    spikeSorter.projectionDimension = 1;
                    numericUpDown_ProjDim.Value = 1;
                    numericUpDown_ProjDim.Enabled = false;
                    break;
                case "Double Voltage Inflection":
                    spikeSorter.projectionDimension = 2;
                    numericUpDown_ProjDim.Value = 2;
                    numericUpDown_ProjDim.Enabled = false;
                    break;
                case "PCA":
                    spikeSorter.projectionDimension = (int)numericUpDown_ProjDim.Value;
                    numericUpDown_ProjDim.Enabled = true;
                    break;
            }
        }

        private void RefreshSpikeCollectionPlot()
        {
            // Create GraphPanes
            zgc.GraphPane.CurveList.Clear();
            GraphPane projPane = zgc.GraphPane;

            // Show data
            zgc.AxisChange();
            zgc.Invalidate();
        }

        private void UpdateSpikeCollectionPlot()
        {
            // Create GraphPanes
            zgc.GraphPane.CurveList.Clear();
            GraphPane numSpikesPane = zgc.GraphPane;
            List<LineItem> myCurves = new List<LineItem>();

            // Plot the spikes
            PointPairList ppl = new PointPairList();
            PointPairList pplThresh = new PointPairList();
            if (spikeSorter != null)
            {
                for (int i = 0; i < numChannels; ++i)
                {
                    lock (this)
                    {
                        ppl.Add((double)i + 1, Convert.ToDouble(spikeSorter.spikesCollectedPerChannel[(short)(i + 1)]));
                    }
                }
            }
            pplThresh.Add(0, (double)numericUpDown_MinSpikesToTrain.Value);
            pplThresh.Add(numChannels, (double)numericUpDown_MinSpikesToTrain.Value);

            // Add data tp plot
            myCurves.Add(numSpikesPane.AddCurve("numSpikes", ppl, Color.RoyalBlue, SymbolType.None));
            myCurves.Add(numSpikesPane.AddCurve("trainThresh", pplThresh, Color.Red, SymbolType.None));

            // Set plot parameteters
            SetMinorPlotParameters(ref numSpikesPane);

            // Show data
            zgc.AxisChange();
            zgc.Invalidate();

        }

        private void SetMinorPlotParameters(ref GraphPane gp)
        {
            // Text settings
            gp.Legend.IsVisible = false;
            gp.Title.IsVisible = false;
            gp.XAxis.Title.IsVisible = false;
            gp.YAxis.Title.IsVisible = false;

            // turn off the opposite tics so the Y tics don't show up on the Y2 axis
            gp.XAxis.IsVisible = false;
            gp.YAxis.IsVisible = false;

            // Display the Y zero line
            gp.YAxis.MajorGrid.IsZeroLine = false;

            // Align the Y axis labels so they are flush to the axis
            gp.YAxis.Scale.Align = AlignP.Inside;

            // Manually set the axis range
            gp.YAxis.Scale.Min = 0;
            if (spikeSorter != null)
                gp.YAxis.Scale.Max = spikeSorter.maxTrainingSpikesPerChannel;
            else
                gp.YAxis.Scale.Max = 200;
            gp.XAxis.Scale.Min = 1;
            gp.XAxis.Scale.Max = numChannels;

            // Fill the axis background with a gradient
            gp.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
        }

        #region Form Event Handlers
        private void button_ForceDetectTrain_Click(object sender, EventArgs e)
        {
            SettingsHaveChanged(this, e);
        }

        private void thresholdMultiplier_ValueChanged(object sender, EventArgs e)
        {
            spikeDetector.thresholdMultiplier = (double)thresholdMultiplier.Value;
            spikeDetectorRaw.thresholdMultiplier = (double)thresholdMultiplier.Value;
            spikeDetectorSalpa.thresholdMultiplier = (double)thresholdMultiplier.Value;
            detectorParameters.Threshold = thresholdMultiplier.Value;
            SettingsHaveChanged(this, e);
        }

        private void numPreSamples_ValueChanged(object sender, EventArgs e)
        {
            // Set min of numPost = numPre
            //numPostSamples.Minimum = ((double)numPreSamples.Value) / 1e6* Convert.ToDouble(sampleRate);

            numPre = (int)((double)numPreSamples.Value / 1e6 * Convert.ToDouble(Properties.Settings.Default.RawSampleFrequency));

            if (numPre == 0)
                numPre = 1;

            detectorParameters.NumPre = numPre;

            SettingsHaveChanged(this, e);

            if (spikeSorter != null)
                spikeSorter.inflectionSample = numPre;

            // Update label
            label_PreSampConv.Text =
                numPre + " samples";
        }

        private void numPostSamples_ValueChanged(object sender, EventArgs e)
        {
            numPost = (int)((double)numPostSamples.Value / 1e6 * Convert.ToDouble(Properties.Settings.Default.RawSampleFrequency));

            if (numPost == 0)
                numPost = 1;

            detectorParameters.NumPost = numPost;

            SettingsHaveChanged(this, e);

            // Update label
            label_PostSampConv.Text =
                numPost + " samples";
        }

        private void numericUpDown_DeadTime_ValueChanged(object sender, EventArgs e)
        {
            detectorParameters.DeadTime = numericUpDown_DeadTime.Value;
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MinSpikeWidth_ValueChanged(object sender, EventArgs e)
        {
            detectorParameters.MinSpikeWidth = numericUpDown_MinSpikeWidth.Value;
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MaxSpikeWidth_ValueChanged(object sender, EventArgs e)
        {
            detectorParameters.MaxSpikeWidth = numericUpDown_MaxSpikeWidth.Value;
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MaxSpkAmp_ValueChanged(object sender, EventArgs e)
        {
            detectorParameters.MaxSpikeAmp = numericUpDown_MaxSpkAmp.Value;
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MinSpikeSlope_ValueChanged(object sender, EventArgs e)
        {
            detectorParameters.MinSpikeSlope = numericUpDown_MinSpikeSlope.Value;
            SettingsHaveChanged(this, e);
        }

        private void button_SaveAndClose_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void comboBox_spikeDetAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SettingsHaveChanged != null)
            {
                detectorParameters.DetectorType = comboBox_spikeDetAlg.SelectedIndex;
                SettingsHaveChanged(this, e);
            }
        }

        private void comboBox_noiseEstAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SettingsHaveChanged != null)
            {
                detectorParameters.NoiseAlgType = comboBox_noiseEstAlg.SelectedIndex;
                SettingsHaveChanged(this, e);
            }
        }

        private void numericUpDown_maxK_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
            {
                spikeSorter.maxK = (int)(numericUpDown_maxK.Value);
                detectorParameters.SS = spikeSorter;
            }
        }

        private void numericUpDown_MinClassificationProb_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
            {
                spikeSorter.pValue = (double)(numericUpDown_MinClassificationProb.Value);
                detectorParameters.SS = spikeSorter;
            }
        }

        private void numericUpDown_MinSpikesToTrain_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
            {
                spikeSorter.minSpikes = (int)(numericUpDown_MinSpikesToTrain.Value);
                detectorParameters.SS = spikeSorter;
            }
            UpdateSpikeCollectionPlot();
        }

        private void button_HoardSpikes_Click(object sender, EventArgs e)
        {
            if (!isHoarding)
            {

                // Make sure they want to kill the current sorter
                if (spikeSorter != null)
                {
                    if (MessageBox.Show("Do you want to overwrite the current spike sorter?", "Overwrite?", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }

                // Update the UI to reflect the state of things
                Flush();

                // Reset the sorter completely
                isTrained = false;
                isEngaged = false;
                hasData = false;
                isHoarding = true;
                spikeSorter = null;

                // Create the appropriate sorter
                switch (comboBox_ProjectionType.SelectedItem.ToString())
                {
                    case "Maximum Voltage Inflection":
                        {
                            spikeSorter = new SpikeSorter(
                            numChannels,
                            (int)numericUpDown_maxK.Value,
                            (int)numericUpDown_MinSpikesToTrain.Value,
                            (double)numericUpDown_MinClassificationProb.Value,
                            comboBox_ProjectionType.SelectedItem.ToString());
                            break;
                        }
                    case "Double Voltage Inflection":
                        {
                            spikeSorter = new SpikeSorter(
                            numChannels,
                            (int)numericUpDown_maxK.Value,
                            (int)numericUpDown_MinSpikesToTrain.Value,
                            (double)numericUpDown_MinClassificationProb.Value,
                            comboBox_ProjectionType.SelectedItem.ToString());
                            break;
                        }
                    case "PCA":
                        {
                            spikeSorter = new SpikeSorter(
                            numChannels,
                            (int)numericUpDown_maxK.Value,
                            (int)numericUpDown_MinSpikesToTrain.Value,
                            (double)numericUpDown_MinClassificationProb.Value,
                            (int)numericUpDown_ProjDim.Value,
                            comboBox_ProjectionType.SelectedItem.ToString());
                            break;
                        }
                }

                // Update hoard button
                button_HoardSpikes.Text = "Stop";
            }
            else
            {
                isHoarding = false;
                hasData = true;
                button_TrainSorter.Enabled = true;
                button_HoardSpikes.Text = "Hoard";
            }

            // Update wrapper
            detectorParameters.SS = spikeSorter;
        }

        private void button_TrainSorter_Click(object sender, EventArgs e)
        {
            // Train the sorter on a separate thread
            button_TrainSorter.Enabled = false;
            button_EngageSpikeSorter.Enabled = false;
            sorterTrainer.RunWorkerAsync();
        }

        private void sorterTrainer_trainSS(object sender, DoWorkEventArgs e)
        {
            // Actual training method
            if (spikeSorter.projectionType == "Maximum Voltage Inflection")
                spikeSorter.Train(numPre);
            else if (spikeSorter.projectionType == "PCA")
                spikeSorter.Train();
            else if (spikeSorter.projectionType == "Double Voltage Inflection")
                spikeSorter.Train(numPre, 0.4, (int)Properties.Settings.Default.RawSampleFrequency);

        }

        private void sorterTrainer_DoneTraining(object sender, RunWorkerCompletedEventArgs e)
        {
            // Tell the user the the sorter is trained
            label_Trained.Text = "Spike sorter is trained.";
            label_Trained.ForeColor = Color.Green;

            // Enable Saving and sorting
            isTrained = true;
            detectorParameters.SS = spikeSorter;
            button_EngageSpikeSorter.Enabled = true;
            button_TrainSorter.Enabled = true;

            // Print detector stats to textbox
            ReportTrainingResults();

        }

        private void button_EngageSpikeSorter_Click(object sender, EventArgs e)
        {
            if (!isTrained)
            {
                MessageBox.Show("Please train the spike sorter first.");
                return;
            }

            if (isEngaged)
            {
                isEngaged = false;
                button_EngageSpikeSorter.Text = "Engage Spike Sorter";
                label_SorterEngaged.Text = "Sorter is not engaged";
                label_SorterEngaged.ForeColor = Color.Red;
                comboBox_ProjectionType.Enabled = true;
                button_HoardSpikes.Enabled = true;
                button_TrainSorter.Enabled = true;
                numericUpDown_ProjDim.Enabled = true;
                numericUpDown_MinClassificationProb.Enabled = true;
                numericUpDown_maxK.Enabled = true;
                numericUpDown_MinSpikesToTrain.Enabled = true;

            }
            else if (!isEngaged)
            {
                isEngaged = true;
                button_EngageSpikeSorter.Text = "Disengage Spike Sorter";
                label_SorterEngaged.Text = "Sorter is engaged";
                label_SorterEngaged.ForeColor = Color.Green;
                comboBox_ProjectionType.Enabled = false;
                button_HoardSpikes.Enabled = false;
                button_TrainSorter.Enabled = false;
                numericUpDown_ProjDim.Enabled = false;
                numericUpDown_MinClassificationProb.Enabled = false;
                numericUpDown_maxK.Enabled = false;
                numericUpDown_MinSpikesToTrain.Enabled = false;
            }
        }

        private void numericUpDown_ProjDim_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
            {
                spikeSorter.projectionDimension = (int)numericUpDown_ProjDim.Value;
                detectorParameters.SS = spikeSorter;
            }
        }

        private void comboBox_ProjectionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
            {
                spikeSorter.projectionType = comboBox_ProjectionType.SelectedItem.ToString();
                ManageProjectionChange();
            }
            detectorParameters.SS = spikeSorter;
        }

        private void saveSpikeFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveSDDialog = new SaveFileDialog();
            saveSDDialog.DefaultExt = "*.nrsd";
            saveSDDialog.Filter = "NeuroRighter Spike Detector|*.nrsd";
            saveSDDialog.Title = "Save NeuroRighter Spike Detector";

            if (saveSDDialog.ShowDialog() == DialogResult.OK && saveSDDialog.FileName != "")
            {
                try
                {
                    SpikeDetectorSerializer spikeDetSerializer = new SpikeDetectorSerializer();
                    spikeDetSerializer.SerializeObject(saveSDDialog.FileName, detectorParameters);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not write file to disk. Original error: " + ex.Message);
                }
            }
        }

        private void loadSpikeFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Make sure they want to kill the current sorter
            if (spikeSorter != null)
            {
                if (MessageBox.Show("This will overwrite the current spike sorter. Do you want to continue?", "Overwrite?", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            // Disengage any current sorter
            isEngaged = false;
            button_EngageSpikeSorter.Text = "Engage Spike Sorter";
            label_SorterEngaged.Text = "Sorter is not engaged";
            label_SorterEngaged.ForeColor = Color.Red;

            // Deserialize your saved sorter
            OpenFileDialog openSDDialog = new OpenFileDialog();
            openSDDialog.DefaultExt = "*.nrsd";
            openSDDialog.Filter = "NeuroRighter Spike Detectpr|*.nrsd";
            openSDDialog.Title = "Load NeuroRighter Spike Detector";
            openSDDialog.InitialDirectory = Properties.Settings.Default.spikeSorterDir;

            if (openSDDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SpikeDetectorSerializer spikeDetSerializer = new SpikeDetectorSerializer();
                    detectorParameters = spikeDetSerializer.DeSerializeObject(openSDDialog.FileName);
                    Properties.Settings.Default.spikeSorterDir = new FileInfo(openSDDialog.FileName).DirectoryName;

                    // Detector parameters
                    comboBox_noiseEstAlg.SelectedIndex = detectorParameters.NoiseAlgType;
                    comboBox_spikeDetAlg.SelectedIndex = detectorParameters.DetectorType;
                    thresholdMultiplier.Value = detectorParameters.Threshold;
                    numericUpDown_DeadTime.Value = detectorParameters.DeadTime;
                    numericUpDown_MaxSpkAmp.Value = detectorParameters.MaxSpikeAmp;
                    numericUpDown_MinSpikeSlope.Value = detectorParameters.MinSpikeSlope;
                    numericUpDown_MinSpikeWidth.Value = detectorParameters.MinSpikeWidth;
                    numericUpDown_MaxSpikeWidth.Value = detectorParameters.MaxSpikeWidth;
                    numPreSamples.Value = detectorParameters.NumPre;
                    numPostSamples.Value = detectorParameters.NumPost;

                    if (detectorParameters.SS != null)
                    {
                        // Deserialize the sorter
                        spikeSorter = detectorParameters.SS;

                        // Update the number of spikes you have trained with
                        UpdateCollectionBar();

                        // Sorter Parameters
                        switch (spikeSorter.projectionType)
                        {
                            case "Maximum Voltage Inflection":
                                comboBox_ProjectionType.SelectedIndex = 0;
                                break;
                            case "Double Voltage Inflection":
                                comboBox_ProjectionType.SelectedIndex = 1;
                                break;
                            case "PCA":
                                comboBox_ProjectionType.SelectedIndex = 2;
                                break;
                        }
                        numericUpDown_ProjDim.Value = (decimal)spikeSorter.projectionDimension;
                        numericUpDown_MinClassificationProb.Value = (decimal)spikeSorter.pValue;
                        numericUpDown_MinSpikesToTrain.Value = (decimal)spikeSorter.minSpikes;
                        numericUpDown_maxK.Value = (decimal)spikeSorter.maxK;

                        // Update UI to reflect the state of things
                        if (spikeSorter.trained)
                        {
                            // Tell the user the the sorter is trained
                            label_Trained.Text = "Spike sorter is trained.";
                            label_Trained.ForeColor = Color.Green;
                            button_EngageSpikeSorter.Enabled = true;
                            button_TrainSorter.Enabled = true;
                            isTrained = true;
                            ReportTrainingResults();
                        }
                        else
                        {
                            label_Trained.Text = "Spike sorter is not trained.";
                            label_Trained.ForeColor = Color.Red;
                            button_EngageSpikeSorter.Enabled = false;
                            button_TrainSorter.Enabled = true;
                            isTrained = false;
                        }

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        #endregion

        # region public accessors

        public int NumPre
        {
            get
            {
                return numPre;
            }
        }

        public int NumPost
        {
            get
            {
                return numPost;
            }
        }

        public bool IsEngaged
        {
            get
            {
                return isEngaged;
            }
        }

        public bool IsHoarding
        {
            get
            {
                return isHoarding;
            }
        }

        public bool IsTrained
        {
            get
            {
                return isTrained;
            }
        }

        # endregion
    }
}
