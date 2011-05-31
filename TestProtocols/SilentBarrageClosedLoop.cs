using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;

namespace TestProtocols
{
    class SilentBarrageClosedLoop : ClosedLoopExperiment
    {

        int inc = 0;
        double freq = 10;
        double isi = 0;
        double lastStimTime = 0;
        int offset = 0;
        Random r = new Random();
        SilentBarrageExperimentClient SBclient;
        protected override void Run()
        {
            double starttime = StimSrv.StimOut.GetTime();
            offset = StimSrv.getBuffSize() * 3;
            Console.WriteLine("closed loop tester starting out at time " + starttime.ToString() + " by StimOut clock");
            SBclient = new SilentBarrageExperimentClient("alzrig.neuro.gatech.edu", 3490);
            SBclient.connect();
            while (Running)
            {
                int tot = 32;// r.Next(32);
                List<int> available = new List<int>();
                for (int i= 0;i<32;i++)
                {
                    available.Add(i+1);
                }
                double[] pole = new double[tot];
                double[] height = new double[tot];
                for (int i = 0; i < tot; i++)
                {
                    int tind = r.Next(available.Count);
                    pole[i] = available.ElementAt(tind);
                        available.RemoveAt(tind);
                        height[i] = Math.Round(r.NextDouble() * 900);
                }

                


                SBclient.updateMotor(pole, height);
                SBclient.synch();
                System.Threading.Thread.Sleep(1000);

            }
        }

        protected override void BuffLoadEvent(object sender, EventArgs e)
        {
            if (Running)
            {
                // Console.WriteLine(StimSrv.StimOut.GetTime() + ": going strong, boss!");
                List<StimulusOutEvent> toAppend = new List<StimulusOutEvent>();
                freq *= (r.NextDouble() + 0.5);
                if (freq > 100)
                    freq = 100;
                if (freq < 1)
                    freq = 1;
                isi = fs / freq;
                inc++;
                //Console.Write("perceived at " + inc * StimSrv.getBuffSize());
                while ((lastStimTime + isi) <= (inc * StimSrv.getBuffSize()))
                {

                    lastStimTime += isi;
                    if (lastStimTime < (inc - 1) * StimSrv.getBuffSize())
                        lastStimTime = inc * StimSrv.getBuffSize();
                    toAppend.Add(new StimulusOutEvent((inc % 64) + 1, (ulong)(lastStimTime + offset), cannedWaveform));
                    // Console.Write(", " + (lastStimTime + offset));
                }





                StimSrv.StimOut.writeToBuffer(toAppend);

            }
        }
    }
}
