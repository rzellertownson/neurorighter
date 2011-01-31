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
        int NUM_SAMPLES_BLANKING = 10;
        private StimBuffer buffer;
        double stim_spike_offset = -1.0;
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
            buffer = new StimBuffer(BUFFSIZE, STIM_SAMPLING_FREQ, NUM_SAMPLES_BLANKING,50);
            
        }

        //initialize the stimbuffer if you want to keep adding stim commands to a buffer
        //bufferlength- size of the wavestim array (ie, the number of individual stimuli that can be stored and ready to go)
        //wavelength- the size of each waveform sent (this will put an upper limit on stimulation frequency as well as a limit on waveform length)
       

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

        public void appendStim(ulong[] timeVec, int[] channelVec, double[,] waveMat)
        {
            buffer.append(timeVec, channelVec, waveMat);
        }
        public void appendStim(double[] timeVecms, int[] channelVec, double[,] waveMat)
        {
            ulong[] timeVec = new ulong[timeVecms.Length];
            for (int i = 0; i < timeVecms.Length; i++)
            {
                timeVec[i] = (ulong)(timeVecms[i] * STIM_SAMPLING_FREQ / 1000);
            }
            buffer.append(timeVec, channelVec, waveMat);
        }

        public void appendStim(List<StimulusData> stim)
        {
            buffer.append(stim);
        }
      //what is the time delay between the spike file and the stimulus indices?
        public double StimOffset()
        {
            
                return stim_spike_offset;
            
        }

        public int StimSampFreq()
        {
            return STIM_SAMPLING_FREQ;
        }

        public double currentStimTime()
        {
            if (buffer != null)
                return buffer.time();
            else
                return -1.0;

        }

        public bool StimRunning()
        {
            return buffer.running;
        }
        public void stimBuffStart()
        {
           // buffer.initialize(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask, buffLoadTask);
            buffer.setup(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask, buffLoadTask);
            
        }

        public void stimBuffStop()
        {
            buffer.stop();
        }

        public int stimuliInQueue()
        {
            return buffer.stimuliInQueue();
        }

        #region simpler buffer
        //outer buffer is list

       
        //wavestim- add these stimuli to the buffer

        //start- start the experiment, all timing based on this point

        //stop- resent experiment

        //time- current time of the DAQ

        #endregion
        #endregion

        #region RECORD METHODS

        

        
        
        //if no argument, just send all the spikes that have been recorded since the last record/clear

        public List<SpikeWaveform> record()
        {
            return waveforms;
        }
        public List<SpikeWaveform> recordClear()
        {
            List<SpikeWaveform> result = new List<SpikeWaveform>();
            lock (waveforms)
            {
                for(int i = 0;i<waveforms.Count();i++)
                {
                    result.Add(waveforms[0]);
                    waveforms.RemoveAt(0);
                }
            }
            return result;
        }
        
        //clear the waveforms recorded so far
        public void clearWaves()
        {
            waveforms = new List<SpikeWaveform>();
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

       
    }

    
}
