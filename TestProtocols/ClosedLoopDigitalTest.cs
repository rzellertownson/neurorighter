using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;


namespace TestProtocols
{
    class ClosedLoopDigitalTest : ClosedLoopExperiment
    {
        int inc = 0;
        double freq = 100;
        double isi = 0;
        double lastStimTime = 0;
        int offset = 0;
        Random r = new Random();

        protected override void Run()
        {
            double starttime = StimSrv.DigitalOut.GetTime();
            offset = StimSrv.getBuffSize() * 3;
            Console.WriteLine("closed loop tester starting out at time " + starttime.ToString() + " by StimOut clock");

            while (Running)
            {


                System.Threading.Thread.Sleep(1000);

            }
        }

        protected override void BuffLoadEvent(object sender, EventArgs e)
        {
            if (Running)
            {
                // Console.WriteLine(StimSrv.StimOut.GetTime() + ": going strong, boss!");
                List<DigitalOutEvent> toAppend = new List<DigitalOutEvent>();
                freq = r.NextDouble() * 100;
                
                
                isi = fs / freq;
                inc++;
                string outbytes = "";
                //Console.Write("perceived at " + inc * StimSrv.getBuffSize());
                while ((lastStimTime + isi) <= (inc * StimSrv.getBuffSize()))
                {
                    uint sigout = (uint)r.Next();
                    lastStimTime += isi;
                    if (lastStimTime < (inc - 1) * StimSrv.getBuffSize())
                        lastStimTime = inc * StimSrv.getBuffSize();


                    toAppend.Add(new DigitalOutEvent((ulong)(lastStimTime + offset), sigout));
                    outbytes += toAppend.ElementAt(toAppend.Count-1).Byte.ToString() + " ";
                    Console.Write(", " + (ulong)(lastStimTime + offset));
                }





                StimSrv.DigitalOut.writeToBuffer(toAppend);
              //  Console.WriteLine(outbytes);

            }
        }

    }
}
