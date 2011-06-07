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
        double alpha = 0.001; // Hz per 100 ms.
        double asdrDes = 100; // Hz
        double stimVoltage = 2; // Volts
        double stimFreq = 2; // Hz
        double stimPulseWidth = 0.0005; // sec

            protected override void Run()
            {
                double starttime = StimSrv.DigitalOut.GetTime();
                offset = StimSrv.getBuffSize() * 3;
                Console.WriteLine("closed loop tester starting out at time " + starttime.ToString() + " by StimOut clock");

                while (Running)
                {


                    System.Threading.Thread.Sleep(1000);
                    string outs = "";
                    string outc = "";
                    foreach (SpikeEvent spike in recspikes)
                    {
                        outs += ((double)(spike.sampleIndex) / 25000.0).ToString() + ",";
                        outc += (spike.channel + 1) + ",";
                    }
                    //  Console.WriteLine(outs);
                    //  Console.WriteLine(outc);


                    ulong tmp;
                    try
                    {
                        tmp = DatSrv.spikeSrv.EstimateAvaiableTimeRange()[1];
                        range = new ulong[2] { recordedToSpike, tmp };
                        recspikes.AddRange(DatSrv.spikeSrv.ReadFromBuffer(range).eventBuffer);
                        recordedToSpike = tmp;
                        //  Console.WriteLine("spike read completed");
                    }
                    catch (Exception eeeee)
                    {
                        //  Console.WriteLine("error reading spikes: " + e.Message);
                    }


                }

            }

            protected override void BuffLoadEvent(object sender, EventArgs e)
            {
                if (Running)
                {
                    // Console.WriteLine(StimSrv.StimOut.GetTime() + ": going strong, boss!");

                    // First figure out what spikes we have in the buffer
                    ulong[] spikeTimeRange = DatSrv.spikeSrv.EstimateAvaiableTimeRange();

                    // Try to get the number of spikes within the available time range
                    EventBuffer<SpikeEvent> lastSpikes = DatSrv.spikeSrv.ReadFromBuffer(spikeTimeRange);

                    // Estimate the ASDR for the last time window
                    double asdrEstimate = (double)lastSpikes.eventBuffer.Count/((double)(spikeTimeRange[1]-spikeTimeRange[0])/lastSpikes.sampleFrequencyHz);

                    // Calculate error in ASDR
                    double asdrDiff = asdrDes - asdrEstimate;

                    // Calculate stimulus frequency based on the asdr error
                    double stimFreq1 = stimFreq + alpha*asdrDiff;
                    if (!double.IsNaN(stimFreq1))
                        stimFreq = stimFreq1;


                    // Bound stim freq between 0.5 and 100 Hz
                    if (stimFreq < 0.5)
                        stimFreq = 0.5;
                    else if (stimFreq > 100)
                        stimFreq = 100;

                    // Make new output buffers based on input
                    List<AuxOutEvent> toAppendAnalog = new List<AuxOutEvent>();
                    List<DigitalOutEvent> toAppendDig = new List<DigitalOutEvent>();

                    isi = fs / stimFreq;
                    ulong pw = (ulong)(fs * stimPulseWidth);
                    inc++;
                    //string outbytes = "";
                    //Console.Write("perceived at " + inc * StimSrv.getBuffSize());

                    // Make periodic stimulation
                    while ((lastStimTime + isi) <= (inc * StimSrv.getBuffSize()))
                    {

                        uint digOut = (uint)r.Next();

                        lastStimTime += isi;

                        if (lastStimTime < (inc - 1) * StimSrv.getBuffSize())
                            lastStimTime = inc * StimSrv.getBuffSize();

                        // Set LED current
                        toAppendAnalog.Add(new AuxOutEvent((ulong)(lastStimTime + offset), 1, stimVoltage));

                        // Stim turns on
                        toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + offset), 1));

                        // Stim turns off
                        toAppendDig.Add(new DigitalOutEvent((ulong)(lastStimTime + offset) +pw, 0));

                        //outbytes += toAppend.ElementAt(toAppend.Count - 1).sampleIndex.ToString() + " ";
                        // Console.Write(", " + (ulong)(lastStimTime + offset));
                    }


                    StimSrv.DigitalOut.writeToBuffer(toAppendDig);
                    StimSrv.AuxOut.writeToBuffer(toAppendAnalog);
                    //  Console.WriteLine(outbytes);

                }
            }

        }

    
}
