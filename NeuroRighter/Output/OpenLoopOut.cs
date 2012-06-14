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
using System.IO;
using NeuroRighter.Log;
using NeuroRighter.Server;

namespace NeuroRighter.Output
{
    /// <summary>
    /// <title> OpenLoopOutput</title>
    /// Implementation class for the open-loop (from file) AO and DO capabilities of NR. This
    /// class produces AO and DO that is syncronized to the recording clock. It uses a double
    /// buffering system to allow continous, non-repeating waveform generation.
    /// <author> Jon Newman </author>
    /// </summary>
    internal class OpenLoopOut
    {

        Logger debugger;

        // File locations
        internal string stimFid;
        internal string digFid;
        internal string auxFid;

        // Actual Tasks that play with NI DAQ
        internal Task buffLoadTask;
        internal ContStimTask stimTaskMaker;
        //internal AuxOutTask auxTaskMaker;

        // Master timing and triggering task
        internal Task masterTask;

        // Interpreters for open-loop files
        internal File2Stim stimProtocol;
        internal File2Dig digProtocol;
        internal File2Aux auxProtocol;

        // Bools that decide weather the Stim, Dig and Aux outputs are finished
        internal bool stimDone = false;
        internal bool digDone = false;
        internal bool auxDone = false;

        // waveform that gets ripped off NR UI
        bool useManStimWave;
        internal double[] guiWave; 

        // Constants
        private int outputSampFreq;
        internal int OUTPUT_BUFFER_SIZE = 10000; // Number of samples delivered to DAQ per buffer load during stimulation from file
        internal ulong EVENTS_PER_BUFFER_LOAD = 50; // Number of events loaded into the buffer everytime it runs low

        // Event system to notify NR that the Open Loop Output protocol is finished
        public delegate void OpenLoopOutFinishedEventHandler(object sender, EventArgs e);
        public event OpenLoopOutFinishedEventHandler OpenLoopOutIsFinished;

        internal OpenLoopOut(string sf, string df, string af, int fs, Task masterTask, Logger debugger)
        {
            this.stimFid = sf;
            this.digFid = df;
            this.auxFid = af;
            this.outputSampFreq = fs;
            this.masterTask = masterTask;
            this.useManStimWave = false;
            this.debugger = debugger;
        }

        internal OpenLoopOut(string sf, string df, string af, int fs, Task masterTask, Logger debugger, double[] standardWave)
        {
            this.stimFid = sf;
            this.digFid = df;
            this.auxFid = af;
            this.outputSampFreq = fs;
            this.masterTask = masterTask;
            this.guiWave = standardWave;
            this.debugger = debugger;
            this.useManStimWave = true;
        }

