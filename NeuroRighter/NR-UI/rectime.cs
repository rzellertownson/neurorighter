// MISC.CS
// Copyright (c) 2008-2011 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace NeuroRighter
{

    ///<summary>misc. methods for the NeuroRighter mainform.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter
    {
        // Recording time display
        private void timer_timeElapsed_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now - experimentStartTime;
            label_timeElapsed.Text = "Time elapsed: \r\n" + String.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        private void checkBox_enableTimedRecording_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown_timedRecordingDuration.Enabled = checkBox_enableTimedRecording.Checked;
            numericUpDown_timedRecordingDurationSeconds.Enabled = checkBox_enableTimedRecording.Checked;
            checkbox_repeatRecord.Enabled = checkBox_enableTimedRecording.Checked;
        }
    }
}
