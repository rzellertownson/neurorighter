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


    }
}
