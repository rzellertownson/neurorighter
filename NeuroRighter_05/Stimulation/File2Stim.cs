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
        internal string timefile; //interstim times (NX1 vector)
        internal string channelfile; // stimulation locations (NX1 vector)
        internal string wavefile; // stimulation waveforms (NXM vector, M samples per waveform)

        // Voltage offset
        double offset;

        internal int[] timeVec; //interstim times (NX1 vector)
        internal int[] channelVec; // stimulation locations (NX1 vector)
        internal double[,] waveMat; // stimulation waveforms (NXM vector, M samples per waveform)
        internal string line;
        internal string _line;
        internal double[,] datmat;
        internal int[] datvec;
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
        internal File2Stim(string timefile, string channelfile, string wavefile, double offset, Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
        {
            //File ID's
            this.timefile = timefile;
            this.channelfile = channelfile;
            this.wavefile = wavefile;
            this.offset = offset;

            //Get references to tasks
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;
        }

        // Method to load data files into arrays of doubles
        internal int[] loadVec(string filePath)
        {
            var lineCount = File.ReadAllLines(@filePath).Length;
            StreamReader file = new StreamReader(filePath);
            int i = 0;
            datvec = new int[lineCount];
            while ((line = file.ReadLine()) != null)
            {
                datvec[i] = Convert.ToInt32(line);
                i++;
            }
            return datvec;
        }

        // Method to load data files into arrays of doubles
        internal double[,] loadMat(string filePath)
        {
            var lineCount = File.ReadAllLines(@filePath).Length;

            StreamReader _file = new StreamReader(filePath);
            string firstline = _file.ReadLine();
            char delimiter = ' ';
            _line = _file.ReadLine();
            string[] _split = _line.Split(delimiter);
            int lineSize = _split.Length;

            StreamReader file = new StreamReader(filePath);
            int i = 0;
            datmat = new double[lineCount, lineSize];
            while ((line = file.ReadLine()) != null)
            {
                string[] split = line.Split(delimiter);
                for (int j = 0; j < split.Length; ++j)
                    datmat[i, j] = Convert.ToDouble(split[j]);
                i++;
            }
            return datmat;
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
                timeVec = loadVec(timefile); //interstim times (NX1 vector of integers is ms)
                channelVec = loadVec(channelfile); // stimulation locations (NX1 vector of integers)
                waveMat = loadMat(wavefile); // stimulation waveforms (NXM vector, M samples per waveform)
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
