// EPHYSAQUIRE.CS
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NationalInstruments.UI;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.Analysis;
using NationalInstruments.Analysis.Dsp;
using NationalInstruments.Analysis.Dsp.Filters;
using NationalInstruments.Analysis.Math;
using NationalInstruments.Analysis.SignalGeneration;
using csmatio.types;
using csmatio.io;
using rawType = System.Double;
using ExtensionMethods;
using NeuroRighter.DataTypes;
using System.Media;

namespace NeuroRighter
{
    ///<summary> This porition of the NeuroRighter class handles all the setup of all e-phys recording (spikes, LFP and EEG) 
    ///and calls the methods that write these data to file. This class is also responsible for writting to the datSrv object,
    ///NR's data server. </summary>
    ///<author>Jon Newman</author>
    sealed internal partial class NeuroRighter
    {
        // Spike and stimulus Aquisition
        #region Spike and Stimulus Acquisition
        private List<BackgroundWorker> bwSpikes;

        /// <summary>
        /// Keeps track of when a stimulus pulse occurred
        /// </summary>
        public struct StimTick
        {
            public int index;
            public int numStimReads;
            public StimTick(int index, int numStimReads)
            {
                this.index = index;
                this.numStimReads = numStimReads;
            }
        }

        private double triggerStartTime = double.PositiveInfinity; //keep track of start of an integration trigger
        private double triggerStopTime = double.PositiveInfinity; //offset of integration trigger
        private bool inTrigger = false;
        private Object numStimReadsLock = new object();

