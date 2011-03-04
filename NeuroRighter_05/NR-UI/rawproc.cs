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


namespace NeuroRighter

{

    ///<summary>Methods for processing raw data streams. This includes filtering (bandpass and SALPA) and the creation of EEG,LFP and MUA data streams.</summary>
    ///<author>John Rolston</author>
    sealed internal partial class NeuroRighter : Form
    {

        int[] trackingreads;
        int[] trackingproc;
        private void bwSpikes_DoWork(object sender, DoWorkEventArgs e)
        {

            Object[] state = (Object[])e.Argument;
            int taskNumber = (int)state[0];
            trackingproc[taskNumber]++;
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
            if (switch_record.Value && checkBox_SaveRawSpikes.Checked)
            {
                rawType oneOverResolution = Properties.Settings.Default.PreAmpGain * Int16.MaxValue / spikeTask[0].AIChannels.All.RangeHigh; //Resolution of 16-bit signal; multiplication is much faster than division
                rawType tempVal;
                for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                    for (int j = 0; j < spikeBufferLength; ++j)
                    {
                        //This next section deals with the fact that NI's range is soft--i.e., values can exceed the max and min values of the range (but trying to convert these to shorts would crash the program)
                        tempVal = Math.Round(filtSpikeData[i][j] * oneOverResolution);
                        if (tempVal <= Int16.MaxValue && tempVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                        else if (tempVal > Int16.MaxValue) { tempVal = Int16.MaxValue; }
                        else { tempVal = Int16.MinValue; }
                        rawFile.read((short)tempVal, i);
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
                if (switch_record.Value) //Convert to 16-bit ints, then write to file
                {
                    rawType oneOverResolution = Int16.MaxValue / spikeTask[0].AIChannels.All.RangeHigh; //Resolution of 16-bit signal; multiplication is much faster than division
                    rawType tempLFPVal;
                    for (int i = taskNumber * numChannelsPerDev; i < (taskNumber + 1) * numChannelsPerDev; ++i)
                        for (int j = 0; j < lfpBufferLength; ++j)
                        {
                            //This next section deals with the fact that NI's range is soft--i.e., values can exceed the max and min values of the range (but trying to convert these to shorts would crash the program)
                            tempLFPVal = Math.Round(finalLFPData[i][j] * oneOverResolution);
                            if (tempLFPVal <= Int16.MaxValue && tempLFPVal >= Int16.MinValue) { /*do nothing, most common case*/ }
                            else if (tempLFPVal > Int16.MaxValue) { tempLFPVal = Int16.MaxValue; }
                            else { tempLFPVal = Int16.MinValue; }
                            lfpFile.read((short)tempLFPVal, i);
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
            #endregion SALPA Filtering

            #region SpikeFiltering
            //Filter spike data
            if (checkBox_spikesFilter.Checked)
            {
                for (int i = numChannelsPerDev * taskNumber; i < numChannelsPerDev * (taskNumber + 1); ++i)
                    spikeFilter[i].filterData(filtSpikeData[i]);
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
                spikeDetector.detectSpikes(filtSpikeData[i], newWaveforms, i);


            #region SpikeValidation
            double Fs = Convert.ToDouble(textBox_spikeSamplingRate.Text);
            int numSamplesPeak = (int)Math.Ceiling(0.0005 * Fs); //Search the first half millisecond after thresh crossing      
            int numSamplesToSearch = (numPre + numPost + 1);

            if (checkBox_spikeValidation.Checked)
            {
                bool skipSecondValidation;

                lock (this)
                {
                    for (int w = 0; w < newWaveforms.Count; w++) //For each waveform
                    {
                        if (w < 0)
                            break;

                        //Ensure that first and last few samples aren't blanked (this happens with artifact suppressions sometimes)
                        if ((newWaveforms[w].waveform[0] <= newWaveforms[w].waveform[1] + VOLTAGE_EPSILON && newWaveforms[w].waveform[0] >= newWaveforms[w].waveform[1] - VOLTAGE_EPSILON &&
                            newWaveforms[w].waveform[1] <= newWaveforms[w].waveform[2] + VOLTAGE_EPSILON && newWaveforms[w].waveform[1] >= newWaveforms[w].waveform[2] - VOLTAGE_EPSILON) ||
                            (newWaveforms[w].waveform[numPost + numPre] <= newWaveforms[w].waveform[numPost + numPre - 1] + VOLTAGE_EPSILON &&
                            newWaveforms[w].waveform[numPost + numPre] >= newWaveforms[w].waveform[numPost + numPre - 1] - VOLTAGE_EPSILON &&
                            newWaveforms[w].waveform[numPost + numPre - 1] <= newWaveforms[w].waveform[numPost + numPre - 2] + VOLTAGE_EPSILON &&
                            newWaveforms[w].waveform[numPost + numPre - 1] >= newWaveforms[w].waveform[numPost + numPre - 2] - VOLTAGE_EPSILON))
                        {
                            newWaveforms.RemoveAt(w);
                            --w;
                            continue;
                        }

                        //Find peak
                        double maxVal = 0.0;
                        for (int k = numPre; k < numPre + numSamplesPeak; ++k)
                        {
                            if (Math.Abs(newWaveforms[w].waveform[k]) > maxVal)
                                maxVal = Math.Abs(newWaveforms[w].waveform[k]);
                        }
                        double artThresh;
                        //Search pts. after the detected peak for other very significant peaks, disqualifying if there are larger peaks
                        try
                        {
                            artThresh = Convert.ToDouble(textBox_AbsArtThresh.Text);
                        }
                        catch
                        {
                            artThresh = 1000;
                        }

                        if (maxVal > 1e-6 * artThresh)
                        {
                            newWaveforms.RemoveAt(w);
                            --w;
                            continue;
                        }
                        else
                        {
                            skipSecondValidation = false;

                            for (int k = numSamplesPeak + numPre; k < numSamplesToSearch; ++k)
                            {

                                if (Math.Abs(newWaveforms[w].waveform[k]) > 0.9 * maxVal)
                                {
                                    newWaveforms.RemoveAt(w);
                                    --w;
                                    skipSecondValidation = true;
                                    break;
                                }

                            }

                            if (!skipSecondValidation)
                            {
                                for (int k = 0; k < numPre; ++k)
                                {

                                    if (Math.Abs(newWaveforms[w].waveform[k]) > 0.9 * maxVal)
                                    {
                                        newWaveforms.RemoveAt(w);
                                        --w;
                                        break;
                                    }

                                }
                            }
                        }


                    }
                }
            }
            #endregion

            //Extract waveforms
            if (Properties.Settings.Default.ChannelMapping != "invitro" || numChannels != 64) //check this first, so we don't have to check it for each spike
            {
                for (int j = 0; j < newWaveforms.Count; ++j) //For each threshold crossing
                {
                    #region WriteSpikeWfmsToFile
                    rawType[] waveformData = newWaveforms[j].waveform;
                    if (switch_record.Value)
                    {
                        lock (fsSpks) //Lock so another NI card doesn't try writing at the same time
                        {
                            fsSpks.WriteSpikeToFile((short)(newWaveforms[j].channel + CHAN_INDEX_START), startTime + newWaveforms[j].index,
                                newWaveforms[j].threshold, waveformData);// JN +1 in channel field switches to 1-based channel numbering
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
                        lock (fsSpks) //Lock so another NI card doesn't try writing at the same time
                        {
                            fsSpks.WriteSpikeToFile((short)(MEAChannelMappings.channel2LinearCR(newWaveforms[j].channel) + CHAN_INDEX_START), startTime + newWaveforms[j].index,
                                newWaveforms[j].threshold, waveformData); // JN +1 in channel field switches to 1-based channel numbering
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
                    MessageBox.Show(exception.Message); //Display Errors
                    reset();
                }
            }
        }

    }
}
