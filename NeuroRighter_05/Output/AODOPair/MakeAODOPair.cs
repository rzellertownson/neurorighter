// NeuroRighter 
// Copyright (c) 2008-2009 John Rolston
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
using NationalInstruments.DAQmx;

namespace NeuroRighter.Output
{
    /// <summary>
    /// Base class for creating a pair of AO and DO tasks within NR. Contains methods
    /// for synchronizing those tasks to a master clock (usually this would be the AI
    /// task), and for synchronizing the task starts to a master digital edge.
    /// <author> Jon Newman </author>
    /// </summary>
    internal abstract class MakeAODOPair
    {
        // Contains AO and DO Stimulation Tasks
        internal Task analogTask;
        internal Task digitalTask;
        internal AnalogMultiChannelWriter analogWriter;
        internal DigitalSingleChannelWriter digitalWriter;

        // Hardware parameters
        internal string dev;
        internal int bufferSize;

        internal MakeAODOPair(string device, int bufferSize) 
        {
            this.dev = device;
            this.bufferSize = bufferSize;
        }

        internal virtual void MakeAODOTasks(string stimTaskName, 
            int stimPortBandWidth, int sampRate) { }

        internal virtual void MakeAODOTasks(string auxTaskName,
            int buffSize, bool useDigOut) { }

        internal void VerifyTasks()
        {
            if (analogTask != null)
            {
                analogTask.Control(TaskAction.Verify);
            }
            if (digitalTask != null)
            {
                digitalTask.Control(TaskAction.Verify);
            }
        }

        // Sync both AO and DO to main AI clock
        internal void SyncTasksToMasterClock(Task masterTask)
        {
            analogTask.Timing.ReferenceClockSource =
                        masterTask.Timing.ReferenceClockSource;
            analogTask.Timing.ReferenceClockRate =
                masterTask.Timing.ReferenceClockRate;
        }

        internal void SyncDOStartToAOStart()
        {
            // Sync DO start to AO start
            digitalTask.Timing.ConfigureSampleClock("/" + dev + "/ao/SampleClock",
                analogTask.Timing.SampleClockRate,
                SampleClockActiveEdge.Rising,
                SampleQuantityMode.ContinuousSamples,
                bufferSize);
        }

        // Sync both AO and DO to main AI start
        internal void SyncTasksToMasterStart(Task masterTask)
        {
            analogTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(
                    masterTask.Triggers.StartTrigger.Terminal, DigitalEdgeStartTriggerEdge.Rising);
        }

        internal void MakeWriters()
        {
            if (analogTask != null)
            {
                analogWriter = new AnalogMultiChannelWriter(analogTask.Stream);
            }
            if (digitalTask != null)
            {
                digitalWriter = new DigitalSingleChannelWriter(digitalTask.Stream);
            }
        }

        internal void Dispose()
        {
            if (analogTask != null) 
            { 
                analogTask.Dispose(); analogTask = null; 
            }
            if (digitalTask != null) 
            { 
                digitalTask.Dispose(); digitalTask = null; 
            }
        }

    }
}
