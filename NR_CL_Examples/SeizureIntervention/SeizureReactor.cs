using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.NeuroRighterTask;
using NeuroRighter.Server;
using NeuroRighter.DataTypes;

namespace NR_CL_Examples
{
    class SeizureReactor : NRTask
    {
        // Parameters
        string triggerStatistic = "LineLength"; // "RMS"  "IISRate"  "LineLength"
        double standardSettlingTime_Sec = 0;
        double statBufferHistory_Sec = 10;
        double statEstimationTau_Sec = 1;
        double thresholdEstimationTau_Sec = 60;
        double fractionChannelsReq = 0.25;
        double stimTimeOut_Sec = 10;
        double postStimPause_Sec = 5;

        # region Internal Variables

        // Internal data buffers
        internal List<LFPChannel> lfp;
        
        // UI
        SeizureControlPanel SCUI;
        double minUpdatePeriodSec = 0.05;
        double maxUpdateRateHz;
        double lastUpdateSec = 0;
        public delegate void UIEventHander(object o, EventArgs e);

        // Internal
        ulong lastSampleRead = 0;
        double clockTickSec;
        double startTime;
        double stimulationStartTime;
        double stimulationEndTime;
        bool startStimulationSequence = false;
        bool stimulationTimeOut = false;
        bool updatePause = false;
        List<int> stimChannels;
        int stimChannelIndex = 0;
        double stimPhaseSec = 0.0004; //sec
        double preStimSec = 0.0001; //sec
        public ulong nextStimSample;
        double[] stimWaveform;
        ulong nextAvailableSample;
        ulong isi;
        Random r = new Random();

        // File writing
        FileWriter seizOut;

        # endregion

        protected override void Setup()
        {
            // Mark start time
            startTime = NRStimSrv.StimOut.GetTime() / 1000;

            // Create UI
            SCUI = new SeizureControlPanel(1 / NRStimSrv.DACPollingPeriodSec, statBufferHistory_Sec, triggerStatistic);
            SCUI.Show();

            // max update rate
            maxUpdateRateHz = 1 / minUpdatePeriodSec;

            // Create room for lfp rms estimationd data
            lfp = new List<LFPChannel>();
            List<int> allChannels = new List<int>();
            for (int i = 0; i < NRDataSrv.LFPSrv.ChannelCount; i++)
            {
                lfp.Add(new LFPChannel(
                    i+1,
                    NRDataSrv.LFPSrv.SampleFrequencyHz,
                    1 / NRStimSrv.DACPollingPeriodSec,
                    triggerStatistic,
                    standardSettlingTime_Sec,
                    statBufferHistory_Sec,
                    statEstimationTau_Sec,
                    thresholdEstimationTau_Sec,
                    SCUI.ThresholdK));
                allChannels.Add(i);
            }

            // Random stimulus channels
            stimChannels = new List<int>();
            for (int i = 0; i < NRDataSrv.LFPSrv.ChannelCount; i++)
            {
                int idx = r.Next(allChannels.Count);
                stimChannels.Add(allChannels[idx]);
                allChannels.Remove(idx);
            }

            // Make the stimulus waveform
            MakeStimWaveform();

            // Create file writer
            if (NRRecording)
                seizOut = new FileWriter(NRFilePath + ".seiz", NRDataSrv.LFPSrv.ChannelCount*2 + 1, NRDataSrv.LFPSrv.SampleFrequencyHz, triggerStatistic);
        }

        protected override void Loop(object sender, EventArgs e)
        {
            // Update run time
            clockTickSec = NRStimSrv.StimOut.GetTime() / 1000 - startTime;

            if (!startStimulationSequence || stimulationTimeOut)
            {
                // Update test statistics, make comparison to standard stats to predict oncoming seizure
                if (stimulationEndTime == 0 || clockTickSec - stimulationEndTime > postStimPause_Sec)
                {
                    updatePause = false;
                    UpdateStatistics();

                }
                //else
                //{
                //    updatePause = true;
                //}

                // Generate Output based on results
                if (SCUI.FBEngaged && !stimulationTimeOut)
                    CheckCommenceStimulation();
            }
            else
            {
                // Apply stimulation for a pre-specified length of time
                if (clockTickSec - stimulationStartTime < SCUI.StimTimeSec)
                    ApplyStimuli();
                else
                    StopStimulation();
            }

            // Check to see if post stim time out has passed
            if (stimulationTimeOut)
                CheckStimulationUnlock();

            // Update UI
            if (clockTickSec - lastUpdateSec > minUpdatePeriodSec)
            {
                lastUpdateSec = clockTickSec;
                SCUI.UpdatePlots(lfp, startStimulationSequence || updatePause, maxUpdateRateHz);
            }

            // Write the output to file
            Write2File();
        }

        protected override void Cleanup()
        {
            SCUI.Close();
            if( seizOut != null)
                seizOut.Close();
        }

        #region Private methods

