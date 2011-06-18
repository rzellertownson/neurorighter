// NOTE.CS
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
using System.Windows.Forms;

namespace NeuroRighter
{
    ///<summary> Create/edit log file during an experiement.</summary>
    ///<author>Jon Newman</author>
    sealed internal partial class NeuroRighter
    {
        #region Note Writing
        private void button_EnterNote_Click(object sender, EventArgs e)
        {
            if (originalNameBase != null)
            {
                RecordingNote rc = new RecordingNote(originalNameBase);
                rc.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please enter a filename to use this feature");
            }
        }
        #endregion
    }
}
