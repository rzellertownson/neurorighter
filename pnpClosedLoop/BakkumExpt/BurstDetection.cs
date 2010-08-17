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
    public class BurstDetection : pnpClosedLoopAbs
    {
        FileStream fs;
        StreamWriter w;
        public override void close()
        {
            try
            {

                w.WriteLine("burst detection algorithm stopped at " + DateTime.Now.TimeOfDay.ToString());
                w.Flush();
                w.Close();
                fs.Close();
                MessageBox.Show("burst detection finished successfully2");
            }
            catch (Exception e)
            {
                //close();
            }
        }

        public override void run()
        {
            MessageBox.Show("burst detection!");
            fs = new FileStream("BurstDetectTest.txt", FileMode.Create);

            w = new StreamWriter(fs, Encoding.UTF8);
            w.WriteLine("burst detection algorithm started at " + DateTime.Now.TimeOfDay.ToString());
            long start = DateTime.Now.Ticks;
            List<SpikeWaveform> burst;
            DateTime duration;
            try
            {
                while (!CLE.isCancelled)
                {
                    burst = CLE.waitForBurst(1000, 0);
                    //MessageBox.Show("burst!");
                    duration = new DateTime(DateTime.Now.Ticks - start);
                    if (burst.Count > 0)
                        w.WriteLine(duration.TimeOfDay.ToString() + ": burst with " + burst.Count + " spikes");
                    else
                        w.WriteLine(duration.TimeOfDay.ToString() + ": no burst");
                    CLE.progress(burst.Count);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                w.WriteLine(e.Message);
                close();
            }
        }
    }
}
