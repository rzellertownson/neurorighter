using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NeuroRighter
{
    

    //purpose of this class is to provide a set of static methods for closed loop experiments that can
    //be written without an exhaustive understanding of NeuroRighter's intestines
    partial class ClosedLoopExpt
    {
        #region stim params
        Int32 BUFFSIZE;
        int STIM_SAMPLING_FREQ;
        int NUM_SAMPLES_BLANKING = 1;
        #endregion
        
        #region STIM METHODS
        internal void initializeStim()
        {
            //stolen from JN's file2stim3 code
            //Set buffer regenation mode to off and set parameters
            stimAnalogTask.Stop();
            stimDigitalTask.Stop();

            stimAnalogTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            stimDigitalTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
            stimAnalogTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
            stimDigitalTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
            stimDigitalTask.Timing.SampleClockRate = STIM_SAMPLING_FREQ;
            stimAnalogTask.Timing.SampleClockRate = STIM_SAMPLING_FREQ;

            //Commit the stimulation tasks
            stimAnalogTask.Control(TaskAction.Commit);
            stimDigitalTask.Control(TaskAction.Commit);

            
        }
        //wavestim
        internal void waveStim(int[] timeVec, int[] channelVec, double[,] waveMat) 
        {
            int lengthWave = waveMat.GetLength(1); // Length of each stimulus waveform in samples
             
            //Instantiate a stimulus buffer object
             StimBuffer stimulusbuffer = new StimBuffer(timeVec, channelVec, waveMat, lengthWave,
                BUFFSIZE, STIM_SAMPLING_FREQ, NUM_SAMPLES_BLANKING);

            //Populate the 1st stimulus buffer
            stimulusbuffer.precompute();
            stimulusbuffer.validateStimulusParameters();
            stimulusbuffer.populateBuffer();

            //Write Samples to the hardware buffer
            stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);

            //Populate the 2nd stimulus buffer
            stimulusbuffer.populateBuffer();

            //Write Samples to the hardware buffer
            stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);

            stimDigitalTask.Start();
            stimAnalogTask.Start();
             long samplessent = 0;
            while (!isCancelled && !bw.CancellationPending && stimulusbuffer.NumBuffLoadsCompleted < stimulusbuffer.NumBuffLoadsRequired)
                    
            {
                //Populate the stimulus buffer
                stimulusbuffer.populateBuffer();

                // Wait for space to open in the buffer
                samplessent = stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                while (((stimulusbuffer.NumBuffLoadsCompleted - 1) * BUFFSIZE - samplessent > BUFFSIZE) && !isCancelled && !bw.CancellationPending)
                {
                    samplessent = stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                }
                if (isCancelled || bw.CancellationPending) break;
                //Write Samples to the hardware buffer
                stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
                stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);
            }
            stimAnalogTask.Stop();
            stimDigitalTask.Stop();
        }

        //useful if you want to avoid the time taken up by generateing the stimulus buffer in the first place.
        internal void waveStim(StimBuffer stimulusbuffer)
        {
            //Populate the 1st stimulus buffer
            stimulusbuffer.precompute();
            stimulusbuffer.validateStimulusParameters();
            stimulusbuffer.populateBuffer();

            //Write Samples to the hardware buffer
            stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);

            //Populate the 2nd stimulus buffer
            stimulusbuffer.populateBuffer();

            //Write Samples to the hardware buffer
            stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);

            stimDigitalTask.Start();
            stimAnalogTask.Start();
            long samplessent = 0;
            while (!isCancelled && !bw.CancellationPending && stimulusbuffer.NumBuffLoadsCompleted < stimulusbuffer.NumBuffLoadsRequired)
            {
                //Populate the stimulus buffer
                stimulusbuffer.populateBuffer();

                // Wait for space to open in the buffer
                samplessent = stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                while (((stimulusbuffer.NumBuffLoadsCompleted - 1) * BUFFSIZE - samplessent > BUFFSIZE) && !isCancelled && !bw.CancellationPending)
                {
                    samplessent = stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                }
                if (isCancelled || bw.CancellationPending) break;
                //Write Samples to the hardware buffer
                stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
                stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);
            }
            stimAnalogTask.Stop();
            stimDigitalTask.Stop();
        }

        #endregion

        #region RECORD METHODS

        //set buffer length

        //read buffer

        //record(ms) - record spikes for the next _ ms
        internal List<SpikeWaveform> record(int ms)
        {
            //filler
            List<SpikeWaveform> waves = new List<SpikeWaveform>();
            waveforms = new List<SpikeWaveform>();


            


            System.Threading.Thread.Sleep(ms);
            waves = waveforms;
            return waves;
            //send signal
            //wait until done
        }
        
        //if no argument, just send all the spikes that have been recorded since the last record/clear
        internal List<SpikeWaveform> record()
        {
            return waveforms;
        }
        
        //clear the waveforms recorded so far
        internal void clearWaves()
        {
            waveforms = new List<SpikeWaveform>();
        }

        //precisely timed wait.. or maybe just a wait
        internal void wait(int ms)
        {
            //filler
            System.Threading.Thread.Sleep(ms);
        }

        #endregion

        #region INTERFACE METHODS

        //log
        //simple log interface that will allow closed loop programs to send log info

        //gui
        //idea here is to allow custom experiments to create their own GUI that will be loaded dynamically into NR

        //stuff

        internal void progress(int pg)
        {
            bw.ReportProgress(pg);
        }
        #endregion

        internal void stim(stimWave sw)
{
    stimAnalogTask.Timing.SamplesPerChannel = sw.analogPulse.GetLength(1);
    stimDigitalTask.Timing.SamplesPerChannel = sw.digitalData.GetLength(0);

    stimAnalogWriter.WriteMultiSample(true, sw.analogPulse);
    if (Properties.Settings.Default.StimPortBandwidth == 32)
        stimDigitalWriter.WriteMultiSamplePort(true, sw.digitalData);
    else if (Properties.Settings.Default.StimPortBandwidth == 8)
        stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(sw.digitalData));
    stimDigitalTask.WaitUntilDone();
    stimAnalogTask.WaitUntilDone();
    stimAnalogTask.Stop();
    stimDigitalTask.Stop();
}
    }

    class stimWave
    {
            internal Double[,] analogPulse;
            internal UInt32[] digitalData;
            public stimWave(int ms)
            {
                int totalLength = 0;
                int numRows = 4;
                totalLength += 1 + StimPulse.STIM_SAMPLING_FREQ * ms / 1000;
                analogPulse = new double[numRows, totalLength]; //Only make one pulse of train, the padding zeros will ensure proper rate when sampling is regenerative
                //digitalData = new UInt32[totalLength + 2 * (StimPulse.NUM_SAMPLES_BLANKING + 2)];
               // digitalData = new UInt32
                int offset = 0;
                int size = 0;
                for (int j = size; j < StimPulse.STIM_SAMPLING_FREQ * ms / 1000; ++j)
                    analogPulse[0, j + offset] = 4.0; //4 Volts, TTL-compatible
                analogPulse[0, analogPulse.GetLength(1) - 1] = 0.0;
            }
    }
}