        /// <summary>
        /// Starts the OpenLoop Experiment by calling methods to create nessesary tasks and 
        /// create/populate the double buffering system using for loading of continous outputs.
        /// </summary>
        /// <returns></returns>
        internal bool Start() //object sender, EventArgs e
        {
            bool stimSetupFail = false;
            try
            {
                bool stimFileProvided = stimFid.Length > 0;
                bool digFileProvided = digFid.Length > 0;
                bool auxFileProvided = auxFid.Length > 0;

                #region check file paths

                // Make sure that the user provided a file of some sort
                if (!stimFileProvided && !digFileProvided && !auxFileProvided)
                {
                    MessageBox.Show("You need to provide a *.olstim, *.oldig, and/or a *.olaux to use the open-loop stimulator.");
                    stimSetupFail = true;
                    return stimSetupFail; 
                }

                // Make sure that the user has input a valid file path for the stimulation file
                if (stimFileProvided && !CheckFilePath(stimFid))
                {
                    MessageBox.Show("The *.olstim file provided does not exist");
                    stimSetupFail = true;
                    return stimSetupFail; 
                }

                // Make sure that the user has input a valid file path for the digital file
                if (digFileProvided && !CheckFilePath(digFid))
                {
                    MessageBox.Show("The *.oldig file provided does not exist");
                    stimSetupFail = true;
                    return stimSetupFail; 
                }

                // Make sure that the user has input a valid file path for the digital file
                if (auxFileProvided && !CheckFilePath(auxFid))
                {
                    MessageBox.Show("The *.olaux file provided does not exist");
                    stimSetupFail = true;
                    return true; 
                }

                #endregion

                // This task will govern the periodicity of DAQ circular-buffer loading so that
                // all digital and stimulus output from the system is hardware timed
                ConfigureCounter();
                string masterLoad = buffLoadTask.COChannels[0].PulseTerminal;//"/"+Properties.Settings.Default.SigOutDev + "/ctr1";
            
                // Set up stimulus output support
                if (stimFileProvided)
                {
                    #region If the user provided a .olstim file

                    if (!Properties.Settings.Default.UseStimulator)
                    {
                        MessageBox.Show("You must use configure your hardware to use NeuroRighter's Stimulator for this feature");
                        stimSetupFail = true;
                        return stimSetupFail; 
                    }

                    // Call configuration method
                    //ConfigureStim(masterTask);

                    // Create a File2Stim object and start to run the protocol via its methods
                    if (useManStimWave)
                    {
                        stimProtocol = new File2Stim(stimFid,
                            outputSampFreq,
                            OUTPUT_BUFFER_SIZE,
                            buffLoadTask,
                            masterTask,
                            masterLoad,
                            debugger,
                            guiWave,
                            Properties.Settings.Default.stimRobust);
                            
                            

                        stimProtocol.AlertAllFinished +=
                            new File2Stim.AllFinishedHandler(SetStimDone);
                        stimProtocol.AlertAllFinished += 
                            new File2Stim.AllFinishedHandler(StopOpenLoopOut);
                    }
                    else
                    {
                        stimProtocol = new File2Stim(stimFid,
                            outputSampFreq,
                            OUTPUT_BUFFER_SIZE,
                            buffLoadTask,
                            masterTask,
                            masterLoad,
                            debugger,
                            null,
                            Properties.Settings.Default.stimRobust);
                            

                        stimProtocol.AlertAllFinished +=
                            new File2Stim.AllFinishedHandler(SetStimDone);
                        stimProtocol.AlertAllFinished += 
                            new File2Stim.AllFinishedHandler(StopOpenLoopOut);

                    }

                    stimSetupFail = stimProtocol.Setup();
                    if (stimSetupFail)
                    {
                        return stimSetupFail;
                    }

                    stimProtocol.Start();

                    #endregion
                }
                else
                {
                    stimDone = true;
                }

                // Set up AO/DO support
                if (digFileProvided || auxFileProvided)
                {
                    #region If the user provided a .oldig or .olaux file
                    if (!Properties.Settings.Default.UseSigOut)
                    {
                        MessageBox.Show("You must use configure your hardware to use A0/D0 to use this feature");
                        stimSetupFail = true;
                        return stimSetupFail; 
                    }

                    // If no file was provided, mark dig or aux outputs as completed
                    if (!digFileProvided)
                    {
                        digDone = true;
                    }

                    if (!auxFileProvided)
                    {
                        auxDone = true;
                    }

                    //ConfigureAODO(digFileProvided, masterTask);
                    //AuxBuffer ab = null;
                    //DigitalBuffer db = null;
                    //because both buffers need to know about eachother (for restarts), we need to build the pointers here and pass them down the procotols

                    auxProtocol = new File2Aux(auxFid,
                        outputSampFreq,
                        OUTPUT_BUFFER_SIZE,
                        buffLoadTask,
                        masterTask,
                        masterLoad,
                        EVENTS_PER_BUFFER_LOAD,
                        auxFileProvided,
                        debugger,
                        Properties.Settings.Default.stimRobust);

                    auxProtocol.AlertAllFinished +=
                        new File2Aux.AllFinishedHandler(SetAuxDone);
                    auxProtocol.AlertAllFinished +=
                        new File2Aux.AllFinishedHandler(SetDigDone);
                    auxProtocol.AlertAllFinished +=
                        new File2Aux.AllFinishedHandler(StopOpenLoopOut);

                    if (digFileProvided)
                    {
                        digProtocol = new File2Dig(digFid,
                            outputSampFreq,
                            OUTPUT_BUFFER_SIZE,
                            buffLoadTask,
                            masterTask,
                            masterLoad,
                            EVENTS_PER_BUFFER_LOAD,
                            debugger,
                            Properties.Settings.Default.stimRobust
                            );

                        digProtocol.connectBuffer(auxProtocol.refBuffer());
                        auxProtocol.connectBuffer(digProtocol.refBuffer());
                        
                        digProtocol.Setup();
                        auxProtocol.Setup(digProtocol.numBuffLoadsRequired);
                    }
                    else
                        auxProtocol.Setup(ulong.MaxValue);

                   
                    
                    auxProtocol.Start();

                    

                    #endregion

                }
                else
                {
                    digDone = true;
                    auxDone = true;
                }

                // Start the master load syncing task which you have attached buffer-refill methods to
                buffLoadTask.Start();

                // Made it through start
                stimSetupFail = false;
                return stimSetupFail; 

            }
            catch(Exception e)
            {
                KillTasks();
                try
                {
                    digProtocol.Stop();
                }
                catch (Exception me)
                { }
                try
                {
                auxProtocol.Stop();
                }
                catch (Exception me)
                { }
                try
                {
                stimProtocol.Stop();
                 }
                catch (Exception me)
                { }
                MessageBox.Show("Could not make it through OpenLoopOut.Start(): \n" + e.Message);
                stimSetupFail = true;
                return stimSetupFail; 
            }
        }

        internal bool CheckFilePath(string filePath)
        {
            string sourceFile = @filePath;
            bool check = File.Exists(sourceFile);
            return (check);
        }

