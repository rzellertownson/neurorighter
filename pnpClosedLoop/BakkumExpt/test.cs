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
    class test:pnpClosedLoopAbs
    {

        FileStream fs;
        StreamWriter w;

        //stim params
        static int nostim = 10;
        static int wavedur = 100;

        int[] timevec = new int[nostim];
        int[] channelvec = new int[nostim];
        double[,] wavemat = new double[nostim, wavedur];
        double[] wave = new double[wavedur];
        
        Random r = new Random();

        public override void close()
        {
            try
            {

                w.WriteLine("pnp tester stopped at " + DateTime.Now.TimeOfDay.ToString());
                w.Flush();
                w.Close();
                fs.Close();
                MessageBox.Show("pnp tester streams stopped successfully");
            }
            catch (Exception e)
            {
                //close();
            }
        }

        public override void run()
        {
             fs = new FileStream("pnp_tester.txt", FileMode.Create);

            w = new StreamWriter(fs, Encoding.UTF8);
            w.WriteLine(DateTime.Now.TimeOfDay.ToString()+":pnp tester started" );
            long start = DateTime.Now.Ticks;
            List<SpikeWaveform> record;
            DateTime duration;
            //MessageBox.Show("gate 0");
            try
            {



                regen();
                CLE.initializeStim();
                //MessageBox.Show("gate 1");
                w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":first stimbuff generated");
                int count = 0;
                while (!CLE.isCancelled)
                {
                    w.WriteLine(DateTime.Now.TimeOfDay.ToString() +":cycle " +(count++).ToString());
                    CLE.clearWaves();
                    CLE.wait(1000);
                    w.WriteLine(DateTime.Now.TimeOfDay.ToString() + ":wait done " );
                    CLE.waveStim(timevec, channelvec, wavemat);
                    w.WriteLine( DateTime.Now.TimeOfDay.ToString() + ":stim a "+(count).ToString());
                   // MessageBox.Show("gate 2");
                    
                    
                    regen();
                    CLE.waveStim(timevec, channelvec, wavemat);
                    w.WriteLine("" + DateTime.Now.TimeOfDay.ToString() + ":stim b " + (count).ToString());
                  //  MessageBox.Show("gate 3");

                }
            }
            catch (Exception e)
            {
                MessageBox.Show("error in pnp tester: " + e.Message,"pnp error");
                w.WriteLine("error in pnp tester: " + e.Message);
                close();
            }
        }


        private void regen()
        {
            //make stimulus waveform 

            for (int j = 0; j < wavedur; j++)
            {
                wave[j] = (wavedur - j) / wavedur - 0.5;
            }
           // MessageBox.Show("gate0.1");
            int current = 0;
            for (int i = 0; i < nostim; i++)
            {
                for (int j = 0; j < wavedur; j++)
                {
                    wavemat[i, j] = wave[j];
                }
                timevec[i] = current;
                current += 10;  
                channelvec[i] = r.Next(64)+1;
            }
           // MessageBox.Show("gate0.2");


            
        }
    }

}
