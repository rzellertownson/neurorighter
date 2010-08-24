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
    class File2Stim3
    {
        internal string stimfile; // ascii file containing all nessesary stimulation info as produced by the matlab script makestimfile.m
        internal int numstim; // the number of separate stimuli in the stimulation protocol
        internal int wavesize; // the number of samples per stimulation waveform

        internal int[] timeVec; //interstim times (NX1 vector)
        internal int[] channelVec; // stimulation locations (NX1 vector)
        internal double[,] waveMat; // stimulation waveforms (NXM vector, M samples per waveform)

        // Hard coded stimulus parameters
        internal Int32 BUFFSIZE; // Number of samples delivered to DAQ per buffer load
        internal int STIM_SAMPLING_FREQ;
        private const int NUM_SAMPLES_BLANKING = 1;
        internal string line;

        private AutoResetEvent _blockExecution = new AutoResetEvent(false);

        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;
        private BackgroundWorker bw;
        private Boolean isCancelled;
        private long samplessent = 0;

        private StimBuffer stimulusbuffer;

        //Event Handling
        internal delegate void ProgressChangedHandler(object sender, int percentage);
        internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender);
        internal event AllFinishedHandler AlertAllFinished;

        //Constructor to form an arbitary stimulation experiment.
        internal File2Stim3(string stimfile, int STIM_SAMPLING_FREQ ,Int32 BUFFSIZE, Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
        {
            //File ID's
            this.stimfile = stimfile;

            //Get references to tasks
            this.BUFFSIZE = BUFFSIZE;
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
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
                try
                {
                    // Load the data files
                    loadStim(stimfile); // create the stimulation data arrays
                    int lengthWave = waveMat.GetLength(1); // Length of each stimulus waveform in samples

                    //Set buffer regenation mode to off and set parameters
                    stimAnalogTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
                    stimDigitalTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;
                    stimAnalogTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
                    stimDigitalTask.Stream.Buffer.OutputBufferSize = 2 * BUFFSIZE;
                    stimDigitalTask.Timing.SampleClockRate = STIM_SAMPLING_FREQ;
                    stimAnalogTask.Timing.SampleClockRate = STIM_SAMPLING_FREQ;

                    //Commit the stimulation tasks
                    stimAnalogTask.Control(TaskAction.Commit);
                    stimDigitalTask.Control(TaskAction.Commit);

                    //Instantiate a stimulus buffer object
                    stimulusbuffer = new StimBuffer(timeVec, channelVec, waveMat, lengthWave,
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

                    while (!isCancelled && !bw.CancellationPending && stimulusbuffer.NumBuffLoadsCompleted < stimulusbuffer.NumBuffLoadsRequired)
                    {
                        output_Callback(null, null);
                    }

                    // Deconstruct stimulus when protocol is finished
                    stimAnalogTask.Stop();
                    stimDigitalTask.Stop();

                }
                catch (DaqException err)
                {
                    MessageBox.Show(err.Message);
                }

            }
        }

        private void output_Callback(object sender, System.EventArgs e)
        {
            //Populate the stimulus buffer
            stimulusbuffer.populateBuffer();

            // Wait for space to open in the buffer
            samplessent = stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
            while ((stimulusbuffer.NumBuffLoadsCompleted - 1) * (ulong)BUFFSIZE - (ulong)samplessent > (ulong)BUFFSIZE)
            {
                samplessent = stimAnalogTask.Stream.TotalSamplesGeneratedPerChannel;
            }

            //Write Samples to the hardware buffer
            stimAnalogWriter.WriteMultiSample(false, stimulusbuffer.AnalogBuffer);
            stimDigitalWriter.WriteMultiSamplePort(false, stimulusbuffer.DigitalBuffer);

            // Report protocol progress
            int percentComplete = (int)Math.Round((double)100 * (stimulusbuffer.NumBuffLoadsCompleted) / (stimulusbuffer.NumBuffLoadsRequired+1));
            bw.ReportProgress(percentComplete);


        }


        #region load .olstim file

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
                    timeVec[j - 3] = Convert.ToInt32(line);
                    j++;
                    goto Reset;
                }
                if (j > 2 + numstim && j <= 2 + 2 * numstim)
                {
                    channelVec[j - numstim - 3] = Convert.ToInt32(line);
                    j++;
                    goto Reset;
                }
                if (j == 3 + 2 * numstim)
                {
                    string[] split = line.Split(delimiter);
                    wavesize = split.Length;
                    waveMat = new double[numstim, wavesize];
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
        #endregion load .olstim file


    }
}
