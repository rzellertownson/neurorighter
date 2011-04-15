// EPHYSWRITE.CS
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

//#define USE_LOG_FILE
//#define DEBUG

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
using NeuroRighter.SpikeDetection;


namespace NeuroRighter

{
    ///<summary>Methods for processing raw data streams. This includes filtering (bandpass and SALPA) and the creation of EEG,LFP and MUA data streams
    ///and, if appropriate, sending those raw streams to file.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {



        private void bwSpikes_DoWork(object sender, DoWorkEventArgs e)
        {

            Object[] state = (Object[])e.Argument;
            int taskNumber = (int)state[0];
            trackingProc[taskNumber]++;
            //Copy data into a new buffer
            for (int i = 0; i < numChannelsPerDev; ++i)
                spikeData[taskNumber][i].GetRawData(0, spikeBufferLength, filtSpikeData[taskNumber * numChannelsPerDev + i], 0);

            //Account for Pre-amp gain
            double ampdec = (1 / Properties.Settings.Default.PreAmpGain);
            for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                for (int j = 0; j < spikeBufferLength; ++j)
                    filtSpikeData[i][j] = ampdec * filtSpikeData[i][j];

            #region Write RAW data
            //Write data to file
            if (switch_record.Value && recordingSettings.recordRaw && spikeTask != null)
            {
                
                lock (this)
                {

                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    {
                        // Temporary storage for converte data
                        Int16[] tempBuff;
                        
                        // Convert raw data to 16-bit int
                        tempBuff = neuralDataScaler.ConvertSoftRawToInt16(ref filtSpikeData[i]);

                        // Send data to file writer
                        for (int j = 0; j < spikeBufferLength; ++j)
                        {
                            recordingSettings.rawOut.read(tempBuff[j], i);
                        }
                    }
                }
            }

            #endregion

            #region LFP_Filtering
            //Filter for LFPs
            if (!Properties.Settings.Default.SeparateLFPBoard && Properties.Settings.Default.UseLFPs)
            {
                //Copy to new array
                for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                        filtLFPData[i][j] = filtSpikeData[i][j];

                #region ArtiFilt (interpolation filtering)
                if (checkBox_artiFilt.Checked)
                    artiFilt.filter(ref filtLFPData, stimIndices, taskNumber * numChannelsPerDev, numChannelsPerDev, numStimReads[taskNumber] - 1);
                #endregion

                if (checkBox_LFPsFilter.Checked)
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        lfpFilter[i].filterData(filtLFPData[i]);

                //Downsample for LFPs
                double dsFactor = (double)spikeSamplingRate / (double)lfpSamplingRate;
                if (dsFactor % 1 == 0) //If it's an integer
                {
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] = filtLFPData[i][(int)(dsFactor * j)];
                }
                else
                {
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] = filtLFPData[i][(int)(Math.Round(dsFactor * j))];
                }

                //Do IISZapper stuff
                if (IISDetected != null) IISDetected(this, finalLFPData, numSpikeReads[taskNumber]);

                #region WriteLFPFile
                if (switch_record.Value && recordingSettings.recordLFP && spikeTask != null) //Convert to 16-bit ints, then write to file
                {
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    {
                        // Temporary storage for converte data
                        Int16[] tempLFPBuff;

                        // Convert raw data to 16-bit int
                        tempLFPBuff = neuralDataScaler.ConvertSoftRawToInt16(ref finalLFPData[i]);

                        // Send data to file writer
                        for (int j = 0; j < lfpBufferLength; ++j)
                        {
                            recordingSettings.lfpOut.read((short)tempLFPBuff[j], i);
                        }
                    }
    
                }
                #endregion

                //Digital ref LFP signals
                if (!checkBox_digRefLFPs.Checked) { /* Do nothing, since prefetch makes if faster than else */ }
                else
                {
                    int refChan = Convert.ToInt16(numericUpDown_digRefLFPs.Value) - 1;
                    for (int i = 0; i < refChan; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] -= finalLFPData[refChan][j];
                    for (int i = refChan + 1; i < numChannels; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                            finalLFPData[i][j] -= finalLFPData[refChan][j];
                }

                //Post to PlotData buffer
                lfpPlotData.write(finalLFPData, taskNumber * numChannelsPerDev, numChannelsPerDev);
            }
            #endregion