        private void AnalogInCallback_spikes(IAsyncResult ar)
        {

            try
            {
                if (taskRunning)
                {
                    int taskNumber = (int)ar.AsyncState;
                    // Debugger.Write(taskNumber.ToString() + ":spike callback start");
                    trackingReads[taskNumber]++;

                    #region Stim_Timing_Acquisition
                    if (Properties.Settings.Default.UseStimulator && Properties.Settings.Default.RecordStimTimes)
                    {
                        lock (numStimReadsLock)
                        {
                            bool getStimData = true;
                            for (int i = 0; i < numStimReads.Count; ++i)
                            {
                                if (numStimReads[taskNumber] < numStimReads[i]) //Test if all stim reads are equal
                                {
                                    getStimData = false;
                                    ++numStimReads[taskNumber];
                                    break;
                                }
                            }

                            if (getStimData)
                            {
                                // This holds everything up since it does not have a callback or anything.
                                //This read handles both stim data and optional aux analog in data. Both are stored in stimDataTmp and parsed out later
                                //int numSampRead;
                                double[,] stimData = stimTimeReader.ReadMultiSample(spikeBufferLength);

                                //stimTimeReader.MemoryOptimizedReadMultiSample(spikeBufferLength, ref stimDataTmp, out numSampRead);

                                //double[,] stimDataTmp = new double[stimTimeChanSet.numericalChannels.Length, spikeBufferLength];

                                //Read the available data from the channels
                                if (twoAITasksOnSingleBoard)
                                {

                                    //Array.Copy(stimData, 0, stimDataTmp, stimTimeChanSet.numericalChannels[0] * spikeBufferLength, 
                                    //  stimTimeChanSet.numericalChannels.Length * spikeBufferLength);

                                    AuxAnalogFromStimData(stimData);
                                }

                                //Copy new data into prepended data, to deal with edge effects
                                double[] prependedData = new double[spikeBufferLength + STIM_BUFFER_LENGTH];
                                for (int i = 0; i < STIM_BUFFER_LENGTH; ++i)
                                    prependedData[i] = stimDataBuffer[i];
                                for (int i = 0; i < spikeBufferLength; ++i)
                                    prependedData[i + STIM_BUFFER_LENGTH] = stimData[0, i];

                                int startTimeStim = numStimReads[taskNumber] * spikeBufferLength - STIM_BUFFER_LENGTH; //Used to mark stim time for file

                                //Encoding is [v1 v2 v3], each lasting 200us
                                //'v1' and 'v2' encode channel number, 'v3' is the stim voltage
                                //'v1' says "which group of eight" was stimulated, 'v2' says
                                //     "which electrode in the group of eight".  E.g., electrode
                                //     16 would have v1=2 and v2=8.  'v1' and 'v2' are always in
                                //     the range of 1-8 volts
                                EventBuffer<ElectricalStimEvent> tempStimBuff = new EventBuffer<ElectricalStimEvent>(Properties.Settings.Default.RawSampleFrequency);

                                lock (stimIndices)
                                    for (int i = 0; i < spikeBufferLength; ++i)
                                    {
                                        //Check for stimIndices (this uses a different buffer, so it's synced to each buffer read
                                        if (stimData[0, i] > 0.9)
                                            stimIndices.Add(new StimTick(i, numStimReads[taskNumber]));

                                        //Get appropriate data and write to file
                                        if (prependedData[i] > 0.8 && prependedData[i + (int)stimJump] > 0.8 && prependedData[i + (int)(2 * stimJump)] > 0.8)
                                        {

                                            // Create ElectricalStimEvent Buffer and add it datSrv
                                            ElectricalStimEvent tempStimEvent = new ElectricalStimEvent((ulong)(startTimeStim + i),
                                                (short)((Convert.ToInt16((prependedData[i + 1] + prependedData[i + (int)stimJump]) / 2) -
                                                (short)1) * (short)8 +
                                                Convert.ToInt16((prependedData[i + (int)(2 * stimJump) + 1] + prependedData[i + (int)(3 * stimJump)]) / 2)),
                                                prependedData[i + (int)(5 * stimJump)], //Stim voltage
                                                prependedData[i + (int)(7 * stimJump)]);
                                            tempStimBuff.eventBuffer.Add(tempStimEvent);

                                            // send to datSrv


                                            if (switch_record.Value && recordingSettings.recordStim)
                                            {
                                                recordingSettings.stimOut.write(startTimeStim, prependedData, stimJump, i);
                                            }

                                            //Overwrite data as 0s, to prevent detecting the middle of a stim pulse in the next buffer cycle
                                            for (int j = 0; j < (int)(8 * stimJump) + 1; ++j)
                                                prependedData[j + i] = 0;
                                            i += (int)(9 * stimJump); //Jump past rest of waveform
                                        }

                                    }

                                datSrv.StimSrv.WriteToBuffer(tempStimBuff, taskNumber);

                                if (!inTrigger) //Assumes trigger lasts longer than refresh time
                                {
                                    for (int i = 0; i < stimData.GetLength(1); ++i)
                                    {
                                        //Check if there's a trigger change
                                        if (stimData[1, i] > 2.5)
                                        {
                                            triggerStartTime = i + numStimReads[taskNumber] * spikeBufferLength;
                                            triggerStopTime = double.PositiveInfinity; //Do this to ensure that we capture spikes till 
                                            inTrigger = true;

#if (DEBUG1)
                                            logFile.WriteLine("Trigger start time: " + triggerStartTime);
#endif
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < stimData.GetLength(1); ++i)
                                    {
                                        if (stimData[1, i] < 2.5)
                                        {
                                            triggerStopTime = i + numStimReads[taskNumber] * spikeBufferLength;
                                            inTrigger = false;
#if (DEBUG1)
                                            logFile.WriteLine("Trigger stop time: " + triggerStopTime);
#endif
                                            break;
                                        }
                                    }
                                }

                                for (int i = spikeBufferLength; i < spikeBufferLength + STIM_BUFFER_LENGTH; ++i)
                                    stimDataBuffer[i - spikeBufferLength] = prependedData[i];

                                //Clear out expired stimIndices
                                if (stimIndices.Count > 0)
                                {
                                    int oldestStimRead = numStimReads[0];
                                    for (int i = 1; i < numStimReads.Count; ++i)
                                    {
                                        if (numStimReads[i] < oldestStimRead)
                                            oldestStimRead = numStimReads[i];
                                    }
                                    lock (stimIndices)
                                        for (int i = stimIndices.Count - 1; i >= 0; --i)
                                            if (stimIndices[i].numStimReads < oldestStimRead - 1) //Add -1 to buy us some breating room
                                                stimIndices.RemoveAt(i);
                                }
                                ++numStimReads[taskNumber];
                            }
                        }
                    }
                    #endregion
                    bwIsRunning[taskNumber] = true;
                    if (!bwSpikes[taskNumber].IsBusy)
                        bwSpikes[taskNumber].RunWorkerAsync(new Object[] { taskNumber, ar });
                    else
                    {
                        DateTime errortime = DateTime.Now;
                        string format = "HH:mm:ss";    // Use this format
                        Console.WriteLine("Warning: bwSpikes was busy at: " + errortime.ToString(format));  // Write to console
                    }
                    //Debugger.Write(taskNumber.ToString() + ":spike callback stop");
                }
            }
            catch (DaqException exception)
            {
                //Display Errors
                MessageBox.Show(exception.Message);
                reset();
            }

        }
        #endregion //End spike acquisition

        // LFP and MUA Aquisition
        #region LFP and MUA Aquisition
        private void AnalogInCallback_LFPs(IAsyncResult ar)
        {
            try
            {
                if (taskRunning)
                {
                    //Read the available data from the channels
                    lfpData = lfpReader.EndReadInt16(ar);

                    //Write to file in format [numChannels numSamples]
                    #region WriteLFPFile
                    if (switch_record.Value && recordingSettings.recordLFP)
                    {
                        recordingSettings.lfpOut.read(lfpData, numChannels, 0, lfpBufferLength);
                    }
                    #endregion

                    //Convert to scaled double array
                    for (int i = 0; i < numChannels; ++i)
                    {
                        //filtLFPData[i] = new double[lfpBufferLength];
                        for (int j = 0; j < lfpBufferLength; ++j)
                        {
                            filtLFPData[i][j] = (rawType)lfpData[i, j] * (rawType)lfpData[i, j] * (rawType)lfpData[i, j] * (rawType)scalingCoeffsLFPs[3] +
                                (rawType)lfpData[i, j] * (rawType)lfpData[i, j] * (rawType)scalingCoeffsLFPs[2] +
                                (rawType)lfpData[i, j] * (rawType)scalingCoeffsLFPs[1] + (rawType)scalingCoeffsLFPs[0];
                        }
                    }

                    //Filter
                    if (checkBox_LFPsFilter.Checked)
                        for (int i = 0; i < numChannels; ++i)
                            lfpFilter[i].filterData(filtLFPData[i]);

                    //Send to datSrv
                    if (Properties.Settings.Default.useLFPDataBuffer)
                        datSrv.LFPSrv.WriteToBuffer(filtLFPData, 0, numChannels);

                    //Post to PlotData buffer
                    lfpPlotData.write(filtLFPData, 0, numChannels);

                    //Setup next callback
                    lfpReader.BeginReadInt16(lfpBufferLength, lfpCallback, lfpReader);
                }
            }
            catch (DaqException exception)
            {
                //Display Errors
                MessageBox.Show(exception.Message);
                reset();
            }
        }
        #endregion

        // EEG Aquisition
        #region EEG Aquisition
        private void AnalogInCallback_EEG(IAsyncResult ar)
        {
            try
            {
                if (taskRunning)
                {
                    //Read the available data from the channels
                    eegData = eegReader.EndReadInt16(ar);

                    double temp;
                    double rangeHigh = eegTask.AIChannels.All.RangeHigh;
                    double rangeLow = eegTask.AIChannels.All.RangeLow;

                    //Write to file in format [numChannels numSamples]
                    #region WriteEEGFile
                    if (switch_record.Value && recordingSettings.recordEEG)
                    {
                        for (int j = 0; j < eegBufferLength; ++j)
                            for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                                recordingSettings.eegOut.read(eegData[i, j], i);
                    }
                    #endregion

                    //Convert to scaled double array
                    for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                    {
                        //filtLFPData[i] = new double[lfpBufferLength];
                        for (int j = 0; j < eegBufferLength; ++j)
                        {
                            filtEEGData[i][j] = (double)eegData[i, j] * (double)eegData[i, j] * (double)eegData[i, j] * scalingCoeffsEEG[3] +
                                (double)eegData[i, j] * (double)eegData[i, j] * scalingCoeffsEEG[2] +
                                (double)eegData[i, j] * scalingCoeffsEEG[1] + scalingCoeffsEEG[0];
                        }
                    }

                    //Filter
                    if (checkBox_eegFilter.Checked)
                        for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                            filtEEGData[i] = eegFilter[i].FilterData(filtEEGData[i]);

                    // Send to datSrv
                    if (Properties.Settings.Default.useEEGDataBuffer)
                        datSrv.EEGSrv.WriteToBuffer(filtEEGData, 0, Properties.Settings.Default.EEGNumChannels);

                    //Stacked plot (if the LFP tab is selected)
                    int jMax = eegPlotData.GetLength(1) - eegBufferLength / eegDownsample;
                    for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)  //for each channel
                    {
                        //first, move old data down in array
                        for (int j = 0; j < jMax; ++j)
                            eegPlotData[i, j] = eegPlotData[i, j + eegBufferLength / eegDownsample];

                        //now, scale new data by stacking offset; add to end of array
                        //double offset = 2 * i * lfpTask.AIChannels.All.RangeHigh;
                        for (int j = jMax; j < eegPlotData.GetLength(1); ++j)
                        {
                            //lfpPlotData[i, j] = finalLFPData[i][(j - jMax) * lfpDownsample] * lfpDisplayGain - lfpOffset[i];
                            temp = filtEEGData[i][(j - jMax) * eegDownsample] * eegDisplayGain;
                            if (temp > rangeHigh)
                                temp = rangeHigh;
                            else if (temp < rangeLow)
                                temp = rangeLow;
                            eegPlotData[i, j] = temp - eegOffset[i];
                        }
                    }
                    if (tabControl.SelectedTab.Text == "EEG" && !checkBox_freeze.Checked)
                    {
                        eegGraph.PlotY(eegPlotData, (double)eegDownsample / (double)eegSamplingRate, (double)eegDownsample / (double)eegSamplingRate, true);
                    }

                    //Setup next callback
                    eegReader.BeginReadInt16(eegBufferLength, eegCallback, eegReader);
                }
            }
            catch (DaqException exception)
            {
                //Display Errors
                MessageBox.Show(exception.Message);
                reset();
            }
        }
        private void setupEEGOffset()
        {
            eegOffset = new double[Convert.ToInt32(comboBox_eegNumChannels.SelectedItem)];
            double rangeHigh;
            rangeHigh = eegTask.AIChannels.All.RangeHigh;
            for (int i = 0; i < Convert.ToInt32(comboBox_eegNumChannels.SelectedItem); ++i)
                eegOffset[i] = 2 * i * rangeHigh;
        }
        #endregion

