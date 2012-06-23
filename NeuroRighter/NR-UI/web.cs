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
    sealed internal partial class NeuroRighter : Form
    {
        /// <summary>
        /// Access the main documentation page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void documenationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://sites.google.com/site/neurorighter/");
        }

        /// <summary>
        /// Access API Documentation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void realTimeAPIReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://potterlab.gatech.edu/main/neurorighter-api-ref/");
        }


        /// <summary>
        /// Access NR code repository.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void codeRespositoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://code.google.com/p/neurorighter/");
        }

        /// <summary>
        /// Access the closed-loop example codes on the NR repository
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closedloopExampleCodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://code.google.com/p/neurorighter/source/browse/#svn%2FNR-ClosedLoop-Examples");
        }
    }
}
