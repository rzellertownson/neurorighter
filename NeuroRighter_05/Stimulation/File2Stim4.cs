using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;


namespace NeuroRighter
{
    class File2Stim4
    {
        StreamReader olstimfile; // The stream reader for the .olstim file being used
        internal string stimfile; // ascii file containing all nessesary stimulation info as produced by the matlab script makestimfile.m
        internal string line; // line from the .olstim file
        internal int wavesize; // the number of samples per stimulation waveform
        internal int numstim; // number of stimuli specified in open-loop file
        internal int numStimPerLoad = 500; // Number of stimuli loaded per read of the olstim file
        internal int numLoadsCompleted = 0; // Number loads completed

        internal int[] TimeVector; //interstim times (NX1 vector)
        internal int[] ChannelVector; // stimulation locations (NX1 vector)
        internal double[,] WaveMatrix; // stimulation waveforms (NXM vector, M samples per waveform)

        internal StimBuffer stimbuff;
        private BackgroundWorker bw;//loads stimuli into the buffer when needed
        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;

        // Stimulation Constants
        internal Int32 BUFFSIZE; // Number of samples delivered to DAQ per buffer load
        internal int STIM_SAMPLING_FREQ;

        //Event Handling
        internal delegate void ProgressChangedHandler(object sender, int percentage);
        internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler AlertAllFinished;

        internal File2Stim4(string stimfile, int STIM_SAMPLING_FREQ ,Int32 BUFFSIZE, Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
        {
            this.stimfile = stimfile;

            //Get references to tasks
            this.BUFFSIZE = BUFFSIZE;
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;

            stimbuff = new StimBuffer(BUFFSIZE, STIM_SAMPLING_FREQ, 2);
        }

        internal void start()
        {
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;

            bw.RunWorkerAsync();
           
        }

        internal void stop()
        {
            bw.CancelAsync();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (AlertAllFinished != null) AlertAllFinished(this);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double[] state = (double[])e.UserState;
            if (AlertProgChanged != null) AlertProgChanged(this, e.ProgressPercentage);
        }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            // Load the stimulus buffer
            stimbuff.QueueLessThanThreshold += new QueueLessThanThresholdHandler(appendStimBufferAtThresh);
            // Stop the StimBuffer When its finished
            stimbuff.QueueEmpty += new QueueEmptyHandler(stimbuff_QueueEmpty);

            //open .olstim file
            olstimfile = new StreamReader(stimfile);
            line = olstimfile.ReadLine(); // one read to get through header
            line = olstimfile.ReadLine(); // this read has the number of stimuli
            numstim = Convert.ToInt32(line); // find the number of stimuli specified in the file
            line = olstimfile.ReadLine(); // this read has the number samples in each stimulus
            wavesize = Convert.ToInt32(line); // find the number of stimuli specified in the file

            // Half the size of the largest stimulus data array that your computer will have to put in memory
            stimbuff.queueTheshold = numStimPerLoad;
            int numFullLoads = (int)Math.Floor((double)numstim / (double)numStimPerLoad);

            
            if (4*numStimPerLoad >= numstim)
            {
                // Create Stimulus data arrays, just load all the data because the file is not that big
                TimeVector = new int[numstim];
                ChannelVector = new int [numstim];
                WaveMatrix = new double [numstim, wavesize];

                // Load the stimuli
                loadStimWithWave(olstimfile,numstim);

                // Append the first stimuli to the stim buffer
                stimbuff.append(TimeVector, ChannelVector, WaveMatrix);//append first N stimuli
                stimbuff.start(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask);

                while(stimbuff.stimuliInQueue() > 0)
                {
                }
            }
            else
            {

                // Create Stimulus data arrays
                TimeVector = new int [numStimPerLoad];
                ChannelVector = new int [numStimPerLoad];
                WaveMatrix = new double[numStimPerLoad, wavesize];

                // Load the first stimuli
                loadStimWithWave(olstimfile,numStimPerLoad);

                // Append the first stimuli to the stim buffer
                stimbuff.append(TimeVector, ChannelVector, WaveMatrix);//append first N stimuli
                numLoadsCompleted++;
                stimbuff.start(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask);
            
                while (stimbuff.stimuliInQueue() > 0)
                {
                }


            }

        }

        internal void appendStimBufferAtThresh(object sender, EventArgs e)
        {
            if (numstim - (numLoadsCompleted * numStimPerLoad) > numStimPerLoad)
            {
                loadStimWithWave(olstimfile, numStimPerLoad);
                stimbuff.append(TimeVector, ChannelVector, WaveMatrix); //add N more stimuli
                numLoadsCompleted++;
            }
            else
            {
                // load the last few stimuli
                TimeVector = new int[numstim - numLoadsCompleted * numStimPerLoad];
                ChannelVector = new int[numstim - numLoadsCompleted * numStimPerLoad];
                WaveMatrix = new double[numstim - numLoadsCompleted * numStimPerLoad, wavesize];
                loadStimWithWave(olstimfile, numstim - numLoadsCompleted * numStimPerLoad);
                stimbuff.append(TimeVector, ChannelVector, WaveMatrix); //add N more stimuli
            }
        }

        internal void stimbuff_QueueEmpty(object sender, EventArgs e)
        {
            stimbuff.stop();
        }

        internal void loadStimWithWave(StreamReader olstimFile, int numStimToRead)
        {
            int j = 0;
            char delimiter = ' ';
            string[] splitWave;
            while ( j <= numStimToRead - 1)
            {
                line = olstimFile.ReadLine();
                if (line == null)
                    break;
                        
                // load stim time
                TimeVector[j] = Convert.ToInt32(line);

                // load stime chan
                line = olstimFile.ReadLine();
                ChannelVector[j] = Convert.ToInt32(line);
                
                // load stim waveform
                line = olstimFile.ReadLine();
                splitWave = line.Split(delimiter);
                    for (int i = 0; i < splitWave.Length; ++i)
                    {
                        WaveMatrix[j, i] = Convert.ToDouble(splitWave[i]);
                    }
                j++;
            }
            int done;

        }

    }
}