        // Aux Data Aquisition
        #region Aux Data Aquisition

        private void AuxAnalogFromStimData(double[,] combinedAnalogData)
        {
            // Create space for the buffer
            auxAnData = new double[auxChanSet.numericalChannels.Length, spikeBufferLength];

            // Pull out the correct channels
            Array.Copy(combinedAnalogData, auxChanSet.numericalChannels[0] * spikeBufferLength, auxAnData,
                0, auxChanSet.numericalChannels.Length * spikeBufferLength);

            // Send to datSrv
            datSrv.AuxAnalogSrv.WriteToBuffer(auxAnData, 0, numChannels);

            //Write to file in format [numChannels numSamples]
            #region Write aux file
            if (switch_record.Value && recordingSettings.recordAuxAnalog)
            {
                short[,] shortAuxAnData = new short[auxChanSet.numericalChannels.Length, spikeBufferLength];
                shortAuxAnData = auxDataScaler.ConvertSoftRawMatixToInt16(ref auxAnData);
                recordingSettings.auxAnalogOut.read(shortAuxAnData, auxChanSet.numericalChannels.Length, 0, spikeBufferLength);
            }

            if (updateAuxGraph)
            {
                ;
                auxInputGraphController.updateScatterGraph(datSrv.AuxAnalogSrv,
                        slide_AnalogDispWidth.Value,
                        slide_AnalogDispMaxVoltage.Value,
                        slide_AuxShift.Value);
            }
            #endregion
        }

