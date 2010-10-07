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
    class burstSuppression : pnpClosedLoopAbs
    {
        //file io
        FileStream fs;
        StreamWriter w;
        int count;

        //stim
        
        int STIM_SAMP_FREQ = 100000;
        int SPIKE_SAMP_FREQ = 25000;

        public override void run()
        {
 //write header to file

            //file header
            fs = new FileStream("burstSuppression.txt", FileMode.Create);

            w = new StreamWriter(fs, Encoding.UTF8);
            w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":closed loop burst suppression experiment started");
            long start = DateTime.Now.Ticks;
            //target firing rate:
            //voltage range:100-900mv

            //screen:
            //find background firing rate, removing bursts
            //stimulate random spatial distribution and  at 1hz on all electrodes
            //calculate voltage/response curve
            //if curve goes above 5x background rate, that electrode is considered 'active'

            //select 10 electrodes

            int [] channelvec = new int[10] {2,3,4,5,6,7,9,10,11,12};
            
            //select target firing rate

            double target_firing_rate = 30;//hz

            int channel_index = 0;
            double nextStimTime = 0;//ms
            double base_voltage = 0.2;//volts
            double[] electrode_gains = new double[channelvec.Length];
            for (int i = 0; i < electrode_gains.Length; i++)
            {
                electrode_gains[i] = 1;
            }
            int window_length = 2000;//ms
            double target_spike_count = target_firing_rate * (double)window_length / 1000;
            double epsilon = 0.02;

            CLE.initializeStim();
            CLE.appendStim(nextStimTime, channelvec[channel_index], 0);//first stim is throwaway to get timing down
            CLE.stimBuffStart();
            CLE.recordClear();//clear recorded spikes

            //every 100 ms
            //measure firing rate over 2 second window
            //update last 
            //update overal stimulation voltage
            //apply next stim in cycle
            //update individual electrode gain


        }
        //helper methods
        internal double updateBaseVoltage()
        { 

        }
        internal double[] updateGains()
        {
 
        }
        internal double[] biphasicSquarePulse(double phase1,double phase2, double amp)//ms, ms, v
        {
            int length1 = phase1 / 1000 * STIM_SAMP_FREQ;
            int length2 = phase2 / 1000 * STIM_SAMP_FREQ;
            double[] waveform = new double[length1 + length2];
            for (int i = 0; i < waveform.Length; i++)
            {
                if (i<length1)
                    waveform[i] = -amp;
                else
                    waveform[i] = amp;

            }
            return waveform;
        }
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


    }
}
