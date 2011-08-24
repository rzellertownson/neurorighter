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

namespace NeuroRighter.SpikeDetection
{

    sealed internal partial class SpikeDetSettings : Form
    {

        // Parameters passed in from NR interface
        private int sampleRate;
        private int spikeBufferLength;
        private int numChannels;
        //private double Properties.Settings.Default.ADCPollingPeriodSec;

        internal SpikeDetector spikeDetector;
        internal int numPre; // num smaples to save pre-spike
        internal int numPost; // num samples to save post-spike
        internal int spikeDetectionLag; // number of samples that spike detector will cause buffers to lag

        // Delegates for informing mainform of settings change
        internal delegate void resetSpkDetSettingsHandler(object sender, EventArgs e);
        internal event resetSpkDetSettingsHandler SettingsHaveChanged;

        public SpikeDetSettings(int spikeBufferLength, int numChannels, int sampleRate)
        {
            this.spikeBufferLength = spikeBufferLength;
            this.numChannels = numChannels;
            //this.Properties.Settings.Default.ADCPollingPeriodSec = Properties.Settings.Default.ADCPollingPeriodSec;
            this.sampleRate = sampleRate;

            InitializeComponent();

            //Default spike det. algorithm is fixed RMS
            this.comboBox_spikeDetAlg.SelectedIndex = 0;
            this.numPre = (int)numPreSamples.Value;
            this.numPost = (int)numPostSamples.Value;

            // Set the pre/post sample coversion label
            label_PreSampConv.Text =
                Convert.ToDouble(sampleRate) * (double)numPre / 1000.0 + " msec";
            label_PostSampConv.Text =
                Convert.ToDouble(sampleRate) * (double)numPost / 1000.0 + " msec";

            // Set min of numPost = numPre
            numPostSamples.Minimum = numPreSamples.Value;
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

            switch (comboBox_spikeDetAlg.SelectedIndex)
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

            spikeDetectionLag = spikeDetector.carryOverLength;
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
                Convert.ToDouble(sampleRate) * (double)numPreSamples.Value / 1000.0 + " msec";
        }

        private void numPostSamples_ValueChanged(object sender, EventArgs e)
        {
            numPre = (int)numPreSamples.Value;
            numPost = (int)numPostSamples.Value;

            SettingsHaveChanged(this, e);

            // Update label
            label_PostSampConv.Text =
                Convert.ToDouble(sampleRate) * (double)numPostSamples.Value / 1000.0 + " msec";
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




    }
}