        private void UpdateStatistics()
        {
            // First, figure out what history of spikes we have
            ulong[] lfpTimeRange = NRDataSrv.LFPSrv.EstimateAvailableTimeRange();

            if (lfpTimeRange[1] > lastSampleRead)
            {
                RawMultiChannelBuffer newLFP = NRDataSrv.LFPSrv.ReadFromBuffer(lastSampleRead, lfpTimeRange[1]);

                // Update the last sample read
                lastSampleRead = lfpTimeRange[1];

                // Update internal lfp channels
                for (int i = 0; i < newLFP.Buffer.Length; i++)
                    lfp[i].Update(newLFP.Buffer[i], newLFP.StartAndEndSample[0], newLFP.StartAndEndSample[1]);

                // Update statistic standards for comparison or actually make comparison
                if (clockTickSec > standardSettlingTime_Sec)
                    if (!SCUI.RetrainRequired)
                    {
                        for (int i = 0; i < newLFP.Buffer.Length; i++)
                        {
                            lfp[i].ThesholdCoefficient = SCUI.ThresholdK;
                            lfp[i].UpdateStandard(newLFP.Buffer[i], newLFP.StartAndEndSample[0], newLFP.StartAndEndSample[1]);
                            lfp[i].Compare();
                        }
                    }
                    else
                    {
                        ResetThresh();
                    }

            }

        }

        private void CheckCommenceStimulation()
        {
            int consensus = 0;
            for (int i = 0; i < lfp.Count; i++)
                if (lfp[i].Triggered)
                    consensus++;
            if ((double)consensus / (double)lfp.Count > fractionChannelsReq)
            {
                // Mark the stimulations start time
                stimulationStartTime = clockTickSec;
                startStimulationSequence = true;
                
                // Advance read position to skip stimulation stuff
                lastSampleRead += (ulong)Math.Ceiling(NRDataSrv.LFPSrv.SampleFrequencyHz * (SCUI.StimTimeSec + postStimPause_Sec));
            }
        }

        private void StopStimulation()
        {
            // Mark the stimulation stop time
            stimulationEndTime = clockTickSec;
            startStimulationSequence = false;

            // Stimulation time out starts
            stimulationTimeOut = true;
            updatePause = true;
        }

        private void CheckStimulationUnlock()
        {
            if (clockTickSec - stimulationEndTime > stimTimeOut_Sec)
                stimulationTimeOut = false;
        }

        private void ApplyStimuli()
        {
            // Get the inter-stimulus-interval
            isi = (ulong)(NRStimSrv.SampleFrequencyHz / SCUI.StimFreqHz );

            // Update the next sample for the new isi 
            nextStimSample += isi;

            // Get the current buffer sample and make sure that we are going
            // to produce stimuli that are in the future
            ulong currentLoad = NRStimSrv.StimOut.GetNumberBuffLoadsCompleted();
            nextAvailableSample = (currentLoad + 1) * (ulong)NRStimSrv.GetBuffSize();
            if (nextStimSample < nextAvailableSample)
            {
                nextStimSample = nextAvailableSample;
            }

            // Create the output buffer
            List<StimulusOutEvent> toAppendStim = new List<StimulusOutEvent>();

            // Send output
            while (nextStimSample <= (nextAvailableSample + (ulong)NRStimSrv.GetBuffSize()))
            {
                // Which channel are we stimulating on
                stimChannelIndex = stimChannelIndex % stimChannels.Count;

                // create stimuli
                toAppendStim.Add(new StimulusOutEvent(stimChannels[stimChannelIndex], nextStimSample, stimWaveform));

                //simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                nextStimSample += isi;

                // Cycle stimulation channel
                stimChannelIndex++;
            }

            // Send stimuli
            if (toAppendStim.Count > 0)
                NRStimSrv.StimOut.WriteToBuffer(toAppendStim);

        }

        private void MakeStimWaveform()
        {
            // Make stimulus waveform
            int preSamp = (int)(preStimSec * NRStimSrv.SampleFrequencyHz);
            int phaseSamp = (int)(stimPhaseSec * NRStimSrv.SampleFrequencyHz);
            int wavelength = (2 * preSamp + 2 * phaseSamp);
            stimWaveform = new double[wavelength];

            for (int i = 0; i < wavelength; i++)
            {
                if (i < preSamp)
                    stimWaveform[i] = 0.0;
                else if (i >= preSamp && i < preSamp + phaseSamp)
                    stimWaveform[i] = SCUI.StimAmpVolts;
                else if (i >= preSamp + phaseSamp && i < preSamp + 2 * phaseSamp)
                    stimWaveform[i] = -SCUI.StimAmpVolts;
                else
                    stimWaveform[i] = 0.0;
            }
        }

        private void ResetThresh()
        {
            for (int i = 0; i < lfp.Count; i++)
            {       
                lfp[i].statStandard.Reset(lfp[i].stat.States);
            }

            SCUI.RetrainRequired = false;
        }

        private void Write2File()
        {
            if (NRRecording)
            {
                // Make the datum array
                double[] datum = new double[seizOut.numStreams];
                for (int i = 0; i < lfp.Count; i++)
                {
                    datum[i] = lfp[i].stat.BaseStat;
                }
                for (int i = 0; i < lfp.Count; i++)
                {
                    datum[lfp.Count + i] = SCUI.ThresholdK*lfp[i].statStandard.BaseStat;
                }

                if (startStimulationSequence)
                    datum[2 * lfp.Count] = 1.0;
                else
                    datum[2 * lfp.Count] = 0.0;
  

                // Write data to file
                seizOut.WriteData(clockTickSec, datum);

            }
        }

        #endregion

    }
}
