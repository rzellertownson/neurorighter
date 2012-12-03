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
                    Console.WriteLine("Logging note...");
                    file = new System.IO.StreamWriter(note_fid, true);
                    file.WriteLine("Time Stamp: [ " + TS + " ]");
                    file.WriteLine(textBox_Note.Text);
                    file.WriteLine("\r\n");
                    file.Close();
                    this.Close();
                    Console.WriteLine("Note logged.");
                }
                else
                {
                    MessageBox.Show("Please enter a note in the text box");
                    return;
                }
                

            }
            else
            {
                Console.WriteLine("Creating log file...");
                NoteWriter = new StreamWriter(note_fid);
                NoteWriter.WriteLine("NEURORIGHTER LOG FILE");
                NoteWriter.WriteLine("Created: " + DateTime.Now);
                NoteWriter.WriteLine("Referenced to recording: " + fid);
                NoteWriter.WriteLine("\r\n");
                NoteWriter.Close();
                button_EnterNote.PerformClick();
                Console.WriteLine("Log file created.");
            }
        }

        private void button_CancelNote_Click(object sender, EventArgs e)
        {
            this.Close();
            Console.WriteLine("Note discarded.");
        }

        private void button_SettingSnapshot_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Taking snapshot of current NeuroRighter settings..");

            textBox_Note.Text += "\r\n";
            textBox_Note.Text += "== CURRENT SETTINGS ==" + "\r\n";

            // General
            textBox_Note.Text += "\r\n";
            textBox_Note.Text += "** General Settings ** \r\n";
            textBox_Note.Text += "  ADC Polling Period (sec):       " + Properties.Settings.Default.ADCPollingPeriodSec + "\r\n"; ;
            if (Properties.Settings.Default.UseBuffload)
            {
                textBox_Note.Text += "  DAC Polling Period (sec):       " + Properties.Settings.Default.DACPollingPeriodSec + "\r\n"; ;
            }
            else
            {
                textBox_Note.Text += "  DAC Polling Period (sec):       No output buffering in use.\r\n";
            }

            // For each recording type that is currently being used, write down the relavant parameters

            // Raw input settings
            textBox_Note.Text += "\r\n";
            textBox_Note.Text += "** Raw Voltage Input ** \r\n";
            textBox_Note.Text += "  No. Channels: " + Properties.Settings.Default.NumChannels + "\r\n";
            textBox_Note.Text += "  Samp. Freq (Hz):                " + Properties.Settings.Default.RawSampleFrequency + "\r\n";
            textBox_Note.Text += "  Amplifier Gain:                 " + Properties.Settings.Default.PreAmpGain + "\r\n";
            textBox_Note.Text += "  A/D Gain:                       " + Properties.Settings.Default.A2Dgain + "X \r\n";

            // Spike band settings
            if (Properties.Settings.Default.UseSpikeBandFilter)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Spike-Pass Filter ** \r\n";
                textBox_Note.Text += "  No. Channels:                   " + Properties.Settings.Default.NumChannels + "\r\n";
                textBox_Note.Text += "  Samp. Freq (Hz):                " + Properties.Settings.Default.RawSampleFrequency + "\r\n";
                textBox_Note.Text += "  Low Freq. Cut (Hz):             " + Properties.Settings.Default.SpikesLowCut + "\r\n";
                textBox_Note.Text += "  High Freq. Cut (Hz):            " + Properties.Settings.Default.SpikesHighCut + "\r\n";
                textBox_Note.Text += "  Filter Order:                   " + Properties.Settings.Default.SpikesNumPoles + "\r\n";
                textBox_Note.Text += "  Filter Type:                    Butterworth \r\n";
            }

            // LFP settings
            if (Properties.Settings.Default.UseLFPs)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Local Field Potentials ** \r\n";
                if (Properties.Settings.Default.SeparateLFPBoard)
                {
                    textBox_Note.Text += "  LFP Recording Type:             Dedicated LFP Board \r\n";
                    textBox_Note.Text += "  No. Channels:                   32 \r\n";
                    textBox_Note.Text += "  A/D Gain:                       " + Properties.Settings.Default.LFPgain + "X \r\n";
                }
                else
                {
                    textBox_Note.Text += "  LFP Recording Type:             Digitally processed LFPs \r\n";
                    textBox_Note.Text += "  No. Channels:                   " + Properties.Settings.Default.NumChannels + "\r\n";
                }
                textBox_Note.Text += "  Samp. Freq (Hz):                " + Properties.Settings.Default.LFPSampleFrequency + "\r\n";
                textBox_Note.Text += "  Low Freq. Cut (Hz):             " + Properties.Settings.Default.LFPLowCut + "\r\n";
                textBox_Note.Text += "  High Freq. Cut (Hz):            " + Properties.Settings.Default.LFPHighCut + "\r\n";
                textBox_Note.Text += "  Filter Order:                   " + Properties.Settings.Default.LFPNumPoles + "\r\n";
                textBox_Note.Text += "  Filter Type:                    Butterworth \r\n";
            }

            // MUA settings
            if (Properties.Settings.Default.ProcessMUA)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Multi-Unit Activity ** \r\n";
                textBox_Note.Text += "  No. Channels:                   " + Properties.Settings.Default.NumChannels + "\r\n";
                textBox_Note.Text += "  Samp. Freq (Hz):                " + Properties.Settings.Default.MUASampleFrequency + "\r\n";
                textBox_Note.Text += "  High Freq. Cut (Hz):            " + Properties.Settings.Default.MUAHighCutHz + "\r\n";
                textBox_Note.Text += "  Filter Order:                   " + Properties.Settings.Default.MUAFilterOrder + "\r\n";
                textBox_Note.Text += "  Filter Typ:                     Butterworth \r\n";
            }

            // EEG settings
            if (Properties.Settings.Default.ProcessMUA)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Electroencephalogram ** \r\n";
                textBox_Note.Text += "  No. Channels:                   " + Properties.Settings.Default.EEGNumChannels + "\r\n";
                textBox_Note.Text += "  Samp. Freq. (Hz):               " + Properties.Settings.Default.EEGSamplingRate + "\r\n";
                textBox_Note.Text += "  Digital Gain:                   " + Properties.Settings.Default.EEGGain + "X\r\n";
            }

            // Auxiliary Analog Input
            if (Properties.Settings.Default.useAuxAnalogInput)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Auxiliary Analog Input ** \r\n";
                textBox_Note.Text += "  No. Channels:                   " + Properties.Settings.Default.auxAnalogInChan.Count + "\r\n";
                textBox_Note.Text += "  Selected Channels:              " + Properties.Settings.Default.auxAnalogInChan[0].ToString() + "\r\n";
                for (int i = 1; i < Properties.Settings.Default.auxAnalogInChan.Count;  i++)
                    textBox_Note.Text += "                                  " + Properties.Settings.Default.auxAnalogInChan[i].ToString() + "\r\n";
                textBox_Note.Text += "  Samp. Freq. (Hz):               " + Properties.Settings.Default.RawSampleFrequency + "\r\n";
            }

            // Auxiliary Digital Input
            if (Properties.Settings.Default.useAuxDigitalInput)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Auxiliary Digital Input ** \r\n";
                textBox_Note.Text += "  No. Bits:                       32 \r\n";
                textBox_Note.Text += "  Input Port:                     " + Properties.Settings.Default.auxDigitalInPort + "\r\n";
                textBox_Note.Text += "  Samp. Freq. (Hz):               " + Properties.Settings.Default.RawSampleFrequency + "\r\n";
            }

            // General AO/DO
            if (Properties.Settings.Default.UseSigOut)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Auxiliary Analog and Digital Outputs ** \r\n";
                textBox_Note.Text += "  No. Digital Bits:               32 \r\n";
                textBox_Note.Text += "  Dig. Output Port:               " + Properties.Settings.Default.SigOutDev + "/Port0 \r\n";
                textBox_Note.Text += "  No. Analog Channels:            4 \r\n";
                textBox_Note.Text += "  An. Output Channels:            " + Properties.Settings.Default.SigOutDev + "/A0-3 \r\n";
                textBox_Note.Text += "  Samp. Freq. (Hz):               100000.0 \r\n";
            }


            // Electrical Stimulation Parameters
            if (Properties.Settings.Default.UseStimulator)
            {
                textBox_Note.Text += "\r\n";
                textBox_Note.Text += "** Electrical Stimulation ** \r\n";
                textBox_Note.Text += "  Current or Voltage Control:     " + (Properties.Settings.Default.StimVoltageControlled ? "Voltage" : "Current") + "\r\n";
                textBox_Note.Text += "  Multiplexer Type:               " + Properties.Settings.Default.MUXChannels + " bits \r\n";
                textBox_Note.Text += "  Channel Selection Bits:         " + Properties.Settings.Default.StimPortBandwidth + " bits \r\n";
                textBox_Note.Text += "  Samp. Freq (Hz):                100000.0 \r\n";
                textBox_Note.Text += "  No. Bits:                       32 \r\n";
            }



            // Current Recording Streams
            textBox_Note.Text += "\r\n";
            // Full FS streams
            textBox_Note.Text += "** Selected Recording Streams ** \r\n";
            textBox_Note.Text += (Properties.Settings.Default.recordRaw ? "  [X]" : "  [ ]") + " Raw Electrode Voltages \r\n";
            textBox_Note.Text += (Properties.Settings.Default.recordSpikeFilt ? "  [X]" : "  [ ]") + " Spike-band Filter \r\n";
            textBox_Note.Text += (Properties.Settings.Default.recordSalpa ? "  [X]" : "  [ ]") + " SALPA Filter \r\n";


            //textBox_Note.Text += "  Raw Electrode Voltages: " + Properties.Settings.Default.recordRaw + "\r\n";
            //textBox_Note.Text += "  Spike-band Filter: " + Properties.Settings.Default.recordSpikeFilt + "\r\n";
            //textBox_Note.Text += "  SALPA Filter: " + Properties.Settings.Default.recordSalpa + "\r\n";

            // Spikes
            textBox_Note.Text += (Properties.Settings.Default.recordRawSpikes ? "  [X]" : "  [ ]") + " Spikes from Raw \r\n";
            textBox_Note.Text += (Properties.Settings.Default.recordSalpaSpikes ? "  [X]" : "  [ ]") + " Spikes from SALPA \r\n";
            textBox_Note.Text += (Properties.Settings.Default.recordSpikes ? "  [X]" : "  [ ]") + " Fully Processed Spikes \r\n";

            //textBox_Note.Text += "  Spikes from Raw: " + Properties.Settings.Default.recordRawSpikes + "\r\n";
            //textBox_Note.Text += "  Spikes from SALPA: " + Properties.Settings.Default.recordSalpaSpikes + "\r\n";
            //textBox_Note.Text += "  Fully Processed Spikes: " + Properties.Settings.Default.recordSpikes + "\r\n";

            // E. Stim
            textBox_Note.Text += (Properties.Settings.Default.recordStim ? "  [X]" : "  [ ]") + " Electrical Stimuli \r\n";
            //textBox_Note.Text += "  Electrical Stimuli: " + Properties.Settings.Default.recordStim + "\r\n";

            // LFP
            textBox_Note.Text += (Properties.Settings.Default.recordLFP ? "  [X]" : "  [ ]") + " LFPs \r\n";
            //textBox_Note.Text += "  LFPs: " + Properties.Settings.Default.recordLFP + "\r\n";

            // MUA
            textBox_Note.Text += (Properties.Settings.Default.recordMUA ? "  [X]" : "  [ ]") + " MUA \r\n";
            //textBox_Note.Text += "  MUA: " + Properties.Settings.Default.recordMUA + "\r\n";

            // EEG
            textBox_Note.Text += (Properties.Settings.Default.recordEEG ? "  [X]" : "  [ ]") + " EEG \r\n";
            //textBox_Note.Text += "  EEG: " + Properties.Settings.Default.recordEEG + "\r\n";

            // Aux signals
            textBox_Note.Text += (Properties.Settings.Default.recordAuxAnalog ? "  [X]" : "  [ ]") + " Aux. Analog Input \r\n";
            textBox_Note.Text += (Properties.Settings.Default.recordAuxDigital ? "  [X]" : "  [ ]") + " Aux. Digital Input \r\n";

            //textBox_Note.Text += "  Aux. Analog Input: " + Properties.Settings.Default.recordAuxAnalog + "\r\n";
            //textBox_Note.Text += "  Aux. Digital Input: " + Properties.Settings.Default.recordAuxDigital + "\r\n";

            Console.WriteLine("NeuroRighter settings written to log entry form.");

        }


    }
}