        private void AnalogInCallback_AuxAn(IAsyncResult ar)
        {
            // Only called when there is no other AI task on aux in board
            lock (this)
            {
                try
                {
                    if (taskRunning)
                    {
                        // Create space for the buffer
                        auxAnData = new double[auxChanSet.numericalChannels.Length, spikeBufferLength];

                        //Read the available data from the channels
                        int numAuxSampRead = 0;
                        auxAnData = auxAnReader.EndMemoryOptimizedReadMultiSample(ar, out numAuxSampRead);

                        // Send to datSrv
                        datSrv.AuxAnalogSrv.WriteToBuffer(auxAnData, 0, numChannels);

                        //Write to file in format [numChannels numSamples]
                        #region Write aux file
                        if (switch_record.Value && recordingSettings.recordAuxAnalog)
                        {
                            short[,] shortAuxAnData = new short[auxChanSet.numericalChannels.Length, spikeBufferLength];
                            shortAuxAnData = auxDataScaler.ConvertSoftRawMatixToInt16(ref auxAnData);
                            recordingSettings.auxAnalogOut.read(shortAuxAnData, auxChanSet.numericalChannels.Length, 0, spikeBufferLength);
                        }
                        #endregion

                        if (updateAuxGraph && !checkBox_FreezeAuxPlot.Checked)
                        {
                            auxInputGraphController.updateScatterGraph(datSrv.AuxAnalogSrv,
                                    slide_AnalogDispWidth.Value,
                                    slide_AnalogDispMaxVoltage.Value,
                                    slide_AuxShift.Value);
                        }

                        // Start next read
                        auxAnReader.BeginMemoryOptimizedReadMultiSample(spikeBufferLength, auxAnCallback, null, auxAnData);
                    }
                }
                catch (DaqException exception)
                {
                    //Display Errors
                    MessageBox.Show(exception.Message);
                    reset();
                }
            }
        }

