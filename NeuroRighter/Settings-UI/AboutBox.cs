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
    public partial class AboutBox : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public AboutBox()
        {
            InitializeComponent();
            label_version.Text += System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                VisitLink();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link that was clicked.  Details: " + ex.Message);
            }
        }
        private void VisitLink()
        {
            // Change the color of the link text by setting LinkVisited 
            // to true.
            linkLabel1.LinkVisited = true;
            //Call the Process.Start method to open the default browser 
            //with a URL:
            System.Diagnostics.Process.Start("http://www.johnrolston.com");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                VisitLink();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link that was clicked.  Details: " + ex.Message);
            }
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
