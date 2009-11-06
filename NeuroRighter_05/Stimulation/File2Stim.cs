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
    class File2Stim
    {
        // Declare space for the file-id's for the three .dat files needed to creat N stimuli and the matracies that will contain the data
        internal string stimfile; // ascii file containing all nessesary stimulation info as produced by the matlab script makestimfile.m
        internal int numstim; // the number of separate stimuli in the stimulation protocol
        internal int wavesize; // the number of samples per stimulation waveform
        // Voltage offset
        double offset;

        internal int[] timeVec; //interstim times (NX1 vector)
        internal int[] channelVec; // stimulation locations (NX1 vector)
        internal double[,] waveMat; // stimulation waveforms (NXM vector, M samples per waveform)
        internal string line;
        internal double[] waveform;

        private AutoResetEvent _blockExecution = new AutoResetEvent(false);

        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;

        private BackgroundWorker bw;
        private Boolean isCancelled;

        //Event Handling
        internal delegate void ProgressChangedHandler(object sender, int percentage);
        internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler AlertAllFinished;

        //Constructor to form an arbitary stimulation experiment.
        internal File2Stim(string stimfile, double offset, Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
        {
            //File ID's
            this.stimfile = stimfile;
            this.offset = offset;

            //Get references to tasks
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
        }

        // Method to load the *.olstim file into arrays of doubles
        internal void loadStim(string filePath)
        {
            StreamReader file = new StreamReader(filePath);
            int j = 1;
            char delimiter = ' ';

            Reset:
            while ((line = file.ReadLine()) != null)
            {
                if (j == 1)
                {
                    j++;
                    goto Reset;
                }
                if (j == 2)
                {
                    numstim = Convert.ToInt32(line);
                    timeVec = new int[numstim];
                    channelVec = new int[numstim];
                    j++;
                    goto Reset;
                }
                if (j > 2 && j <= 2 + numstim)
                {
                    timeVec[j-3] = Convert.ToInt32(line);
                    j++;
                    goto Reset;
                }
                if (j > 2 + numstim && j <= 2+2*numstim)
                {
                    channelVec[j-numstim -3] = Convert.ToInt32(line);
                    j++;
                    goto Reset;
                }
                if (j == 3 + 2*numstim)
                {
                    string[] split = line.Split(delimiter);
                    wavesize = split.Length;
                    waveMat = new double[numstim,wavesize];
                }
                if (j > 2 + 2 * numstim && j <= 2 + 3 * numstim)
                {
                    string[] split = line.Split(delimiter);
                    for (int i = 0; i < split.Length; ++i)
                    {
                        waveMat[j - 2 * numstim - 3, i] = Convert.ToDouble(split[i]);
                    }
                    j++;
                }
            }
        }
  
        internal void start()
        {
            // Setup BGW
            bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw.WorkerSupportsCancellation = true;
            bw.WorkerReportsProgress = true;

            // Run Worker
            bw.RunWorkerAsync();
        }

        // Stop Method
        internal void stop()
        {
            isCancelled = true;
            bw.CancelAsync();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            stimAnalogTask.Stop();
            stimDigitalTask.Stop();
            if (AlertAllFinished != null) AlertAllFinished(this);
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double[] state = (double[])e.UserState;
            if (AlertProgChanged != null) AlertProgChanged(this, e.ProgressPercentage);
        }

        // Experiment Method
        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            
            if (!bw.CancellationPending)
            {
                // Load the data files
                loadStim(stimfile); // create the stimulation data arrays
                int lengthStim = waveMat.GetLength(1); // Length of each stimulus waveform in samples

                // Post stimulus blanking
                double[] blank = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                //Get starting and ending time
                //outputFileWriter.Flush();
                Boolean isDone = false;
                stimAnalogTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
                stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;

                // Deliver the stimuli!

                int j = 0;
                DateTime startTime = DateTime.Now;
                while (!isDone && !isCancelled && !bw.CancellationPending)
                {
                    int isi;
                    if (j < timeVec.Length - 1)
                    {
                        isi = timeVec[j+1]-timeVec[j];
                    }
                    else
                    {
                        isi = 0;
                        isDone = true;
                    }

                    int chan = channelVec[j];

                    // Create waveform vector from matrix of waveforms
                    waveform = new double[lengthStim + blank.Length];
                    for (int i = 0; i < lengthStim; ++i)
                        waveform[i] = waveMat[j, i] + offset;
                    for (int i = lengthStim; i < +blank.Length; ++i)
                        waveform[i] = blank[i];
                    
                    // fire!
                    deliverStimulus(isi, chan, waveform);

                    // Report protocol progress
                    j++;
                    int percentComplete = (int)((100*j)/(timeVec.Length));
                    bw.ReportProgress(percentComplete); 
                }
            }
        }

        // Stimulus Delivery Method for arbitrary waveform. Calls StimWave.cs
        private void deliverStimulus(int interstim_interval, int channel, double[] wave)
        {

            // Create stim object
            StimWave stimulus = new StimWave(channel, waveform);
            stimulus.populate();

            //Setup pulse timing
            stimAnalogTask.Timing.SamplesPerChannel = stimulus.analogPulse.GetLength(1);
            stimDigitalTask.Timing.SamplesPerChannel = stimulus.digitalData.GetLength(0);

            //Deliver stimulus 
            stimAnalogWriter.WriteMultiSample(true, stimulus.analogPulse);
            stimDigitalWriter.WriteMultiSamplePort(true, stimulus.digitalData);

            // Deconstruct stimulus
            stimDigitalTask.WaitUntilDone();
            stimAnalogTask.WaitUntilDone();
            stimAnalogTask.Stop();
            stimDigitalTask.Stop();

            System.Threading.Thread.Sleep(interstim_interval); // in milliseconds

        }

    }
}
