using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter;
using NeuroRighter.NeuroRighterTask;
using NeuroRighter.Server;
using NeuroRighter.DataTypes;


namespace NR_CL_Examples
{
    /// <summary>
    /// Port of Daniel Wagenaars multielectrode burst quieting alogorithm (SIMOC). Written by Jon Newman, Georgia Tech.
    /// </summary>
    public class MEAStim_UNFRControl_Perfusion : NRTask
    {
        // DEBUG
        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Jon\Desktop\NR-CL-Examples\internal-asdr.txt", false);

        // Number of units 
        int numberOfDetectedUnits = 43;

        // cl timing start
        double startTime;
        double fTarget = 3; // target firing rate (Hz)
        double clStartSec = 3600; // seconds before tracking set point
        double clTrackSec = 6 * 3600;// seconds of setpoint step

        // Perfusion timing
        double perufusionStartTimeSec = 500 * 3600;
        double pumpOnTimeSec = 300;

        // algorithm parameters
        int[] stimElectrodes = { 52, 53, 23, 50, 20, 44, 19, 10, 21, 5 };
        double tau = 5; // FB time constant, sec
        double totalStimFreq = 10; // Hz
        double smoothingPeriodSec = 2; // sec
        double stimPhaseSec = 0.0004; //sec
        double preStimSec = 0.0001; //sec
        int averagingHistory = 20; // # cycles required before responses are dequeued
        double maxStimVoltage = 0.9; //volts
        double minStimVoltage = 0.001; //Volts
        double totalStimPeriodSec;

        # region state variables
        // cl variables

        static readonly object lockObj = new object();

        double stimVoltage; // volts
        double epsilon;
        double[] secSinceLastStimChannel;
        double[][] lastChannelResponses;
        double[] channelVoltage;
        int[] stimulusIndex;
        bool[] indexRequiresUpdate;
        double[] tuningFactor;
        double[] tuningFactorNorm;

        // time variables
        Queue<double> tickBuffer = new Queue<double>();
        Queue<double> unfrBuffer = new Queue<double>();
        int bufferLength;
        double clockTickSec;
        double proTime;
        ulong nextAvailableSample;
        ulong isi;

        // Pump variables
        bool pumpIsOn = false;

        // internal variables
        double currentTarget = 0;
        ulong lastSampleRead = 0;
        int stimChannel;
        public ulong nextStimSample;
        Random r = new Random();
        # endregion

        protected override void Setup()
        {
            // Set up closed loop algorithm
            startTime = NRStimSrv.StimOut.GetTime() / 1000;
            Console.WriteLine("CLOSED LOOP MEA STIMULATION starting out at time " + startTime.ToString() + " seconds.");

            // How fast will we be stimulating each electrode
            totalStimPeriodSec = 1 / totalStimFreq;

            // Initialize queues
            tickBuffer.Enqueue(0);
            unfrBuffer.Enqueue(0);

            // caclculate unitless FB time constant
            epsilon = NRStimSrv.DACPollingPeriodSec / tau;

            // Calculate how long our averaging window is
            bufferLength = (int)Math.Ceiling(smoothingPeriodSec / NRStimSrv.DACPollingPeriodSec);

            // Refresh all feedback variables
            RefreshFeedbackVar();

            // How long is a full protocol
            proTime = clStartSec + clTrackSec;
        }

