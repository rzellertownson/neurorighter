using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using pnpCL;
using System.IO;
using System.Windows.Forms;
using NeuroRighter;


namespace SharanyaExperiments
{
    public class oneElectrodeSinusoid : pnpClosedLoopAbs
    
    {
       
        
        
        
        

        //int STIM_SAMP_FREQ = 100000;//hz
        

        public  override void  run()
        {
              //THIS STUFF WOULD NORMALLY BE IN MATLAB FOR OPEN LOOP STIM

            //params
            double amplitude = 0.5;//voltage
            double frequency = 7.7;//hz
            double duration = 12987;

            int[] channels = new int[1] { 1};//, 2 };

            //calculate timing stuff here:
            double isi = duration;//1000.0 / trainFrequency;//ms- what is the spacing between each stimulus?
            double[] phaseOffset = new double[channels.GetLength(0)];
            for (int i = 0; i < phaseOffset.Length; i++)
            {
                phaseOffset[i] = isi / phaseOffset.Length * i;//space each stimulus equally
            }
            
            //create the waveform
            double[] wave = sinusoid(frequency, duration, amplitude);// waveform(phaseLength, pulsesPerTrain, amplitude);
            double[,] wavemat = new double[wave.Length, channels.Length];
            for (int i = 0; i < channels.Length; i++)
            {
                for (int j = 0; j < wave.Length; j++)
                {
                    wavemat[j, i] = wave[j];
                }
            }
            //END OF MATLAB-STUFF

            double currentTime = 0;

            double[] stimTimes = new double[channels.Length];
            stimTimes = calcTimeVec(phaseOffset, currentTime);
            


            DateTime timestart = DateTime.Now;
            DateTime timeend = timestart + new TimeSpan(24, 0, 0);
            //start stimulation

            try
            {
                

                CLE.initializeStim();//create the buffer
                CLE.appendStim(stimTimes, channels, wavemat);//append the first pair to the buffer
                CLE.stimBuffStart();//start stimin'
                while (!CLE.isCancelled & DateTime.Now < timeend)//wait for 1 day to pass, or for the experiment to be cancelled.
                {
                    if (CLE.stimuliInQueue() < 2)//lets put an upper bound of one stimulus 'on deck' before we add more
                    {
                        currentTime += isi;//this is in ms, and is timed to the start of the stimbuffer
                        stimTimes = calcTimeVec(phaseOffset, currentTime);//when do we stimulate next?


                        CLE.appendStim(stimTimes, channels, wavemat);
                    }
                }
                CLE.stimBuffStop();
            }
            catch (Exception me)
                {
                    close();
                }
            
        }
        internal double[] sinusoid(double frequency, double duration, double amplitude)//in hz, ms, volts
        {
            double[] outwaveform = new double[(int)(duration * CLE.StimSampFreq() / 1000)];
            for (int i = 0; i < outwaveform.Length; i++)
            {
                outwaveform[i] = Math.Sin((double)(i)*2*Math.PI / CLE.StimSampFreq() * frequency)*amplitude;
            }
            return outwaveform;
        }


        //create the waveform for each pulse- another function that would usually be in matlab
        internal double[] waveform(double phaseLength, int no_pulses, double amplitude)
        {
            int samplesPerPhase = (int) (phaseLength*CLE.StimSampFreq()/1000);
            int samples = samplesPerPhase*no_pulses*2;
            double[] waveout = new double[samples];
            int index = 0;
            for (int i = 0;i<no_pulses;i++)
            {
                for (int j = 0;j<samplesPerPhase*2;j++)
                {
                    if (j<samplesPerPhase)
                        waveout[index++]=-amplitude;
                    else
                        waveout[index++] = amplitude;
                }
            }
            return waveout;
        }

        //update stimulation times
        internal double[] calcTimeVec(double[] phases, double currentTime)
        {
            double[] timesOut = new double[phases.Length];
            for (int i = 0; i < phases.Length; i++)
            {
                timesOut[i] = phases[i] + currentTime;
            }
            return timesOut;
        }

        //what do we do when we're done?
        public override void close()
        {
           
        }
    }
}
