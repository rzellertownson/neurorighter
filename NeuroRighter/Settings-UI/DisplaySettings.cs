// NeuroRighter
// Copyright (c) 2008 John Rolston
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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeuroRighter
{

    public partial class DisplaySettings : Form
    {
        /// <summary>
        /// NeuroRighter display settings.
        /// </summary>
        public DisplaySettings()
        {
            InitializeComponent();

            if (Properties.Settings.Default.ChannelMapping == "invitro")
                radioButton_inVitroMapping.Checked = true;
            else if (Properties.Settings.Default.ChannelMapping == "invivo")
                radioButton_inVivoMapping.Checked = true;
        }

        private void button_accept_Click(object sender, EventArgs e)
        {
            if (radioButton_inVitroMapping.Checked)
            {
                Properties.Settings.Default.ChannelMapping = "invitro";
            }
            else if (radioButton_inVivoMapping.Checked)
            {
                Properties.Settings.Default.ChannelMapping = "invivo";
            }
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
