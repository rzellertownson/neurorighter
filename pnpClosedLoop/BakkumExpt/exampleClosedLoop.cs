using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pnpCL;
using System.IO;
using System.Windows.Forms;
using NeuroRighter;

namespace RileyClosedLoops
{
    class exampleClosedLoop : pnpClosedLoopAbs
    {
        //file io
        FileStream fs;
        StreamWriter w;
        int count;

        //stim
        double[] timevec;
        int[] channelvec;
        double[,] wavemat;
        double[] wave;
        int lastStimTime = 0;
        int STIM_SAMP_FREQ = 100000;
        int SPIKE_SAMP_FREQ = 25000;


#region overriden methods
        public override void run()
        {
            //file header
            fs = new FileStream("burstSuppression.txt", FileMode.Create);

            w = new StreamWriter(fs, Encoding.UTF8);
            w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":closed loop burst suppression experiment started");
            long start = DateTime.Now.Ticks;

            //overhead



            //these aren't directly related to the closed loop guts, but are instead a few objects to 
            count = 0;
            List<SpikeWaveform> record;//for holding spikes you have recorded
            string response;
            
            //this is a simple stimulus construction method I use for demonstration purposes
            firstStim();

            //SPECIAL STUFF

            //in this experiment, we are going to use a stimulation buffer
            //this will allow us to look at the spikes that have occured so far, and based on them
            //create a train of stimuli, which we will then append onto the stimulation buffer
            //once we tell the stimulation buffer to start, it looks at all the stimuli that have been appended
            //so far, and starts applying them to the electrodes one by one.
            //you can let the buffer run out if you want- that just means it will wait for you to add more stimuli
            //if you tell it to stimulate at a time in the past, your experiment will close with an error.

            //first, put the stimulation buffer together
            CLE.initializeStim();

            //and lets go ahead and start it up- note that since we are starting the stimulation buffer
            //now, as opposed to at the very beginning of the experiment, there are two clocks running
            //- the first is for the spikes, which started as soon as the experiment started.  The second is for
            //the stimulations, which doesn't start until you call the method below.  This second clock is what
            //is used to figure out when your stimuli are applied- for example, if you decide to stimulate at
            //time '1.0,' you will stimulate 1 millisecond after the stimBuffStart method gets called.
            //you can find out the delay between the two clocks using the method CLE.stimOffset()
            CLE.stimBuffStart();

            //the loop
            //this runs until you hit the 'stop' button in NeuroRighter.  You could include other logic here
            //if you wanted the experiment to stop for other reasons.
            while (!CLE.isCancelled)
            {
                //the writeline stuff is just file i/o
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": appending " + stimToString());


                //append stimuli to the stimulation buffer.  As it takes a second for CLE.stimBuffStart to
                //get going, this code actually puts stimuli on the buffer before the buffer starts stimulating, 
                //, so we are safe using a stimulus time of '0ms'.
               CLE.appendStim(timevec, channelvec, wavemat);

              // w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": appended");

                //this method regenerates the vectors used above- you would include your own code here
                firstStim();
                
                //just a dumb little wait routine.
                System.Threading.Thread.Sleep(100);

                //SPECIAL STUFF
                //this method takes all the spikes that have been recorded since the last time you called
                //this method, and provides them as a list<spikeWaveform>
                record = CLE.recordClear();

                //here is an example of how you could go through a list like that:
                response = null;
                SpikeWaveform current;
                for (int i = 0; i < record.Count(); i++)
                {
                    current = record.ElementAt(i);
                    response += "(" + ((double)current.index) / SPIKE_SAMP_FREQ + ", " + current.channel + ") ";
                    count++;
                }
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":" + response);
            
                //this line demonstrates a couple of nifty little methods:
                //CLE.StimOffset tells you what time your first stimulation happened, with reference to 
                //the spikes clock.  
                //CLE.stimuliInQueue tells you how many stimuli are in the buffer right now
                //CLE.currentStimTime tells you what the current time is based on the stim buffer clock.  Useful for
                //making sure you don't get to far ahead of yourself.
                    w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":stim offset: " + CLE.StimOffset() + " stim in queue: " + CLE.stimuliInQueue()+ ". time ="+CLE.currentStimTime());
            }

        }

        //this code gets called when you click the 'stop' button
        public override void close()
        {
            try
            {
            //  file i/o stuff
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":" + count + " spikes recorded");
                w.WriteLine("closed loop burst suppression experiment stopped at " + DateTime.Now.TimeOfDay.ToString());
                w.Flush();
                w.Close();
                fs.Close();
                MessageBox.Show("closed loop burst suppression experiment stopped successfully");
            }
            catch (Exception e)
            {
                MessageBox.Show("tried to close, but that didn't work.  hmm." + e.Message);
            }
        }

#endregion

        #region helper methods

        //code to update the vectors we are going to append to the stimulation buffer
        public void firstStim()
        {
            wave = biphasicSquarePulse(0.5, 0.4, 0.4);//v,ms,ms
            int stims = 16;
            timevec = new double[stims];
            for (int i = 0; i < timevec.Length; i++)
            {
                int isi = i*3;
                timevec[i] = lastStimTime + isi;
                lastStimTime += isi;
            }

            channelvec = new int[stims];
            for (int i = 0; i < channelvec.Length; i++)
            {
                channelvec[i] = i * 3+1;
            }
            wavemat = new double[wave.Length, stims];
            for (int i = 0; i < wavemat.GetLength(1); i++)
            {
                for (int j = 0; j < wave.Length; j++)
                {
                    wavemat[j, i] = wave[j];
                }

            }
        }
        public string stimToString()
        {
            string outstring = "";
            for (int i = 0; i < timevec.Length; i++)
            {
                outstring += "(" + timevec[i] + "ms, " + channelvec[i] + ") ";
            }
            return outstring;
        }
        public void calculateStim()
        {

        }

        public double[] biphasicSquarePulse(double amplitude, double phase1, double phase2)
        {
            
            double[] waveout = new double[(int)((phase1 + phase2) * STIM_SAMP_FREQ / 1000)];
            for (int i = 0; i < waveout.Length; i++)
            {
                if (i < phase1 * STIM_SAMP_FREQ / 1000)
                    waveout[i] = -amplitude;
                else
                    waveout[i] = amplitude;
            }
            return waveout;
        }

        #endregion
        // public double[] noisePulse(double 
    }
}
