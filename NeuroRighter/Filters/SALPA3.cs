using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//implementation of Daniel Wagenaar's SALPA algorithm, based on his code from Meabench
//I have attempted to use variable names to reflect the vocabulary of the methods paper,
//but default to the meabench code when the paper is not specific, and to previous incarnations
//of the code in NeuroRighter for interface specifics.
//takes in one continuous raw stream (either raw or filtered data) and outputs two streams: 
//the 'corrected' stream with artifacts removed, as well as a stream detailing the estimated artifact 


namespace NeuroRighter.Filters
{
    using rawType = System.Double;
    class SALPA3:filter
    {
        
        
        private int numElectrodes;//how many electrodes (16 or 59) are we processing?
        private int numSamples;
        private rawType railHigh;
        private rawType railLow;
        private int PRE;
        private int POST;

        private int length_sams;//= 75;
        private int asym_sams;// = 10;
        private int blank_sams;// = 20;
        private int ahead_sams;// = 5;
        private int period_sams;// = 0;
        private int delay_sams;// = 0;
        private int forcepeg_sams;// = 0;
        
        private rawType[] thresh;
       

        private LocalFit[] fitters;

        //note that this only needs to filter the channels on this particular device


        public SALPA3(int length_sams,int asym_sams,int blank_sams,int ahead_sams, int forcepeg_sams, rawType railLow, rawType railHigh, int numElectrodes, int bufferLength, rawType[] thresh)
        {

                                            //MB defaults:
            this.length_sams = length_sams;   // 75;
            this.asym_sams = asym_sams;// 10;
            this.blank_sams = blank_sams;// 75;//HACK try 20
            this.ahead_sams = ahead_sams;// 5;
            //this.period_sams = period_sams;// 0;
            //this.delay_sams = 0;
            this.forcepeg_sams = forcepeg_sams;//10;


            this.thresh = thresh;
            this.numElectrodes = numElectrodes;
            this.railHigh = railHigh;
            this.railLow = railLow;
            this.numSamples = bufferLength;

            this.PRE = 2 * length_sams ;
            this.POST = 2 * length_sams + 1 + ahead_sams;
            fitters = new LocalFit[numElectrodes];
            for (int i = 0; i < numElectrodes; i++)
            {
                fitters[i] = new LocalFit(thresh[i], length_sams, blank_sams, ahead_sams, asym_sams, railHigh, railLow, bufferLength, forcepeg_sams);
            }

         }


        
         public void filter(ref rawType[][] filtData, int startChannel, int numChannels, 
            List<NeuroRighter.StimTick> stimIndicesIn, int numBufferReads)
        {
            List<int>[] stimIndices = new List<int>[numChannels];
            //grab the stim indices needed for this particular buffer load
                lock (stimIndicesIn)
                {
                    //convert the stimindices input into something easier to search- indices are in relationship to the current buffload,
                    //use all indices from the last two buffers (current buffer included).  Note that this is a deviation from previous NR
                    //SALPA implimentations
                    for (int j = 0; j < numChannels; j++)
                    {
                        stimIndices[j] = new List<int>(stimIndicesIn.Count);
                        for (int i = 0; i < stimIndicesIn.Count; ++i)
                        {
                            if (stimIndicesIn[i].numStimReads == numBufferReads)
                                stimIndices[j].Add(stimIndicesIn[i].index);
                            else if (stimIndicesIn[i].numStimReads == numBufferReads - 1)
                                stimIndices[j].Add(stimIndicesIn[i].index - numSamples);
                        }
                    }
                }
                //push the buffer's worth of data you have through the state machine, channel by channel
                
                for (int i = startChannel; i < startChannel + numChannels; i++)
                {
                    
                    rawType[] dataout;
                    
                    //actually creates new arrays for output- 
                    dataout = fitters[i].filter(ref stimIndices[i-startChannel],ref filtData, i);
                    for (int j = 0; j < filtData[0].Length; j++)
                    {
                        filtData[i][j] = dataout[j];
                    }
                }
            
        }

         public int offset()
         {
             return POST;
         }

        

         

    //    static void Main(string[] args)
    //{
    //    SALPA3 test = new SALPA3(75, 5, 5, 5, 5, 5, 5, 5, 5);    
    //    test.mein(args);
    //}
         
    }

    
}
