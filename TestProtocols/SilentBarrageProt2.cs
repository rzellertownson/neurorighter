using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;

namespace TestProtocols
{
    class SilentBarrageProt2 : ClosedLoopExperiment
    {
        int inc = 0;
        int buffInc = 0;
        double freq = 20;
        double isi = 0;
        double lastStimTime = 0;
        int offset = 0;
        Random r = new Random();
        SilentBarrageExperimentClient SBclient;
        protected override void Run()
        {
            
            double starttime = StimSrv.StimOut.GetTime();
            offset = StimSrv.GetBuffSize() * 3;
            Console.WriteLine("closed loop tester starting out at time " + starttime.ToString() + " by StimOut clock");
            SBclient = new SilentBarrageExperimentClient("alzrig.neuro.gatech.edu", 3490);
            SBclient.connect();
            ulong recordedToStim = 0;
            ulong processedToStim = 0;
            ulong recordedToSpike = 0;
            ulong[] range;
            ulong processedToSpike = 0;
           
            List<SpikeEvent> recspikes = new List<SpikeEvent>(); ;
            List<ElectricalStimEvent> recstim = new List<ElectricalStimEvent>();
            while (Running)
            {
                //read in all the newest stim/spike data
                //spike
                Console.WriteLine("begin process loop");
                ulong tmp;
                try
                {
                    tmp = DatSrv.spikeSrv.EstimateAvailableTimeRange()[1];
                    range = new ulong[2] { recordedToSpike, tmp };
                    recspikes.AddRange(DatSrv.spikeSrv.ReadFromBuffer(range[0], range[1]).eventBuffer);
                    recordedToSpike = tmp;
                  //  Console.WriteLine("spike read completed");
                }
                catch (Exception e)
                {
                  //  Console.WriteLine("error reading spikes: " + e.Message);
                }
                
                ////stim
                try
                {
                    //tmp = DatSrv.stimSrv.EstimateAvailableTimeRange()[1];
                    //range = new ulong[2] { recordedToStim, tmp };
                    //recstim.AddRange(DatSrv.stimSrv.ReadFromBuffer(range).eventBuffer);
                    //recordedToStim = tmp;
                    
                   // Console.WriteLine("stim read completed");
                }
                catch (Exception e)
                {
                   // Console.WriteLine("error reading stims: " + e.Message);
                }
                string outs = "";
                string outc = "";
                foreach (SpikeEvent spike in recspikes)
                    {
                        outs += ((double)(spike.sampleIndex) / 25000.0).ToString() + ",";
                        outc += (spike.channel+1) + ",";
                    }
                Console.WriteLine(outs);
                Console.WriteLine(outc);
               // Console.WriteLine(recspikes.Count.ToString() + " spikes after "+recordedToSpike.ToString() +"samples, " + recstim.Count.ToString() + " stims after "+ recordedToStim.ToString() + "samples");
                //process and discard as much stim/spike data as you can (into peristimulus)
                //while(recstim.Count>0)
                //{
                ////find the first 
                //}





                int tot = 32;// r.Next(32);
                List<int> available = new List<int>();
                for (int i = 0; i < 32; i++)
                {
                    available.Add(i + 1);
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
                freq *= (r.NextDouble()*1.104 + 0.5);
                if (freq > 100)
                    freq = 100;
                if (freq < 1)
                    freq = 1;
                Console.WriteLine("stim at freq: " + freq.ToString());
                isi = fs / freq;



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

                buffInc++;
                //Console.Write("perceived at " + inc * StimSrv.GetBuffSize());
                while ((lastStimTime + isi) <= (buffInc * StimSrv.GetBuffSize()))
                {

                    lastStimTime += isi;
                    if (lastStimTime < (buffInc - 1) * StimSrv.GetBuffSize())
                        lastStimTime = buffInc * StimSrv.GetBuffSize();
                    toAppend.Add(new StimulusOutEvent((buffInc % 64) + 1, (ulong)(lastStimTime + offset), cannedWaveform));
                    inc++;
                    // Console.Write(", " + (lastStimTime + offset));
                }





                StimSrv.StimOut.writeToBuffer(toAppend);

            }
        }
    }
}
