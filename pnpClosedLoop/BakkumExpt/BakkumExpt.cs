using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using pnpCL;//for closed loop experiment
using System.Windows.Forms;//if you want to debug stuff using pop ups
using System.IO;//if you want to be able to write to files
using NeuroRighter;//if you want to use neurorighter types (including the SpikeWaveformType, which is how spike data is stored)

namespace RileyClosedLoops
{
    public class BakkumExpt:pnpClosedLoopAbs
    {
        //file io stuff
        FileStream fs;
        StreamWriter w;
        Random r = new Random();

        public override void close()
        {
            //what happens when we get shut down unexpectedly?
            //close the file io stuff so we know what happened
            //maybe use a pop up

            try  //we don't know what caused this to crash, so we should be cautious while closing stuff out.
            {
                
                w.WriteLine("bakkum experiment stopped at " + DateTime.Now.TimeOfDay.ToString());
                w.Flush();
                w.Close();
                fs.Close();

                MessageBox.Show("bakkum experiment streams stopped successfully");
            }
            catch (Exception e)
            {
                MessageBox.Show("bakkum experiment streams did not close successfully: " + e.Message);
            }

        }

        public override void run()
        {
            //this code will run when you hit the start button

            //for now, let's just input channel choices in the code (we can change this later by adding a gui or some sort of file reading)
            
            int probe = 42;//1-indexed
            int[] context = {2,3,4,5,6,7};
            int[] cps_isi = { 200, 200, 200, 200, 200, 1000 };//ms
            double voltage = 0.5;//volts
            int phase_duration = 400;//us
            int OFFSET = 10;//us
            double STIM_RATE = 1 / 10;//samples per microsecond
            int[] patterned = new int[59 - 1 - context.Length];

            //create single pulse
            double[] waveform = new double[(int)((phase_duration+OFFSET) * 2 * STIM_RATE)];
            for (int i = 0; i < waveform.Length; i++)
            {
                if (i < OFFSET * STIM_RATE)
                    waveform[i] = 0;
                else if (i < (OFFSET + phase_duration) * STIM_RATE)
                    waveform[i] = -voltage;
                else if (i < (OFFSET + phase_duration * 2) * STIM_RATE)
                    waveform[i] = voltage;
                else
                    waveform[i] = 0;
            }

            //create CPS/probe sequence
            int length = 1 + context.Length;
            int[] CPS_time = new int[length];
            int[] CPS_channel = new int[length];
            double[,] CPS_waves = new double[(int)((phase_duration + OFFSET) * 2 * STIM_RATE), length];
            int current_time = 0;
            for (int i = 0; i < length; i++)
            {
                CPS_time[i] = current_time;
                if (i < context.Length)
                    CPS_channel[i] = context[i];
                else
                    CPS_channel[i] = probe;
                for (int k = 0; k < (phase_duration + OFFSET) * 2 * STIM_RATE; k++)
                {
                    CPS_waves[k, i] = waveform[k];
                }
                current_time += cps_isi[i];
            }

            //recording specs
            int[] rec_duration = {100};//ms
            int[] rec_start = {CPS_time[CPS_time.Length-1]};


            //for simplity's sake, lets just try running this as is

            DateTime start = DateTime.Now;
            TimeSpan duration = new TimeSpan(2, 0, 0);
            List<SpikeWaveform> waves;
            while (DateTime.Now < start + duration)
            {
                CLE.clearWaves();
                CLE.waveStim(CPS_time, CPS_channel, CPS_waves,rec_start,rec_duration);
                waves = CLE.record();//calculate stuff with waves!
            }



        }
    }
}
