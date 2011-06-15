using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;

namespace TestProtocols
{
    class OptoASDRController : ClosedLoopExperiment
    {
        int inc = 0;
        double isi = 0;
        double lastStimTime = 0;
        int offset = 0;
        Random r = new Random();
        List<SpikeEvent> recspikes = new List<SpikeEvent>();
        EventBuffer<SpikeEvent> lastSpikes;
        ulong recordedToSpike = 0;
        ulong[] range;

        // Closed loop alg. prameters
        Queue<double> csdrEstimates;
        int queueLength;
        int numRecChan = 59;
        ulong numReadsCompleted = 0;
        double daqPollingPeriodSec = 0.1; // Set this in hardware!
        ulong daqPollingPeriodSamples; // Set this in hardware!
        double windowSizeSec = 10; // moving window to estimate ASDR from
        double alpha = 0.001; // Hz per refresh period
        double csdrDes = 10; // Hz (desired median FR).
        double stimVoltage = 0; // Volts
        double stimFreq = 2; // Hz
        double stimPulseWidth = 0.002; // sec
        double minFreq = 0.5; // Hz
        double maxFreq = 50; // Hz


            protected override void Run()
            {
                double starttime = StimSrv.DigitalOut.GetTime();
                offset = StimSrv.GetBuffSize() * 3;
                Console.WriteLine("OPTO ASDR CONTROLLER starting out at time " + starttime.ToString() + " by StimOut clock");
                
                // Create CSDR Esimate Queue
                queueLength = (int)(windowSizeSec/daqPollingPeriodSec);
                csdrEstimates = new Queue<double>(queueLength);

                // calculate DAQ polling period in samples
                daqPollingPeriodSamples = (ulong)Math.Round(daqPollingPeriodSec*DatSrv.spikeSrv.sampleFrequencyHz);
               
            }

            protected override void BuffLoadEvent(object sender, EventArgs e)
            {
                if (Running)
                {
                    // First, figure out what history of spikes we have
                    ulong[] spikeTimeRange = DatSrv.spikeSrv.EstimateAvailableTimeRange();

                    // Try to get the number of spikes within the available time range
                    ulong[] dataRange = new ulong[2] {spikeTimeRange[0], spikeTimeRange[0] - daqPollingPeriodSamples};
                    EventBuffer<SpikeEvent> spikes = DatSrv.spikeSrv.ReadFromBuffer(dataRange);

                    // Estimate the ASDR for the last time window
                    double asdrEstimate = (double)spikes.eventBuffer.Count/daqPollingPeriodSec;
                    ++numReadsCompleted;

                    // Update the csdr queue
                    csdrEstimates.Enqueue(asdrEstimate / numRecChan);

                    // Caculate the csdr estimate
                    double csdrEstimate;

                    if (numReadsCompleted > (ulong)queueLength)
                    {
                        csdrEstimates.Dequeue();

                        // Find the median of the csdr estimates thus far
                        var orderedCsdr = from element in csdrEstimates
                                          orderby element ascending
                                          select element;
                        csdrEstimate = orderedCsdr.ElementAt(queueLength / 2 - 1);
                    }
                    else
                    {
                        // Do this until buffer is filled
                        csdrEstimate = csdrDes;
                    }

                    // Calculate error in CSDR
                    double csdrDiff = csdrDes - csdrEstimate;

                    // Calculate stimulus frequency based on the csdr error
                    double stimFreq1 = stimFreq + alpha*csdrDiff;
                    if (!double.IsNaN(stimFreq1))
                        stimFreq = stimFreq1;

                    // Bound stim freq
                    if (stimFreq < minFreq)
                        stimFreq = minFreq;
                    else if (stimFreq > maxFreq)
                        stimFreq = maxFreq;

                    // Make new output buffers based on input
                    List<AuxOutEvent> toAppendAnalog = new List<AuxOutEvent>();
                    List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();

                    isi = fs / stimFreq;
                    ulong pw = (ulong)(fs * stimPulseWidth);
                    inc++;

                    //string outbytes = "";
                    //Console.Write("perceived at " + inc * StimSrv.GetBuffSize());

                    // Make periodic stimulation
                    while ((lastStimTime + isi) <= (inc * StimSrv.GetBuffSize()))
                    {
                        // Get event time
                        lastStimTime += isi;
                        if (lastStimTime < (inc - 1) * StimSrv.GetBuffSize())
                            lastStimTime = inc * StimSrv.GetBuffSize();

                        // Set LED current(ulong)(lastStimTime + offset), 1, stimVoltage)
                        toAppendAnalog.Add(new AuxOutEvent((ulong)(lastStimTime + offset) , 0, stimVoltage));

                        // Stim turns on
                        toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + offset)+pw, 1));

                        // Stim turns off
                        toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + offset) + 2*pw, 0));
                        toAppendAnalog.Add(new AuxOutEvent((ulong)(lastStimTime + offset) + 3*pw, 0, 0.328));

                        //outbytes += toAppend.ElementAt(toAppend.Count - 1).sampleIndex.ToString() + " ";
                        // Console.Write(", " + (ulong)(lastStimTime + offset));
                    }


                    StimSrv.DigitalOut.writeToBuffer(toAppendDig);
                    StimSrv.AuxOut.writeToBuffer(toAppendAnalog);
                    Console.WriteLine(toAppendAnalog.Count + " stims " + toAppendDig.Count + " digs");

                }
            }

        }

    
}
