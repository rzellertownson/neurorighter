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

namespace NeuroRighter.SpikeDetection
{

    sealed internal partial class SpikeDetSettings : Form
    {

        // Parameters passed in from NR interface
        private int sampleRate;
        private int spikeBufferLength;
        private int numChannels;

        // Detector
        internal SpikeDetector spikeDetector;
        internal int numPre; // num smaples to save pre-spike
        internal int numPost; // num samples to save post-spike
        internal int spikeDetectionLag; // number of samples that spike detector will cause buffers to lag
        internal int detectorType = 0;

        // Spike sorter
        internal SpikeSorter spikeSorter;
        internal bool isHoarding = false;
        internal bool isTrained = false;
        internal bool isEngaged = false;
        internal bool hasData;
        private BackgroundWorker sorterTrainer;

        // Delegates for informing mainform of settings change
        internal delegate void resetSpkDetSettingsHandler(object sender, EventArgs e);
        internal event resetSpkDetSettingsHandler SettingsHaveChanged;
        private delegate void SetTextCallback();

        public SpikeDetSettings(int spikeBufferLength, int numChannels, int sampleRate)
        {
            this.spikeBufferLength = spikeBufferLength;
            this.numChannels = numChannels;
            //this.Properties.Settings.Default.ADCPollingPeriodSec = Properties.Settings.Default.ADCPollingPeriodSec;
            this.sampleRate = sampleRate;

            InitializeComponent();

            //Default spike det. algorithm is fixed RMS
            this.comboBox_noiseEstAlg.SelectedIndex = 0;
            this.comboBox_spikeDetAlg.SelectedIndex = 0;
            this.numPre = (int)numPreSamples.Value;
            this.numPost = (int)numPostSamples.Value;

            // Set the pre/post sample coversion label
            label_PreSampConv.Text =
                1000 * ((double)numPre / Convert.ToDouble(sampleRate)) + " msec";
            label_PostSampConv.Text =
                1000 * ((double)numPost / Convert.ToDouble(sampleRate)) + " msec";

            // Set min of numPost = numPre
            numPostSamples.Minimum = numPreSamples.Value;

            // Set up the spike sorter's BW
            sorterTrainer = new BackgroundWorker();
            sorterTrainer.DoWork +=
                new DoWorkEventHandler(sorterTrainer_trainSS);
            sorterTrainer.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(sorterTrainer_DoneTraining);

            // Flush Component
            //spikeSorter = new SpikeSorter(
            //        numChannels,
            //        Convert.ToInt32(numericUpDown_maxK.Value),
            //        Convert.ToInt32(numericUpDown_MinSpikesToTrain.Value));
            comboBox_ProjectionType.SelectedIndex = 0;
            Flush();
        }