        private void AnalogInCallback_AuxDig(IAsyncResult ar)
        {
            lock (this)
            {
                try
                {
                    if (taskRunning)
                    {
                        //Read the available data from the channels
                        auxDigData = auxDigReader.EndReadMultiSamplePortUInt32(ar);
                        trackingDigReads++;

                        //Find changes in digital state
                        for (int i = 0; i < spikeBufferLength; ++i)
                        {
                            if (auxDigData[i] != lastDigState)
                            {
                                //int dt = DateTime.Now.Millisecond;
                                lastDigState = auxDigData[i];

                                // Create Digital data
                                DigitalPortEvent thisPortEvent = new DigitalPortEvent((ulong)i, lastDigState);
                                EventBuffer<DigitalPortEvent> thisDigitalEventBuffer = new EventBuffer<DigitalPortEvent>(Properties.Settings.Default.RawSampleFrequency);
                                thisDigitalEventBuffer.eventBuffer.Add(thisPortEvent);

                                // send to datSrv
                                datSrv.AuxDigitalSrv.WriteToBufferRelative(thisDigitalEventBuffer, 0);

                                if (switch_record.Value && recordingSettings.recordAuxDig)
                                {
                                    recordingSettings.auxDigitalOut.write(i, lastDigState, trackingDigReads, spikeBufferLength);
                                }

                                // Update led array
                                bool[] boolLEDState = new bool[32];
                                var ledState = new BitArray(new int[] { (int)auxDigData[i] });
                                for (int j = 0; j < 32; j++)
                                    boolLEDState[j] = ledState[j];
                                ledArray_DigitalState.SetValues(boolLEDState, 0, 32);
                            }
                        }

                        // Start next read
                        auxDigReader.BeginReadMultiSamplePortUInt32(spikeBufferLength, auxDigCallback, auxDigReader);
                    }
                }
                catch (DaqException exception)
                {
                    //Display Errors
                    MessageBox.Show(exception.Message);
                    reset();
                }
            }
        }

        #endregion


    }
}