            #region SALPA Filtering
            if (checkBox_SALPA.Checked && numStimReads == null) //Account for those not using the stimulator and stimulus coding scheme
            {
                SALPAFilter.filter(ref filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev, thrSALPA, stimIndices, 0);
            }
            else if (checkBox_SALPA.Checked)
            {
                SALPAFilter.filter(ref filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev, thrSALPA, stimIndices, numStimReads[taskNumber] - 1);
            }

            if (switch_record.Value && recordingSettings.recordSALPA && spikeTask != null)
            {
                lock (this)
                {
                    int startIdx;

                    if (firstRawWrite[taskNumber]) // account for SALPA delay
                    {
                        if (!recordingSettings.recordSpikeFilt)
                            firstRawWrite[taskNumber] = false;
                        startIdx = 2 * SALPA_WIDTH;
                    }
                    else
                    {
                        startIdx = 0;
                    }

                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    {
                        // Temporary storage for converte data
                        Int16[] tempBuff;

                        // Convert raw data to 16-bit int
                        tempBuff = neuralDataScaler.ConvertSoftRawToInt16(ref filtSpikeData[i]);

                        // Send data to file writer
                        for (int j = startIdx; j < spikeBufferLength; ++j)
                        {
                            recordingSettings.salpaOut.read((short)tempBuff[j], i);
                        }
                    }
                }
            }

            #endregion SALPA Filtering

            #region SpikeFiltering
            //Filter spike data
            if (checkBox_spikesFilter.Checked)
            {

                    for (int i = numChannelsPerDev * taskNumber; i < numChannelsPerDev * (taskNumber + 1); ++i)
                        spikeFilter[i].filterData(filtSpikeData[i]);
                    lock (this)
                    {

                    if (switch_record.Value && recordingSettings.recordSpikeFilt && spikeTask != null)
                    {

                        int startIdx;
                        if (firstRawWrite[taskNumber] && checkBox_SALPA.Checked) // account for SALPA delay
                        {
                            firstRawWrite[taskNumber] = false;
                            startIdx = 2 * SALPA_WIDTH;
                        }
                        else
                        {
                            startIdx = 0;
                        }

                        for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        {
                            // Temporary storage for converte data
                            Int16[] tempBuff;

                            // Convert raw data to 16-bit int
                            tempBuff = neuralDataScaler.ConvertSoftRawToInt16(ref filtSpikeData[i]);

                            // Send data to file writer
                            for (int j = startIdx; j < spikeBufferLength; ++j)
                            {
                                recordingSettings.spkFiltOut.read((short)tempBuff[j], i);
                            }
                        }
                    }
                }


            }
            #endregion

