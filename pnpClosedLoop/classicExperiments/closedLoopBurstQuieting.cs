using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using pnpCL;
using System.IO;
using System.Windows.Forms;
using NeuroRighter;

namespace classicExperiments
{
    public class closedLoopBurstQuieting: pnpClosedLoopAbs
    {
        public override void run()
        {
            //params
            int integrateSteps = 10;
            int integrateChannelSteps = 10;
            //target firing rate
            double targetFiringRate = 20.0;//hz

            //stimulation rate
            double stimulationRate = 10.0;//hz


            //generate channels
            int[] channels = new int [10] {1,2,3,4,5,6,7,8,9,10};


           double[] wave = biphasicSquarePulse(1.0,0.4,0.4);
            //generate waveforms for each channel

            double overalScale = 1.0;

           double[] wave_amplitude = new double[channels.Length];
           for (int i = 0; i < channels.Length;i++ )
           {
               wave_amplitude[i] = 0.2;
           }
           int asdr = 0;
           List<int> asdr_list = new List<int>(integrateSteps);
           for (int i = 0; i < integrateSteps; i++)
           {
               asdr_list.Add(0);
           }
            //need moving average - list/sum
           int[] channelSDR = new int[channels.Length];
           List<int>[] channelSDR_list = new List<int>[channels.Length];
           for (int i = 0; i < channels.Length; i++)
           {
               for (int j = 0; j < integrateChannelSteps; j++)
               {
                   channelSDR_list[i].Add(0);
               }
           }
           Random r = new Random();
           CLE.initializeStim();
           List<int> stim_history = new List<int>;
           int channel_index = r.Next(channelSDR_list.Length);
            double current_time = 0;
            StimulusData nextStim = new StimulusData(channel_index,current_time,wave*overalScale*wave_amplitude[channel_index]);
            CLE.appendStim(nextStim);
            stim_history.Add(channel_index);

            current_time+=1000/stimulationRate;//ms
            StimulusData nextStim = new StimulusData(channel_index,current_time,wave*overalScale*wave_amplitude[channel_index]);
            CLE.appendStim(nextStim);
            stim_history.Add(channel_index);//have two stimuli in the buffer

            CLE.clearWaves();
            CLE.stimBuffStart();
            List<SpikeWaveform> recorded = List<SpikeWaveform>;
            int count = 0;
            while (!CLE.isCancelled)
            {
                 while(CLE.stimuliInQueue>0)
                {
                    //twiddle thumbs
                }


                //clear. Add next stimulus
                current_time+=1000/stimulationRate;//ms



                channel_index = r.Next(channelSDR_list.Length);
                nextStim = new StimulusData(channel_index, current_time, wave * overalScale * wave_amplitude[channel_index]);
           
                CLE.appendStim(nextStim);
                stim_history.Add(channel_index);

                //now have 1 stim on append.

               //record for 100 ms, know what channel the most recent stimulus was
                thresh1 = CLE.StimOffset()+current_time-200;
                thresh2 = CLE.StimOffset()+current_time-100;
                recorded = CLE.record();
                count = recorded.FindLastIndex(delegate(SpikeWaveform spk){return (double)spk.index/CLE.SpikeSampFreq()<thresh2;});
                 count -= recorded.FindLastIndex(delegate(SpikeWaveform spk){return (double)spk.index/CLE.SpikeSampFreq()<thresh1;});
                
                //update asdr sliding window
                asdr +=count;
                asdr_list.Add(count);
                asdr-=asdr_list.ElementAt(0);
                asdr_list.RemoveAt(0);

                //update channel sliding window
                channelSDR[stim_history.ElementAt(0)] +=count;
                channelSDR_list[stim_history.ElementAt(0)].Add(count);
                channelSDR[stim_history.ElementAt(0)] -= channelSDR_list[stim_history.ElementAt(0)].ElementAt(0);
                channelSDR_list[stim_history.ElementAt(0)].RemoveAt(0);

                
                    
                //update voltages
                    //update master voltage control - multiply 
                    //update tuned voltages
                //append stimulation
                    //draw from distribution
            }
            CLE.stimBuffStop();
        }

        public override void close()
        {

        }

        //todo: create library of fxns, including squarewave, file output, file input
        public double[] biphasicSquarePulse(double amplitude, double phase1, double phase2)
        {

            double[] waveout = new double[(int)((phase1 + phase2) * CLE.StimSampFreq() / 1000)];
            for (int i = 0; i < waveout.Length; i++)
            {
                if (i < phase1 * CLE.StimSampFreq() / 1000)
                    waveout[i] = -amplitude;
                else
                    waveout[i] = amplitude;
            }
            return waveout;
        }


    }
}
