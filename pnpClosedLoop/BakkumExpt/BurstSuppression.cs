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
        FileStream fs;
        StreamWriter w;
        int count;

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
            while (!CLE.isCancelled)
            {
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
            
            }

        }


        public override void close()
        {
            try
            {
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
    }
}