        internal void SetSpikeDetector()
        {
            int detectionDeadTime = (int)Math.Round(Convert.ToDouble(sampleRate)*
                (double)numericUpDown_DeadTime.Value/1.0e6);
            int minSpikeWidth = (int)Math.Floor(Convert.ToDouble(sampleRate) *
                (double)numericUpDown_MinSpikeWidth.Value / 1.0e6);
            int maxSpikeWidth = (int)Math.Round(Convert.ToDouble(sampleRate) *
                (double)numericUpDown_MaxSpikeWidth.Value / 1.0e6);
            double maxSpikeAmp = (double)numericUpDown_MaxSpkAmp.Value / 1.0e6;
            double minSpikeSlope = (double)numericUpDown_MinSpikeSlope.Value / 1.0e6;

            // Repopulate conversion table
            label_deadTimeSamp.Text = detectionDeadTime + " sample(s)";
            label_MinWidthSamp.Text = minSpikeWidth + " sample(s)";
            label_MaxWidthSamp.Text = maxSpikeWidth + " sample(s)";

            // Half a millisecond to determine spike polarity
            int spikeIntegrationTime = (int)Math.Ceiling(Convert.ToDouble(sampleRate)/1000);

            switch (comboBox_noiseEstAlg.SelectedIndex)
            {
                case 0:  //RMS Fixed
                    spikeDetector = new RMSThresholdFixed(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value),detectionDeadTime,minSpikeWidth,maxSpikeWidth,
                        maxSpikeAmp, minSpikeSlope, spikeIntegrationTime, Properties.Settings.Default.ADCPollingPeriodSec);
                    break;
                case 1:  //RMS Adaptive
                    spikeDetector = new AdaptiveRMSThreshold(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime, minSpikeWidth, maxSpikeWidth,
                        maxSpikeAmp, minSpikeSlope, spikeIntegrationTime, Properties.Settings.Default.ADCPollingPeriodSec);
                    break;
                case 2:  //Limada
                    spikeDetector = new LimAda(spikeBufferLength, numChannels, 2, numPre + numPost + 1, numPost,
                        numPre, (rawType)Convert.ToDouble(thresholdMultiplier.Value), detectionDeadTime,minSpikeWidth,maxSpikeWidth,
                        maxSpikeWidth, minSpikeSlope, spikeIntegrationTime, Convert.ToInt32(sampleRate));
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

            spikeDetectionLag = spikeDetector.carryOverLength;
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
                    int spikesCollected = spikeSorter.trainingSpikes.eventBuffer.Count;
                    this.label_NumSpikesCollected.Text = spikesCollected.ToString();
                }
            }
        }

        private void button_ForceDetectTrain_Click(object sender, EventArgs e)
        {
            SettingsHaveChanged(this, e);
        }

        private void thresholdMultiplier_ValueChanged(object sender, EventArgs e)
        {
            spikeDetector.thresholdMultiplier = (double)thresholdMultiplier.Value;
            SettingsHaveChanged(this, e);
        }

        private void numPreSamples_ValueChanged(object sender, EventArgs e)
        {
            // Set min of numPost = numPre
            numPostSamples.Minimum = numPreSamples.Value;

            SettingsHaveChanged(this, e);
            numPre = (int)numPreSamples.Value;
            numPost = (int)numPostSamples.Value;

            // Update label
            label_PreSampConv.Text =
                1000.0 * (double)numPreSamples.Value / Convert.ToDouble(sampleRate) + " msec";
        }

        private void numPostSamples_ValueChanged(object sender, EventArgs e)
        {
            numPre = (int)numPreSamples.Value;
            numPost = (int)numPostSamples.Value;

            SettingsHaveChanged(this, e);

            // Update label
            label_PostSampConv.Text =
                1000.0 * (double)numPostSamples.Value / Convert.ToDouble(sampleRate) + " msec";
        }

        private void numericUpDown_DeadTime_ValueChanged(object sender, EventArgs e)
        {
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MinSpikeWidth_ValueChanged(object sender, EventArgs e)
        {
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MaxSpikeWidth_ValueChanged(object sender, EventArgs e)
        {
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MaxSpkAmp_ValueChanged(object sender, EventArgs e)
        {
            SettingsHaveChanged(this, e);
        }

        private void numericUpDown_MinSpikeSlope_ValueChanged(object sender, EventArgs e)
        {
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
                SettingsHaveChanged(this, e);
            }
        }

        private void comboBox_noiseEstAlg_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SettingsHaveChanged !=null)
            {
                SettingsHaveChanged(this, e);
            }
        }

        private void numericUpDown_maxK_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
                spikeSorter.maxK = (int)(numericUpDown_maxK.Value);
        }

        private void numericUpDown_MinSpikesToTrain_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
                spikeSorter.minSpikes = (int)(numericUpDown_MinSpikesToTrain.Value);
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
                if (comboBox_ProjectionType.SelectedItem.ToString() == "Maximum Voltage Inflection")
                {
                    spikeSorter = new SpikeSorter(
                    numChannels,
                    (int)numericUpDown_maxK.Value,
                    (int)numericUpDown_MinSpikesToTrain.Value,
                    (int)numericUpDown_ProjDim.Value);
                    spikeSorter.projectionType = comboBox_ProjectionType.SelectedItem.ToString();
                }
                else if (comboBox_ProjectionType.SelectedItem.ToString() == "PCA")
                {
                    spikeSorter = new SpikeSorter(
                    numChannels,
                    (int)numericUpDown_maxK.Value,
                    (int)numericUpDown_MinSpikesToTrain.Value,
                    (int)numericUpDown_ProjDim.Value);
                    spikeSorter.projectionType = comboBox_ProjectionType.SelectedItem.ToString();
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
                button_SaveSpikeSorter.Enabled = true;
            }
        }

        private void button_TrainSorter_Click(object sender, EventArgs e)
        {
            // Train the sorter on a separate thread
            sorterTrainer.RunWorkerAsync();
        }

        private void sorterTrainer_trainSS(object sender, DoWorkEventArgs e)
        {
            // Actual training method
            if (spikeSorter.projectionType == "Maximum Voltage Inflection")
                spikeSorter.Train(numPre);
            else if (spikeSorter.projectionType == "PCA")
                spikeSorter.Train();
        }

        private void sorterTrainer_DoneTraining(object sender, RunWorkerCompletedEventArgs e)
        {
            // Tell the user the the sorter is trained
            label_Trained.Text = "Spike sorter is trained.";
            label_Trained.ForeColor = Color.Green;
            // Enable Saving and sorting
            isTrained = true;
            button_EngageSpikeSorter.Enabled = true;

            // Print detector stats to textbox
            ReportTrainingResults();

        }

        private void button_SaveSpikeSorter_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveSSDialog = new SaveFileDialog();
            saveSSDialog.DefaultExt = "*.nrss";
            saveSSDialog.Filter = "NeuroRighter Spike Sorter|*.nrss";
            saveSSDialog.Title = "Save NeuroRighter Spike Sorter";

            if (saveSSDialog.ShowDialog() == DialogResult.OK && saveSSDialog.FileName != "")
            {
                try
                {
                    SorterSerializer spikeSorterSerializer = new SorterSerializer();
                    spikeSorterSerializer.SerializeObject(saveSSDialog.FileName, spikeSorter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not write file to disk. Original error: " + ex.Message);
                }
            }
        }

        private void button_LoadSpikeSorter_Click(object sender, EventArgs e)
        {
            // Make sure they want to kill the current sorter
            if (spikeSorter != null)
            {
                if (MessageBox.Show("Do you want to overwrite the current spike sorter?", "Overwrite?", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            // Disengage any current sorter
            isEngaged = false;
            label_SorterEngaged.Text = "Sorter is not engaged";
            label_SorterEngaged.ForeColor = Color.Red;

            // Deserialize your saved sorter
            OpenFileDialog openSSDialog = new OpenFileDialog();
            openSSDialog.DefaultExt = "*.nrss";
            openSSDialog.Filter = "NeuroRighter Spike Sorter|*.nrss";
            openSSDialog.Title = "Load NeuroRighter Spike Sorter";
            openSSDialog.InitialDirectory = Properties.Settings.Default.spikeSorterDir;

            if (openSSDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    textBox_SorterLocation.Text = openSSDialog.FileName;
                    SorterSerializer spikeSorterSerializer = new SorterSerializer();
                    spikeSorter = spikeSorterSerializer.DeSerializeObject(openSSDialog.FileName);
                    Properties.Settings.Default.spikeSorterDir = new FileInfo(openSSDialog.FileName).DirectoryName;

                    // Update the number of spikes you have trained with
                    UpdateCollectionBar();

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
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
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
                button_LoadSpikeSorter.Enabled = true;
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
                button_LoadSpikeSorter.Enabled = false;
            }
        }

        private void ReportTrainingResults()
        {
            this.UseWaitCursor = true;
            this.Cursor = Cursors.WaitCursor;

            textBox_Results.Clear();
            textBox_Results.Text += "NEURORIGHTER SPIKE SORTER - TRAINING STATS\r\n";
            textBox_Results.Text += "------------------------------------------\r\n";
            textBox_Results.Text += "Projection Method: " + spikeSorter.projectionType + "\r\n";
            textBox_Results.Text += "# channels to sort on: " + spikeSorter.channelsToSort.Count + " / " + numChannels.ToString() + "\r\n";
            textBox_Results.Text += "# of units identified: " + spikeSorter.totalNumberOfUnits.ToString() + "\r\n\r\n";


            for (int i = 0; i < numChannels; ++i)
            {
                List<ChannelModel> tmpCM = spikeSorter.channelModels.Where(x => x.channelNumber == i).ToList();

                if (tmpCM.Count > 0)
                {
                    textBox_Results.Text += "CHANNEL " + (tmpCM[0].channelNumber + 1).ToString() + "\r\n";
                    textBox_Results.Text += " Number of training spikes: " + spikeSorter.spikesCollectedPerChannel[tmpCM[0].channelNumber + 1].ToString() + " / "  + spikeSorter.maxTrainingSpikesPerChannel.ToString() + "\r\n";
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

        private void Flush()
        {
            // Update the UI to reflect the state of things
            button_TrainSorter.Enabled = false;
            button_SaveSpikeSorter.Enabled = false;
            button_EngageSpikeSorter.Enabled = false;
            label_SorterEngaged.Text = "Sorter is not engaged";
            label_SorterEngaged.ForeColor = Color.Red;
            label_Trained.Text = "Spike sorter is not trained.";
            label_Trained.ForeColor = Color.Red;
            button_HoardSpikes.Text = "Hoard";
            button_SaveSpikeSorter.Enabled = false;
            UpdateCollectionBar();
        }

        private void numericUpDown_ProjDim_ValueChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
                spikeSorter.projectionDimension = (int)numericUpDown_ProjDim.Value;
        }

        private void comboBox_ProjectionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (spikeSorter != null)
                spikeSorter.projectionType = comboBox_ProjectionType.SelectedItem.ToString();
        }

    }
}
