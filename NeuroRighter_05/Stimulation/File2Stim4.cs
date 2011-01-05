using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;


namespace NeuroRighter.Stimulation
{
    class File2Stim4
    {
        internal StimBuffer stimbuff;
        private BackgroundWorker bw;//loads stimuli into the buffer when needed
        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;

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
            //open file
            
             StreamReader file = new StreamReader(filePath);
            int j = 1;
            char delimiter = ' ';

            int ondeck = 10;
            int N = 50;
            //load first N stims
            TimeVector = new double [N];
            ChannelVector = new double [N]; 
            WaveMatrix = new double [N, wavesize];


            stimbuff.append(TimeVector, ChannelVector, WaveMatrix);//append first N stimuli
            stimbuff.start(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask);

            while (notdone)//logic to determine if exit or not
            {
                //load next N stims


                //load next N stims

                if (stimbuff.stimuliInQueue<ondeck) //if there are less than 'ondeck' stimuli in the outer buffer
                    stimbuff.append(TimeVector, ChannelVector, WaveMatrix); //add N more stimuli

                //probably also want to include some stuff for reporting progress in here.
            }

            stimbuff.stop();

        }

        



    }
}
