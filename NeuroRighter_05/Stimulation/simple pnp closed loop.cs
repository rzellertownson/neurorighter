using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace NeuroRighter
{
    class simple:pnpClosedLoop
    {
        #region crap
        ClosedLoopExpt CLE;
        
        
        public void grab(ClosedLoopExpt CLE)
        {
            this.CLE = CLE;
        }
        #endregion

        #region important crap
        public void run()
        {
            
            CLE.initializeStim();//sets stim params for wavestimming
            //int percentProgress = 0;
            List<SpikeWaveform> recording;
            int[] timeVec; //interstim times (NX1 vector)
         int[] channelVec; // stimulation locations (NX1 vector)
         double[,] waveMat; // stimulation waveforms (NXM vector, M samples per waveform)
            //initialize experiment
            //stimWave sw = new stimWave(100);
            while (!CLE.isCancelled)
            {
                //stuff




                //set timeVec, channelVec, waveMat, lengthWave for this round
                timeVec = new int[3];
                channelVec = new int[3];
                waveMat = new double[3, 90];
                int current_time = 0;
                for (int i = 0; i < 3; i++)
                {
                    timeVec[i] = current_time;
                    current_time += 2000;
                    channelVec[i] = i * 10 + 1;
                    for (int j = 0; j < 90; j++)
                    {
                        waveMat[i, j] = j / 100 - 0.45;
                    }
                }

                CLE.waveStim(timeVec, channelVec, waveMat);//takes the timeVec, channelVec, waveMat and lengWave values and stims with them, as if
                //a .olstim file with those params had been loaded.


                //record spikes for 100ms
                recording = CLE.record(100);
                //update progress bar
                //percentProgress+=10;
                //percentProgress %= 100;
                CLE.progress(recording.Count % 100);
            }
        }

        #endregion

    }
}
