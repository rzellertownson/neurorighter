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

    /// <summary>
    /// 
    /// </summary>
    /// <author>John Rolston (rolston2@gmail.com)</author>
    public partial class ProcessingSettings : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public ProcessingSettings()
        {
            InitializeComponent();

            checkBox_processLFPs.Checked = Properties.Settings.Default.UseLFPs;
        }

        private void button_accept_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.UseLFPs = checkBox_processLFPs.Checked;

            Properties.Settings.Default.Save();
            this.Close();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
