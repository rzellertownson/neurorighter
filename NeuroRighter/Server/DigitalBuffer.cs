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
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using NeuroRighter.DataTypes;
using NeuroRighter.Debug;

namespace NeuroRighter.Server
{
    // called when the 2+requested number of buffer loads have occured
    //internal delegate void StimulationCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    //internal delegate void QueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    //internal delegate void DAQLoadCompletedHandler(object sender, EventArgs e);

    /// <summary>
    /// NeuroRighter's standard output buffer for digital signals.
    /// </summary>
    public class DigitalBuffer : NROutBuffer<DigitalOutEvent>
    {
        AuxBuffer analogBuffer;
        internal bool failflag;

        internal DigitalBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold, bool robust)
            : base(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold, robust) {
                //this.analogBuffer = analogBuffer;
                failflag = false;
        }
       
        internal void grabPartner(AuxBuffer analogBuffer)//different from the analog buffer version!
        {
            this.analogBuffer = analogBuffer;
            recoveryInProgress = analogBuffer.recoveryInProgress;
        }

        internal void setNBLC(ulong nblc)
        {
            numBuffLoadsCompleted = nblc;
        }

        public override double GetTime()
        {
            return analogBuffer.GetTime();
        }

        public override ulong GetCurrentSample()
        {
            return analogBuffer.GetCurrentSample();
        }

        protected override void SetupTasksSpecific(ref Task[] analogTasks, ref Task[] digitalTasks)
        {
            //this should never get called
            
        }

        protected override void  WriteEvent(DigitalOutEvent stim, ref List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            anEventValues = null;
            digEventValues = new List<uint[]>();
            digEventValues.Add(new uint[1]);
            digEventValues.ElementAt(0)[0] = stim.PortInt32;
        }
    }

}
