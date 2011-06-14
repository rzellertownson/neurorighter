using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;


namespace TestProtocols
{
    class ClosedLoopTest:ClosedLoopExperiment
    {

        int inc = 0;
        double freq = 100;
        double isi = 0;
        double lastStimTime = 0;
        int offset =0;
        Random r = new Random();

        protected override void Run()
        {
            double starttime = StimSrv.StimOut.GetTime();
            offset = StimSrv.GetBuffSize() * 3;
            Console.WriteLine("closed loop tester starting out at time " + starttime.ToString() + " by StimOut clock");
            
            while (Running)
            {

                Debugger.Write("HI!  I AM A CLOSED LOOP PROTOCOL!  WILL YOU BE MY FRIEND?");
                System.Threading.Thread.Sleep(1000);
                
            }
        }

        protected override void BuffLoadEvent(object sender, EventArgs e)
        {
            if (Running)
            {
               // Console.WriteLine(StimSrv.StimOut.GetTime() + ": going strong, boss!");
                List<StimulusOutEvent> toAppend = new List<StimulusOutEvent>();
                freq = r.NextDouble() * 100;
                isi = fs / freq;
                inc++;
                //Console.Write("perceived at " + inc * StimSrv.GetBuffSize());
                while ((lastStimTime + isi) <= (inc * StimSrv.GetBuffSize()))
                {
                    
                    lastStimTime += isi;
                    if (lastStimTime < (inc-1) * StimSrv.GetBuffSize())
                        lastStimTime = inc * StimSrv.GetBuffSize();
                    toAppend.Add(new StimulusOutEvent((inc % 64) + 1, (ulong)(lastStimTime + offset), cannedWaveform));
                   // Console.Write(", " + (lastStimTime + offset));
                }
                




                StimSrv.StimOut.writeToBuffer(toAppend);
                
            }
        }
    }
}
