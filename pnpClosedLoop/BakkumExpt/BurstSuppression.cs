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
    class BurstSuppression : pnpClosedLoopAbs
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
#region overriden methods
        public override void run()
        {
            //file header
            fs = new FileStream("burstSuppression.txt", FileMode.Create);

            w = new StreamWriter(fs, Encoding.UTF8);
            w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":closed loop burst suppression experiment started");
            long start = DateTime.Now.Ticks;

            //overhead



            //the loop
            count = 0;
            List<SpikeWaveform> record;
            string response;
            firstStim();
            CLE.initializeStim();
            CLE.stimBuffStart();
            while (!CLE.isCancelled)
            {
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": appending " + stimToString());
               CLE.appendStim(timevec, channelvec, wavemat);
               w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ": appended");
                firstStim();
                System.Threading.Thread.Sleep(100);
                record = CLE.recordClear();
                response = null;
                SpikeWaveform current;
                for (int i = 0; i < record.Count(); i++)
                {
                    current = record.ElementAt(i);
                    response += "(" + ((double)current.index)/25000 + ", " + current.channel + ") ";
                    count++;
                }
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":" + response);
            
                
                    w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":stim offset: " + CLE.StimOffset() + " stim in queue: " + CLE.stimuliInQueue()+ ".");
            }

        }


        public override void close()
        {
            try
            {
            //    CLE.stimBuffStop();
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":" + count + " spikes recorded");
                w.WriteLine("closed loop burst suppression experiment stopped at " + DateTime.Now.TimeOfDay.ToString());
                w.Flush();
                w.Close();
                fs.Close();
                MessageBox.Show("closed loop burst suppression experiment stopped successfully");
            }
            catch (Exception e)
            {
                //close();
            }
        }

#endregion

        #region helper methods
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
