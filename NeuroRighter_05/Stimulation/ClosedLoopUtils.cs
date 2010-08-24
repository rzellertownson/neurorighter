using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NeuroRighter
{
    

    //purpose of this class is to provide a set of static methods for closed loop experiments that can
    //be written without an exhaustive understanding of NeuroRighter's intestines
    public partial class ClosedLoopExpt
    {
        #region stim params
        Int32 BUFFSIZE;
        int STIM_SAMPLING_FREQ;
        int NUM_SAMPLES_BLANKING = 1;
        private StimBuffer buffer;
        #endregion
        
        #region STIM METHODS
        //initialize the stimbuffer if you want to generate each wavestim separately
        public void initializeStim()
        {
            //stolen from JN's file2stim3 code
            //create a stimbuffer that you can start appending to
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

        //initialize the stimbuffer if you want to keep adding stim commands to a buffer
        //bufferlength- size of the wavestim array (ie, the number of individual stimuli that can be stored and ready to go)
        //wavelength- the size of each waveform sent (this will put an upper limit on stimulation frequency as well as a limit on waveform length)
        public void initializeStim(int bufferlength, int wavelength)
        {
            //stolen from JN's file2stim3 code
            //create a stimbuffer that you can start appending to
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
            buffer = new StimBuffer(bufferlength, wavelength, BUFFSIZE, STIM_SAMPLING_FREQ, NUM_SAMPLES_BLANKING);
            
        
        }

        //wavestim
        public void waveStim(int[] timeVec, int[] channelVec, double[,] waveMat) 
        {
            
            StimBuffer stimulusbuffer;
            ulong samplessent;
           
                int lengthWave = waveMat.GetLength(1); // Length of each stimulus waveform in samples

                //Instantiate a stimulus buffer object
                 stimulusbuffer = new StimBuffer(timeVec, channelVec, waveMat, lengthWave,
                   BUFFSIZE, STIM_SAMPLING_FREQ, NUM_SAMPLES_BLANKING);
          
                //Populate the 1st stimulus buffer
                stimulusbuffer.precompute();


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
                samplessent = 0;
           
                while (!isCancelled && !bw.CancellationPending && stimulusbuffer.NumBuffLoadsCompleted < stimulusbuffer.NumBuffLoadsRequired)
                {
                    //Populate the stimulus buffer
                    stimulusbuffer.populateBuffer();

                    // Wait for space to open in the buffer
                    samplessent = (ulong) stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                    while (((stimulusbuffer.NumBuffLoadsCompleted - 1) * (ulong)BUFFSIZE - samplessent > (ulong)BUFFSIZE) && !isCancelled && !bw.CancellationPending)
                    {
                        samplessent = (ulong) stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
                    }
                    if (isCancelled || bw.CancellationPending) break;
                    //Write Samples to the hardware buffer
                    stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
                    stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);
                }
                stimAnalogTask.Stop();
                stimDigitalTask.Stop();
                stimulusbuffer = null;
           
        }

        public void appendStim(int[] timeVec, int[] channelVec, double[,] waveMat)
        {
            buffer.append(timeVec, channelVec, waveMat);
        }

        public uint availableBufferSpace()
        {
            return buffer.availableBufferSpace();
        }

        public bool StimRunning()
        {
            return buffer.running;
        }
        public void stimBuffStart()
        {
            buffer.start(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask);
        }

        public void stimBuffStop()
        {
            buffer.stop();
        }
        #endregion

        #region RECORD METHODS

        //wait for a burst to occur, and then return all spikes in that burst.  If timeout occurs, return empty array
        //algorithms:
        //  0- simple envelope detection
        //  1- wagenaar detection (using singlet bursts) (NOT IMPLEMENTED)
        public List<SpikeWaveform> waitForBurst(int msTimeout,int algorithm)
        {
            int rez = 10;//ms
            int threshold = 100;
            List<SpikeWaveform> burst = new List<SpikeWaveform>();
            switch (algorithm)
            {
                case 0:
                    #region envelope method
                    //clear all previously detected waveforms
                    clearWaves();

                    //setup timeout detection
                    DateTime start = DateTime.Now;
                    DateTime end = start + new TimeSpan(0, 0, 0, 0, msTimeout);
                    //List<int>  = new List<int>(100);
                    List<List<SpikeWaveform>> envelope = new List<List<SpikeWaveform>>();
                    int mostRecentSpikeCount = 0;
                    int[] filter = {
                                       10,
                                       30,
                                       50
                                   };
                    List<SpikeWaveform> temp;
                    int score = 0;
                    while ((DateTime.Now < end)&&(score<threshold))
                    {
                        //read spike rate- we only care about the spikes that fall within the filter
                        if (envelope.Count >= filter.Length)
                            envelope.RemoveAt(0);
                        temp = new List<SpikeWaveform>();
                        temp.AddRange(waveforms);
                        
                        clearWaves();
                        envelope.Add(temp );
                        
                        score = 0;
                        int total = 0;
                        //filter the response
                        if (envelope.Count >= filter.Length)
                        {
                            for (int i = 0; i < filter.Length; i++)
                            {
                                total += envelope.ElementAt(envelope.Count - filter.Length + i).Count;
                                score += filter[i] * envelope.ElementAt(envelope.Count - filter.Length + i).Count;
                            }
                            if (total > 0)
                                score /= total;
                            else
                                score = 0;
                           // progress(score);
                        }
                        wait(rez);

                        
                    }
                    if (score>=threshold)
                    {
                        for (int i = 0; i < envelope.Count; i++)
                        {
                            burst.AddRange(envelope.ElementAt(i));
                        }
                    }
                        
                    break;
                    #endregion
                case 1:
                    #region wagenaar method

                    throw new NotImplementedException("Wagenaar algorithm is not implemented yet. Try a lower number.");
                    //TODO:
                    //clear all previously detected waveforms
                    //clearWaves();
                    //locate singlet bursts
                    //find coincidence of singlet bursts
                    //locate starting point of burst
                    //wait for end of burst
                    break;
                    #endregion
                default:

                    throw new NotImplementedException("that algorithm is not implemented yet. Try a different algorithm.");
                    break;
            }
            return burst;
        }

        //record(ms) - record spikes for the next _ ms
        public List<SpikeWaveform> record(int ms)
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
        public List<SpikeWaveform> record()
        {
            return waveforms;
        }
        
        //clear the waveforms recorded so far
        public void clearWaves()
        {
            waveforms = new List<SpikeWaveform>();
        }

        //precisely timed wait.. or maybe just a wait
        public void wait(int ms)
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

        public void progress(int pg)
        {
            if (pg>100)
                bw.ReportProgress(100);
            else if (pg<0)
                bw.ReportProgress(0);
            else 
                bw.ReportProgress(pg);
        
                
        }
        #endregion

        public void stim(stimWave sw)
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

    public class stimWave
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