        internal void ConfigureCounter()
        {
            //configure counter
            if (buffLoadTask != null) { buffLoadTask.Dispose(); buffLoadTask = null; }

            buffLoadTask = new Task("stimBufferTask");

            // Trigger a buffer load event off every edge of this channel
            buffLoadTask.COChannels.CreatePulseChannelFrequency(Properties.Settings.Default.AnalogInDevice[0] + "/ctr1",
                "BufferLoadCounter", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, ((double)outputSampFreq / (double)OUTPUT_BUFFER_SIZE) / 2.0, 0.5);
            buffLoadTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);
            buffLoadTask.SynchronizeCallbacks = false;
            buffLoadTask.Timing.ReferenceClockSource = "OnboardClock";
            buffLoadTask.Control(TaskAction.Verify);

            // Syncronize the start to the master recording task
            buffLoadTask.Triggers.ArmStartTrigger.ConfigureDigitalEdgeTrigger(
                masterTask.Triggers.StartTrigger.Terminal, DigitalEdgeArmStartTriggerEdge.Rising);

           // buffLoadTask.CounterOutput+=new CounterOutputEventHandler(delegate
            //    {
                    
            //        debugger.Write("output counter tick " );
                    
             //   }
           // );
        }

        //internal void ConfigureStim(Task masterTask)
        //{

        //    //configure stim
        //    // Refresh DAQ tasks as they are needed for file2stim
        //    if (stimTaskMaker != null)
        //    {
        //        stimTaskMaker.Dispose();
        //        stimTaskMaker = null;
        //    }

        //    // Create new DAQ tasks and corresponding writers
        //    stimTaskMaker = new ContStimTask(Properties.Settings.Default.StimulatorDevice, 
        //        OUTPUT_BUFFER_SIZE);
        //    stimTaskMaker.MakeAODOTasks("NeuralStim",
        //        Properties.Settings.Default.StimPortBandwidth,
        //        outputSampFreq);

        //    // Verify
        //    stimTaskMaker.VerifyTasks();

        //    // Sync DO start to AO start
        //    stimTaskMaker.SyncDOStartToAOStart();

        //    // Syncronize stimulation with the master task
        //    stimTaskMaker.SyncTasksToMasterClock(masterTask);
        //    stimTaskMaker.SyncTasksToMasterStart(buffLoadTask.COChannels[0].PulseTerminal);

        //    // Create buffer writters
        //    stimTaskMaker.MakeWriters();

        //    // Verify
        //    stimTaskMaker.VerifyTasks();
        //}

        //internal void ConfigureAODO(bool digProvided, Task masterTask)
        //{
        //    //configure stim
        //    // Refresh DAQ tasks as they are needed for file2stim
        //    if (auxTaskMaker != null)
        //    {
        //        auxTaskMaker.Dispose();
        //        auxTaskMaker = null;
        //    }

        //    // Create new DAQ tasks and corresponding writers
        //    auxTaskMaker = new AuxOutTask(Properties.Settings.Default.SigOutDev, 
        //        OUTPUT_BUFFER_SIZE);
        //    auxTaskMaker.MakeAODOTasks("auxOut", 
        //        outputSampFreq, 
        //        digProvided);

        //    // Verify
        //    auxTaskMaker.VerifyTasks();

        //    // Sync DO start to AO start
        //    if (digProvided)
        //        auxTaskMaker.SyncDOStartToAOStart();

        //    // Syncronize stimulation with the master task
        //    auxTaskMaker.SyncTasksToMasterClock(masterTask);
        //    auxTaskMaker.SyncTasksToMasterStart(buffLoadTask.COChannels[0].PulseTerminal);

        //    //// Pipe the master clock to PFI1 on this board (for use with zeroing)
        //    auxTaskMaker.PipeReferenceClockToPFI(masterTask, "PFI2");

        //    // Create buffer writters
        //    auxTaskMaker.MakeWriters();

        //    // Verify
        //    auxTaskMaker.VerifyTasks();
        //}

        internal void KillTasks()
        {
            
                if (buffLoadTask != null)
                {
                    lock (buffLoadTask)
                    {
                    buffLoadTask.Dispose();
                    buffLoadTask = null;
                    }
                }
            
            //if (digProtocol != null)
            //{
            //    digProtocol.Stop();
            //}

            //if (stimProtocol != null)
            //{
            //    stimProtocol.Stop();
            //}

            // if (auxProtocol != null)
            //{
            //    auxProtocol.Stop();
            //}

        }

        internal void StopOpenLoopOut(object sender, EventArgs e)
        {

            // Tell NR that the OpenLoopOut protocol is done
            if (stimDone && auxDone && digDone)
            {
                KillTasks();
                OpenLoopOutFinishedEventHandler temp = OpenLoopOutIsFinished;
                if (temp != null)
                    temp(this, e);
            }
        }

        internal void SetStimDone(object sender, EventArgs e)
        {
            stimDone = true;
        }

        internal void SetDigDone(object sender, EventArgs e)
        {
            digDone = true;
        }

        internal void SetAuxDone(object sender, EventArgs e)
        {
            auxDone = true;
        }

        internal void StopAllBuffers()
        {
            if (auxProtocol != null)
                auxProtocol.Stop();
            if (digProtocol != null)
                digProtocol.Stop();
            if (stimProtocol != null)
                stimProtocol.Stop();
            

        }


    }
}
