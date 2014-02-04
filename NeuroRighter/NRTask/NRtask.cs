// NeuroRighter
// Copyright (c) 2008-2012 Potter Lab
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
using NeuroRighter.Server;
using NeuroRighter.Log;
using NeuroRighter;

namespace NeuroRighter.NeuroRighterTask
{
    /// <summary>
    /// NeuroRighter's abstract class for user defined closed loop experiments.
    /// </summary>
    public class NRTask
    {
        /// <summary>
        /// NeuroRighter's data server. This object contains specialized data servers that provided access to the steams specified at 'real-time' in hardware settings.
        /// </summary>
        protected DataSrv NRDataSrv;

        /// <summary>
        /// NeuroRighter's stimulus server. This object contains specialized data servers that can be used to produce electrical stimuli, analog output and digital output on-the-fly.
        /// </summary>
        protected StimSrv NRStimSrv;

        private Logger debugger;
        private double[] cannedWaveform;
        private string nrFilePath;
        private bool nrRecording;
        private NeuroRighter NR;

        internal void Grab(DataSrv DatSrv, StimSrv StimSrv, Logger Debugger, string NRFilePath, bool NRRecording, NeuroRighter NR)
        {
            this.NRDataSrv = DatSrv;
            this.NRStimSrv = StimSrv;
            this.debugger = Debugger;
            this.nrFilePath = NRFilePath;
            this.nrRecording = NRRecording;
            this.NR = NR;//we need a reference back to NR to initiate the stop sequence
        }

        internal void GrabWave(double[] waveform)
        {
            cannedWaveform = waveform;
        }

        /// <summary>
        /// stops the task as if you had clicked the stop button.  Stimulation is stopped, and then the cleanup() method is called
        /// </summary>
        internal protected void Stop()
        {
            NR.KillClosedLoop();
        }

        /// <summary>
        /// Abstract method to initialize the closed-loop protocol.  Executes upon clicking the start button, and finishes execution before
        /// recording, stimulation or the Loop() method execute.  You probably want to use this for setting up a GUI, initializing
        /// data structures, and constructing larger objects/initializing secondary threads.
        /// </summary>
        internal protected virtual void Setup()
        { }

        /// <summary>
        /// The meat.  This abstract method gets called repeatedly, after Setup() has completed.  It is triggered off of a clock in the
        /// NI DAQ, and given high priority relative to other threads.  Use this method to update stimulation parameters, grab data
        /// off of the NR data streams, and alert GUIs that updates are available.  Try to offload really intensive processing to 
        /// secondary threads, and just use Loop() for the absolutely essential high-speed updates
        /// </summary>
        internal protected virtual void Loop(object sender, EventArgs e)
        { }

        /// <summary>
        /// This abstract method will be executed last out of the three overriden methods in this class.  It is called either by clicking the
        /// 'stop' button during closed loop execution, or by calling it directly through some other closed loop method (which will also
        /// force the closed loop protocol to close).  Use this method to close off file streams, dispose objects.
        /// </summary>
        internal protected virtual void Cleanup()
        { }

        #region Protected Accessors

        ///// <summary>
        ///// NeuroRighter's data server object that can be used to access incoming data streams.
        ///// </summary>
        //protected DataSrv DatSrv
        //{
        //    get
        //    {
        //        return datSrv;
        //    }
        //}

        ///// <summary>
        ///// NeuroRighter's data server object for writing data to the doubled-buffered output FIFO on NI data aqusition cards.
        ///// </summary>
        //protected StimSrv StimSrv
        //{
        //    get
        //    {
        //        return stimSrv;
        //    }
        //}

        /// <summary>
        /// A real-time debugging tool usefule for debugging protcols created with the NeuroRighter API.
        /// </summary>
        protected Logger Debugger
        {
            get
            {
                return debugger;
            }
        }

        /// <summary>
        /// Stimuation waveform defined within NeuroRighter's GUI.
        /// </summary>
        protected double[] PredefinedWaveform
        {
            get
            {
                return cannedWaveform;
            }
        }

        /// <summary>
        /// The current recording filepath for the main NeuroRighter application.
        /// </summary>
        protected string NRFilePath
        {
            get
            {
                return nrFilePath;
            }
        }

        /// <summary>
        /// Tells whether the main NeuroRighter applicaiton is saving data to disk.
        /// </summary>
        protected bool NRRecording
        {
            get
            {
                return nrRecording;
            }
        }

        #endregion
    }
}
