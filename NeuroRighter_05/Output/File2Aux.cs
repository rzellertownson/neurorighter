using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using NeuroRighter.DataTypes;

namespace NeuroRighter.Output
{
    class File2Aux
    {
        StreamReader olauxfile; // The stream reader for the .olaux file being used
        internal string auxfile; // ascii file containing all nessesary stimulation info as produced by the matlab script makeauxfile.m
        internal string line; // line from the .olaux file
        internal ulong numAuxEvent; // number of stimuli specified in open-loop file
        internal ulong numEventPerLoad; // Number of stimuli loaded per read of the olstim file
        internal ulong numLoadsCompleted = 0; // Number loads completed
        internal ulong numBuffLoadsRequired; // Number of DAQ loads needed to complete an openloop experiment
        internal bool lastLoad;

        internal List<AuxOutEvent> auxDataChunk;
        internal AuxOutEvent auxDatum;
        private double eventVoltage;
        private int eventChannel;
        private ulong eventTime;
        internal AuxBuffer auxBuff;
        private BackgroundWorker bw;//loads stimuli into the buffer when needed
        private Task auxOutputTask, buffLoadTask;
        private AnalogMultiChannelWriter auxOutputWriter;

        // Did the user provide an aux file
        internal bool auxFileExists;

        // Stimulation Constants
        internal Int32 BUFFSIZE; // Number of samples delivered to DAQ per buffer load
        internal int STIM_SAMPLING_FREQ;

        //Event Handling
        internal delegate void ProgressChangedHandler(object sender, EventArgs e, int percentage);
        internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender, EventArgs e);
        internal event AllFinishedHandler AlertAllFinished;

        internal File2Aux(string auxfile, int STIM_SAMPLING_FREQ, Int32 BUFFSIZE, Task auxOutputTask,
            Task buffLoadTask, AnalogMultiChannelWriter auxOutputWriter, ulong numEventPerLoad, bool auxFileExists)
        {
            this.auxfile = auxfile;
            this.BUFFSIZE = BUFFSIZE;
            this.auxOutputTask = auxOutputTask;
            this.buffLoadTask = buffLoadTask;
            this.auxOutputWriter = auxOutputWriter;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
            this.numEventPerLoad = numEventPerLoad;
            this.auxFileExists = auxFileExists;

            // Instatiate a DigitalBuffer object
            auxBuff = new AuxBuffer(BUFFSIZE, STIM_SAMPLING_FREQ, (int)numEventPerLoad);

        }

        internal void Stop()
        {
            auxBuff.Stop();
        }

        internal void Setup()
        {
            
            // Load the stimulus buffer
            auxBuff.QueueLessThanThreshold += new QueueLessThanThresholdHandler(AppendAuxBufferAtThresh);
            // Stop the StimBuffer When its finished
            auxBuff.StimulationComplete += new StimulationCompleteHandler(AuxBuff_Complete);
            // Alert that auxBuff just completed a DAQ bufferload
            auxBuff.DAQLoadCompleted += new DAQLoadCompletedHandler(AuxBuff_DAQLoadCompleted);


            if (auxFileExists)
            {
                //open .olaux file
                olauxfile = new StreamReader(auxfile);
                line = olauxfile.ReadLine(); // one read to get through header

                line = olauxfile.ReadLine(); // this read has the number of stimuli
                numAuxEvent = Convert.ToUInt64(line); // find the number of stimuli specified in the file
                auxBuff.SetNumberofEvents(numAuxEvent);

                line = olauxfile.ReadLine(); // this read has the final stimulus time
                double finalEventTime = Convert.ToDouble(line); // find the number of stimuli specified in the file
                auxBuff.CalculateLoadsRequired(finalEventTime); // inform the stimbuffer how many DAQ loads it needs to take care of
                numBuffLoadsRequired = auxBuff.numBuffLoadsRequired;

                // Half the size of the largest stimulus data array that your computer will have to put in memory
                int numFullLoads = (int)Math.Floor((double)numAuxEvent / (double)numEventPerLoad);
            }
            else
            {
                numAuxEvent = ulong.MaxValue;
                numBuffLoadsRequired = uint.MaxValue;
                auxBuff.numBuffLoadsRequired = uint.MaxValue;
                int numFullLoads = (int)Math.Floor((double)numAuxEvent / (double)numEventPerLoad);
                auxBuff.SetNumberofEvents(numAuxEvent);

            }


            if (2 * numEventPerLoad > numAuxEvent)
            {
                // Load the stimuli
                LoadAuxEvent(olauxfile, (int)numEventPerLoad);

                // Append the first stimuli to the stim buffer
                Console.WriteLine("File2Aux : Only a single load is needed because there are less than " + 2 * numEventPerLoad + " aux signals");
                auxBuff.writeToBuffer(auxDataChunk); // Append all the stimuli
                numLoadsCompleted = numAuxEvent;
                lastLoad = true;
                auxBuff.Setup(auxOutputWriter, auxOutputTask, buffLoadTask);
            }
            else
            {
                // Load the first stimuli
                LoadAuxEvent(olauxfile, (int)numEventPerLoad);

                // Append the first stimuli to the stim buffer
                auxBuff.writeToBuffer(auxDataChunk);//append first N stimuli
                numLoadsCompleted++;
                auxBuff.Setup(auxOutputWriter, auxOutputTask, buffLoadTask);

            }

        }

        internal void Start()
        {
            auxBuff.Start();
        }

        internal void AppendAuxBufferAtThresh(object sender, EventArgs e)
        {
            if (numAuxEvent - (numLoadsCompleted * numEventPerLoad) > numEventPerLoad)
            {
                //Console.WriteLine("file2aux: loading:" + numEventPerLoad + "more aux events. " + numLoadsCompleted + " have been completed");
                LoadAuxEvent(olauxfile, (int)numEventPerLoad);
                auxBuff.writeToBuffer(auxDataChunk); //add N more stimuli
                numLoadsCompleted++;
            }
            else
            {
                if (!lastLoad)
                {
                    // load the last few auxevents
                    //Console.WriteLine("file2aux: last load");
                    LoadAuxEvent(olauxfile, (int)(numAuxEvent - numLoadsCompleted * numEventPerLoad));
                    auxBuff.writeToBuffer(auxDataChunk); //add N more stimuli
                    lastLoad = true;
                }
            }
        }

        internal void AuxBuff_Complete(object sender, EventArgs e)
        {
            Console.WriteLine("AUXILIARY-OUTPUT STOP CALLED");
            AlertAllFinished(this, e);
        }

        internal void AuxBuff_DAQLoadCompleted(object sender, EventArgs e)
        {
            // Report protocol progress
            int percentComplete = (int)Math.Round((double)100 * (auxBuff.numBuffLoadsCompleted) / numBuffLoadsRequired);
        }

        internal void LoadAuxEvent(StreamReader olAuxFile, int numEventToRead)
        {
            int j = 0;
            auxDataChunk = new List<AuxOutEvent>();

            if (auxFileExists)
            {
                while (j < numEventToRead)
                {
                    line = olAuxFile.ReadLine();
                    if (line == null)
                        break;

                    // load aux time
                    eventTime = Convert.ToUInt64(line);

                    // load aux chan
                    line = olAuxFile.ReadLine();
                    eventChannel = Convert.ToInt16(line);

                    // load aux voltage
                    line = olAuxFile.ReadLine();
                    eventVoltage = Convert.ToDouble(line);

                    //Append digital data
                    auxDatum = new AuxOutEvent(eventTime, (ushort)eventChannel, eventVoltage);
                    auxDataChunk.Add(auxDatum);

                    j++;
                }
            }

        }
    }
}
