using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter
{
    /// <summary>
    /// This form allows the user to enter information about the current recording as ascii text. 
    /// When the information is logged it is stored as a *.log file with the same basefile name as the 
    /// current recording. Each time a note is entered, it is prefaced by the time-stamp at which the note
    /// form was opened.
    /// </summary>
    /// 
    public partial class RecordingNote : Form
    {
        

        string TS;
        string note_fid;
        string fid;
        TextWriter NoteWriter;
        StreamWriter file;

        /// <summary>
        /// Creates a RecordingNote window for the user to enter/save a note with.
        /// </summary>
        /// <param name="namebase">filename prefix for the timestamped file to generate using this RecordingNote</param>
        public RecordingNote(string namebase)
        {
            InitializeComponent();
            DateTime nowDate = DateTime.Now;//Get current time (local to computer);
            string datePrefix = nowDate.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss");
            TS = datePrefix;
            label_TimeStamp.Text += datePrefix;
            fid = namebase;
            note_fid = namebase + ".log";
        }

        private void button_EnterNote_Click(object sender, EventArgs e)
        {
            // Check if this base filename already has a .log file associated with it
            if (File.Exists(note_fid))
            {
                if (textBox_Note.Text != null)
                {
                    file = new System.IO.StreamWriter(note_fid, true);
                    file.WriteLine("Time Stamp: [ " + TS + " ]");
                    file.WriteLine(textBox_Note.Text);
                    file.WriteLine("\r\n");
                    file.Close();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Please enter a note in the text box");
                    return;
                }
                

            }
            else
            {
                NoteWriter = new StreamWriter(note_fid);
                NoteWriter.WriteLine("NEURORIGHTER LOG FILE");
                NoteWriter.WriteLine("Created: " + DateTime.Now);
                NoteWriter.WriteLine("Referenced to recording: " + fid);
                NoteWriter.WriteLine("\r\n");
                NoteWriter.Close();
                button_EnterNote.PerformClick();
            }
        }

        private void button_CancelNote_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button_SettingSnapshot_Click(object sender, EventArgs e)
        {   
            // General
            textBox_Note.Text += "** General Settings ** \r\n";
            textBox_Note.Text += "Digital Gain: " + Properties.Settings.Default.Gain + "X \r\n";
            textBox_Note.Text += "ADC Polling Period (sec): " + Properties.Settings.Default.ADCPollingPeriodSec + "\r\n"; ;
            if (Properties.Settings.Default.UseBuffload)
            {
                textBox_Note.Text += "DAC Polling Period (sec): " + Properties.Settings.Default.DACPollingPeriodSec + "\r\n"; ;
            }
            else
            {
                textBox_Note.Text += "DAC Polling Period (sec): No output buffering in use.\r\n";
            }

            // For each recording type that is currently being used, write down the relavant parameters

            // Raw voltages
            textBox_Note.Text += "\r\n";
            textBox_Note.Text += "** Raw Voltage Input ** \r\n";
            textBox_Note.Text += "No. Channels: " + Properties.Settings.Default.DefaultNumChannels + "\r\n";
            textBox_Note.Text += "Samp. Freq (Hz): " + Properties.Settings.Default.RawSampleFrequency + "\r\n";
            textBox_Note.Text += "Amplifier Gain: " + Properties.Settings.Default.PreAmpGain + "\r\n";

            // Spike band filtering
            textBox_Note.Text += "\r\n";
            textBox_Note.Text += "** Spike-Pass Filter ** \r\n";
            textBox_Note.Text += "No. Channels: " + Properties.Settings.Default.DefaultNumChannels + "\r\n";
            textBox_Note.Text += "Samp. Freq (Hz): " + Properties.Settings.Default.RawSampleFrequency + "\r\n";
            textBox_Note.Text += "Low Freq. Cut (Hz): " + Properties.Settings.Default.SpikesLowCut + "\r\n";
            textBox_Note.Text += "High Freq. Cut (Hz): " + Properties.Settings.Default.SpikesHighCut + "\r\n";
            textBox_Note.Text += "Filter Order (Hz): " + Properties.Settings.Default.SpikesNumPoles + "\r\n";
            textBox_Note.Text += "Filter Type (Hz): Butterworth" + Properties.Settings.Default.RawSampleFrequency + "\r\n";


            // Current Recording Streams
            textBox_Note.Text += "\r\n";
            // Full FS streams
            textBox_Note.Text += "** Selected Recording Streams ** \r\n";
            textBox_Note.Text += "Raw Electrode Voltages: " + Properties.Settings.Default.recordRaw + "\r\n";
            textBox_Note.Text += "Spike-band Filter: " + Properties.Settings.Default.recordSpikeFilt + "\r\n";
            textBox_Note.Text += "SALPA Filter: " + Properties.Settings.Default.recordSalpa + "\r\n";

            // Spikes
            textBox_Note.Text += "Raw Electrode Voltages: " + Properties.Settings.Default.recordRaw + "\r\n";
            textBox_Note.Text += "Spike-band Filter: " + Properties.Settings.Default.recordSpikeFilt + "\r\n";
            textBox_Note.Text += "SALPA Filter: " + Properties.Settings.Default.recordSalpa + "\r\n";

            // LFP

            // EEG

            // MUA

            // Aux signals

        }


    }
}
