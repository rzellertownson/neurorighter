using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.FileWriting;

namespace NeuroRighter
{
    ///<summary>Methods that invoke and inform the file writing classes
    ///of settings changes and data aquisision.</summary>
    ///<author>Jon Newman</author>
    sealed internal partial class NeuroRighter
    {

        private void button_SetRecordingStreams_Click(object sender, EventArgs e)
        {
            recordingSettings.RefreshForm();
            recordingSettings.MoveToDefaultLocation();
            recordingSettings.ShowDialog();
        }

        private void recordingSettings_SettingsHaveChanged(object sender, EventArgs e)
        {
            recordingSettings.RefreshForm();
        }

    }
}