            //NEED TO FIX FOR MULTI DEVS
            #region Digital_Referencing_Spikes
            //Digital ref spikes signals
            if (checkBox_digRefSpikes.Checked)
            {
                int refChan = Convert.ToInt16(numericUpDown_digRefSpikes.Value) - 1;
                for (int i = 0; i < refChan; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                        filtSpikeData[i][j] -= filtSpikeData[refChan][j];
                for (int i = refChan + 1; i < numChannels; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                        filtSpikeData[i][j] -= filtSpikeData[refChan][j];
            }

            //Common average or median referencing
            if (referncer != null)
            {
                lock (this)
                    referncer.reference(filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev);
            }
            #endregion

            #region SpikeDetection
            ++(numSpikeReads[taskNumber]);

            SALPA_WIDTH = Convert.ToInt32(numericUpDown_salpa_halfwidth.Value);

            int startTime = (numSpikeReads[taskNumber] - 1) * spikeBufferLength; //Used to mark spike time for *.spk file
            if (checkBox_SALPA.Checked)
                startTime -= 2 * SALPA_WIDTH; //To account for delay of SALPA filter

            List<SpikeWaveform> newWaveforms = new List<SpikeWaveform>(100);
            for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                newWaveforms.AddRange(spikeDet.spikeDetector.DetectSpikes(filtSpikeData[i], i));

            //Extract waveforms
            if (Properties.Settings.Default.ChannelMapping != "invitro" || numChannels != 64) //check this first, so we don't have to check it for each spike
            {
                for (int j = 0; j < newWaveforms.Count; ++j) //For each threshold crossing
                {
                    #region WriteSpikeWfmsToFile
                    rawType[] waveformData = newWaveforms[j].waveform;
                    if (switch_record.Value)
                    {
                        lock (recordingSettings) //Lock so another NI card doesn't try writing at the same time
                        {
                            recordingSettings.spkOut.WriteSpikeToFile((short)(newWaveforms[j].channel + CHAN_INDEX_START), startTime + newWaveforms[j].index,
                                newWaveforms[j].threshold, waveformData);
                        }
                    }
                    #endregion
                }
            }
            else //in vitro mappings
            {
                for (int j = 0; j < newWaveforms.Count; ++j) //For each threshold crossing
                {
                    #region WriteSpikeWfmsToFile
                    rawType[] waveformData = newWaveforms[j].waveform;
                    if (switch_record.Value)
                    {
                        lock (recordingSettings) //Lock so another NI card doesn't try writing at the same time
                        {
                            recordingSettings.spkOut.WriteSpikeToFile((short)(MEAChannelMappings.channel2LinearCR(newWaveforms[j].channel) + CHAN_INDEX_START), startTime + newWaveforms[j].index,
                                newWaveforms[j].threshold, waveformData); 
                        }
                    }
                    #endregion
                }
            }

            //Post to PlotData
            waveformPlotData.write(newWaveforms);

            #region WriteSpikeWfmsToListeningProcesses
            //Alert any listening processes that we have new spikes.  It's up to them to clear wavefroms periodically.
            //That's definitely not the best way to do it.
            if (spikesAcquired != null)
            {
                lock (this)
                {
                    //Check to see if spikes are within trigger
                    for (int i = 0; i < newWaveforms.Count; ++i)
                    {
                        if (newWaveforms[i].index + startTime >= triggerStartTime && newWaveforms[i].index + startTime <= triggerStopTime)
                        {
                            _waveforms.Add(newWaveforms[i]);
#if (DEBUG1)
                            logFile.WriteLine("Waveform in trigger, index: " + newWaveforms[i].index);
#endif
                        }
                    }
                }
                spikesAcquired(this, inTrigger);
            }
            #endregion

            //Clear new ones, since we're done with them.
            newWaveforms.Clear();
            #endregion

            #region BNC_Output
            //Send selected channel to BNC
            if (Properties.Settings.Default.UseSingleChannelPlayback)
            {
                int ch = (int)(channelOut.Value) - 1;
                if (ch >= numChannelsPerDev * taskNumber && ch < numChannelsPerDev * (taskNumber + 1))
                    //spikeOutWriter.BeginWriteMultiSample(true, filtSpikeData[ch], null, null);
                    BNCOutput.write(filtSpikeData[ch]);
                //spikeOutWriter.WriteMultiSample(true, filtSpikeData[ch - 1]);
            }
            #endregion

            //Write to PlotData buffer
            spikePlotData.write(filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev);

            #region MUA
            if (Properties.Settings.Default.ProcessMUA)
            {
                muaFilter.Filter(filtSpikeData, taskNumber * numChannelsPerDev, numChannelsPerDev, ref muaData);

                //Write to plot buffer
                muaPlotData.write(muaData, taskNumber * numChannelsPerDev, numChannelsPerDev);
            }
            #endregion

            e.Result = taskNumber;
        }

        internal delegate void spikesAcquiredHandler(object sender, bool inTrigger);

        internal event spikesAcquiredHandler spikesAcquired;

        private void bwSpikes_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int taskNumber = (int)e.Result;

            //Check whether timed recording is done
            if (checkBox_enableTimedRecording.Checked && DateTime.Now > timedRecordingStopTime)
            {
                if (taskNumber == spikeReader.Count - 1)
                {
                    if (checkbox_repeatRecord.Checked)//make sure this is the last spike processor so that this only gets called once  -&& (((int)e.Result)==(bwSpikes.Count-1))
                    {

                        //if (taskRunning) reset();
                        buttonStop.PerformClick();
                        reset();
                        buttonStart.PerformClick();

                    }
                    else
                    {

                        //if (taskRunning) reset();
                        buttonStop.PerformClick();

                    }
                }
            }
            else
            {
                try
                {
                    spikeReader[taskNumber].BeginMemoryOptimizedReadWaveform(spikeBufferLength, spikeCallback, taskNumber, spikeData[taskNumber]);
                }
                catch (DaqException exception)
                {
                //    MessageBox.Show(exception.Message); //Display Errors
                    reset();
                }
            }
        }

    }
}
