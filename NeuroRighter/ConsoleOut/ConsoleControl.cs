using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleWidget;
using System.Windows.Forms;

namespace NeuroRighter
{

    /// <summary>
    /// Wrapper for console output in NR.
    /// </summary>
    class ConsoleControl
    {
        // Console
        ConsoleForm nrConsole;
        bool consoleHidden = true;

        /// <summary>
        /// Create a new  Console Controller.
        /// </summary>
        public ConsoleControl()
        {
            nrConsole = new ConsoleForm();
            nrConsole.Text = "NeuroRighter Console";
            nrConsole.ControlBox = true;
            nrConsole.MinimizeBox = false;
            nrConsole.FormClosing += new System.Windows.Forms.FormClosingEventHandler(nrConsole_FormClosing);
        }

        private void nrConsole_FormClosing(object sender, FormClosingEventArgs e)
        {
            HideConsole();
            e.Cancel = true;
        }


        /// <summary>
        /// Is the console currently hidden?
        /// </summary>
        public bool ConsoleHidden
        {
            get
            {
                return consoleHidden;
            }
            set
            {
                value = consoleHidden;
            }
        }

        /// <summary>
        /// Hide the console.
        /// </summary>
        internal void ShowConsole()
        {
            nrConsole.Show();
            nrConsole.BringToFront();
            consoleHidden = false;
        }

        /// <summary>
        /// Show the console.
        /// </summary>
        internal void HideConsole()
        {
            nrConsole.Hide();
            consoleHidden = true;
        }

    }
}
