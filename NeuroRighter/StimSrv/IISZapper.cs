//#define USE_LOG_FILE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

//The goal of this is to respond with stimulation 
//whenever an IIS is seen in an LFP trace.

namespace NeuroRighter.StimSrv
{
    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal class IISZapper
    {
        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;
        private StimPulse sp;
        private Boolean isTraining = true;
        private double threshold = 0.0;
        private readonly int totalNumReadsTraining;
        private int numReadsTraining = 0;
        private readonly int totalNumReadsRefractory;
        private int numReadsRefractory;
        internal readonly int channel; //0-based

        private const double thresholdMult = 5;
        private const double numSecondsTraining = 3.0;
        private const double refractory = 1; // in seconds

        internal IISZapper(int phaseWidth, double amplitude, int channel, int numPulses, double rate,
            Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter, double deviceRefreshRate, NeuroRighter sender)
        {
            const int prePadding = 100;
            const int postPadding = 100;

            const double offsetVoltage = 0.0;
            const int interPhaseLength = 0;

            this.stimDigitalTask = stimDigitalTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogTask = stimAnalogTask;
            this.stimAnalogWriter = stimAnalogWriter;
            this.channel = channel - 1;

            sp = new StimPulse(phaseWidth, phaseWidth, amplitude, -amplitude, channel,
                numPulses, rate, offsetVoltage, interPhaseLength, prePadding, postPadding, true);

            stimAnalogTask.Timing.SamplesPerChannel = numPulses * sp.analogPulse.GetLength(1);
            stimDigitalTask.Timing.SamplesPerChannel = numPulses * sp.digitalData.GetLength(0);

            totalNumReadsTraining = (int)(numSecondsTraining / deviceRefreshRate);
            totalNumReadsRefractory = (int)(refractory / deviceRefreshRate);
            numReadsRefractory = totalNumReadsRefractory;
        }

        private void zap()
        {
            //lock (this)
            //{
                stimAnalogWriter.WriteMultiSample(true, sp.analogPulse);
                //if (Properties.Settings.Default.StimPortBandwidth == 32)
                    stimDigitalWriter.WriteMultiSamplePort(true, sp.digitalData);
                //else if (Properties.Settings.Default.StimPortBandwidth == 8)
                //    stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(sp.digitalData));
                stimDigitalTask.WaitUntilDone();
                stimAnalogTask.WaitUntilDone();
                stimAnalogTask.Stop();
                stimDigitalTask.Stop();
            //}
        }

        private void IISDetector(object sender, double[][] lfpData, int numReads)
        {
            if (!isTraining)
            {
                if (numReadsRefractory >= totalNumReadsRefractory)
                {
                    for (int i = 0; i < lfpData[channel].Length; ++i)
                    {
                        if (lfpData[channel][i] > threshold)
                        {
                            zap();
                            numReadsRefractory = 1;

#if(USE_LOG_FILE)
                            NeuroRighter nr = (NeuroRighter)sender;
                            //nr.logFile.WriteLine("Zap trigger at index, buffer read (buffer length): " + i + " " + numReads + " (" + lfpData[channel].Length + ")");
                            nr.logFile.Write(((i / 2000.0) + numReads * 0.003).ToString() + " ");
#endif

                            break; //automatically stop checking, supposing we're in refractory
                            //this will break down if the device refresh rate is long
                        }
                    }
                }
                else ++numReadsRefractory;
            }
            else
            {
                threshold += NeuroRighter.rootMeanSquared(lfpData[channel]);
                if (++numReadsTraining >= totalNumReadsTraining)
                {
                    isTraining = false;
                    threshold /= totalNumReadsTraining;
                    threshold *= thresholdMult;
                }
            }
        }

        internal void start(NeuroRighter sender) { sender.IISDetected += new NeuroRighter.IISDetectedHandler(IISDetector); }
        internal void stop(NeuroRighter sender)  { sender.IISDetected -= new NeuroRighter.IISDetectedHandler(IISDetector); }
    }
}