        protected override void Loop(object sender, EventArgs e)
        {
            try
            {
                lock (lockObj)
                {
                    // Update run time
                    clockTickSec = NRStimSrv.StimOut.GetTime() / 1000 - startTime;

                    // First, we grab the new spike data and estimate the chosen observable
                    MakeASDRObservation();

                    // Update current timing for output
                    CalculateOutputTime();

                    // Encode the current state using DO
                    EncodeDigInfo();

                    // Apply CL Stimulation
                    if (clockTickSec > clStartSec && clockTickSec <= proTime)
                    {
                        currentTarget = fTarget;

                        CreateFeedback();

                        // Fine tune the feedback signal
                        UpdateFineTuning();
                        //}

                        // Apply electrical feedback
                        ApplyEStimFeedback();
                    }
                    else
                    {
                        currentTarget = 0;
                    }

                    // Should we turn the pump on?
                    if (clockTickSec > perufusionStartTimeSec && clockTickSec <= perufusionStartTimeSec + pumpOnTimeSec)
                    {
                        pumpIsOn = true;
                    }
                    else
                    {
                        pumpIsOn = false;
                    }

                    // Step back the current stim time
                    StepBackISI();
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("In BuffLoadEvent: \r\r" + ex.Message);
            }
        }

        protected override void Cleanup()
        {
            Console.WriteLine("Terminating protocol...");
        }

        private void MakeASDRObservation()
        {
            // First, figure out what history of spikes we have
            ulong[] spikeTimeRange = NRDataSrv.SpikeSrv.EstimateAvailableTimeRange();

            if (spikeTimeRange[1] > Math.Pow(10, 18))
            {
                Console.WriteLine("Warning - absurd current tick result");
                return;
            }

            // Allocate space for new spikes
            EventBuffer<SpikeEvent> newSpikes = new EventBuffer<SpikeEvent>(NRDataSrv.SpikeSrv.SampleFrequencyHz);

            // Do is there any new data yet?
            if (spikeTimeRange[1] > lastSampleRead)
            {
                // Try to get the number of spikes within the available time range
                newSpikes = NRDataSrv.SpikeSrv.ReadFromBuffer(lastSampleRead, spikeTimeRange[1]);

                // Update the last sample read
                lastSampleRead = spikeTimeRange[1];
            }

            double thisTick = tickBuffer.Last() + NRStimSrv.DACPollingPeriodSec;
            double thisUNFR = (double)newSpikes.Buffer.Where(x => x.Unit != 0).Count() / (double)numberOfDetectedUnits / NRStimSrv.DACPollingPeriodSec;

            // Estimate the ASDR
            tickBuffer.Enqueue(thisTick);
            unfrBuffer.Enqueue(thisUNFR);

            // Get rid of old data
            if (unfrBuffer.Count > bufferLength)
            {
                tickBuffer.Dequeue();
                unfrBuffer.Dequeue();
            }

        }

        private void CreateFeedback()
        {
            // Recursive Voltage calculation
            double unfrSmooth = unfrBuffer.Average();
            stimVoltage = stimVoltage * (1 - (epsilon * ((unfrSmooth / currentTarget) - 1)));

            // Anti-windup
            if (stimVoltage > maxStimVoltage)
                stimVoltage = maxStimVoltage;
            if (stimVoltage < minStimVoltage)
                stimVoltage = minStimVoltage;
        }

        private void UpdateFineTuning()
        {

            // For each channel, check if it has been less than x msec since last the stimulus, 
            // if so, update the ASDR estimate for stimulating on that channel.
            for (int chan = 0; chan < stimElectrodes.Length; chan++)
            {
                if (secSinceLastStimChannel[chan] < totalStimPeriodSec)
                {
                    // Update post stimulus firing rate for this channel
                    lastChannelResponses[chan][stimulusIndex[chan]] += unfrBuffer.Last(); // not normalized
                    indexRequiresUpdate[chan] = true;
                }
                else if (indexRequiresUpdate[chan])
                {
                    // Update stimulus index for this channel
                    stimulusIndex[chan]++;
                    stimulusIndex[chan] = stimulusIndex[chan] % averagingHistory;
                    indexRequiresUpdate[chan] = false;

                    // Update fine tuning factors
                    if (lastChannelResponses[chan].Average() != 0)
                    {
                        // What was the average post-stimulus response on this channel
                        double avgResponse = lastChannelResponses[chan].Average();

                        // Calculate fine tuning factor
                        tuningFactor[chan] = 1 / avgResponse;

                    }

                }

                secSinceLastStimChannel[chan] += NRStimSrv.DACPollingPeriodSec;
            }

            // Normalize tuning factor and update stim voltages
            List<double> tFA = new List<double>();
            for (int chan = 0; chan < stimElectrodes.Length; chan++)
            {
                // Only include non-saturated channels in tuning factor average
                if (channelVoltage[chan] < maxStimVoltage)
                    tFA.Add(tuningFactor[chan]);
            }

            for (int chan = 0; chan < stimElectrodes.Length; chan++)
            {
                // Normalize tuning factors s.t. average tuning factor is 1.0
                if (tFA.Count > 0)
                    tuningFactorNorm[chan] = tuningFactor[chan] / tFA.Average();
                else
                    tuningFactorNorm[chan] = 1.0;

                // Update stimulation voltage for this channel
                channelVoltage[chan] = stimVoltage * tuningFactorNorm[chan];

                //Make sure that modified channelVoltage does not exceed maxima
                if (channelVoltage[chan] < 0)
                    channelVoltage[chan] = 0.0;
                else if (channelVoltage[chan] > maxStimVoltage)
                    channelVoltage[chan] = maxStimVoltage;

            }

        }

        private void CalculateOutputTime()
        {
            // Get the inter-stimulus-interval
            isi = (ulong)(NRStimSrv.SampleFrequencyHz / totalStimFreq);

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
        }

        private void EncodeDigInfo()
        {
            // Create the output buffer
            List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();

            // Send output
            if (nextStimSample <= (nextAvailableSample + (ulong)NRStimSrv.GetBuffSize()))
            {

                // Digital port state
                uint pState = 0;

                // ecode the current target digitally, using a pulse
                pState += Convert.ToUInt32(100 * currentTarget);

                // Pump control
                if (!pumpIsOn)
                    pState += (uint)1 << 31;
                else
                    pState += 0;

                // Write Port State
                toAppendDig.Add(new DigitalOutEvent(nextStimSample, pState));
            }

            // Send dig encoding
            if (toAppendDig.Count > 0)
                NRStimSrv.DigitalOut.WriteToBuffer(toAppendDig);
        }

        private void ApplyEStimFeedback()
        {
            // Create the output buffer
            List<StimulusOutEvent> toAppendStim = new List<StimulusOutEvent>();

            // Which channel are we stimulating on
            stimChannel = stimChannel % stimElectrodes.Length;

            // Make stimulus waveform
            int preSamp = (int)(preStimSec * NRStimSrv.SampleFrequencyHz);
            int phaseSamp = (int)(stimPhaseSec * NRStimSrv.SampleFrequencyHz);
            int wavelength = (2 * preSamp + 2 * phaseSamp);
            double[] waveform = new double[wavelength];

            for (int i = 0; i < wavelength; i++)
            {
                if (i < preSamp)
                    waveform[i] = 0.0;
                else if (i >= preSamp && i < preSamp + phaseSamp)
                    waveform[i] = channelVoltage[stimChannel];
                else if (i >= preSamp + phaseSamp && i < preSamp + 2 * phaseSamp)
                    waveform[i] = -channelVoltage[stimChannel];
                else
                    waveform[i] = 0.0;
            }

            // Send output
            while (nextStimSample <= (nextAvailableSample + (ulong)NRStimSrv.GetBuffSize()))
            {
                // Update channel stim times
                secSinceLastStimChannel[stimChannel] = 0;

                // create stimuli
                if (channelVoltage[stimChannel] > 0.01)
                    toAppendStim.Add(new StimulusOutEvent(stimElectrodes[stimChannel], nextStimSample, waveform));

                //simocVariableStorage.LastAuxEventSample = simocVariableStorage.NextAuxEventSample;
                nextStimSample += isi;

                // Cycle stimulation channel
                stimChannel++;
            }

            // Send stimuli
            if (toAppendStim.Count > 0)
                NRStimSrv.StimOut.WriteToBuffer(toAppendStim);
        }

        private void StepBackISI()
        {
            // Remove the last isi since it was never sent
            nextStimSample -= isi;
        }

        private void RefreshFeedbackVar()
        {
            // stimVoltage initial condition
            stimVoltage = 0.2; //volts

            // Fine tuning variable storage
            lastChannelResponses = new double[stimElectrodes.Length][];
            channelVoltage = new double[stimElectrodes.Length];
            secSinceLastStimChannel = new double[stimElectrodes.Length];
            stimulusIndex = new int[stimElectrodes.Length];
            indexRequiresUpdate = new bool[stimElectrodes.Length];
            tuningFactor = new double[stimElectrodes.Length];
            tuningFactorNorm = new double[stimElectrodes.Length];
            for (int i = 0; i < stimElectrodes.Length; i++)
            {
                lastChannelResponses[i] = new double[averagingHistory];
                secSinceLastStimChannel[i] = 1000;
                channelVoltage[i] = stimVoltage;
                tuningFactor[i] = 1.0;
                tuningFactorNorm[i] = 1.0;
            }
        }

    }
}