using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace NeuroRighter.Output
{
    class File2Dig
    {

        private StreamReader oldigfile; // The stream reader for the .olstim file being used
        private string digfile; // ascii file containing all nessesary stimulation info as produced by the matlab script makestimfile.m
        private string line; // line from the .olstim file
        private ulong numDigEvent; // number of stimuli specified in open-loop file
        private ulong numEventPerLoad; // Number of stimuli loaded per read of the olstim file
        private ulong numLoadsCompleted = 0; // Number loads completed
        private ulong numBuffLoadsRequired; // Number of DAQ loads needed to complete an openloop experiment
        private bool lastLoad;

        internal List<DigitalData> DigitalDataChunk;
        private DigitalData DigitalDatum;
        private UInt32 Byte;
        private UInt64 EventTime;
        private DigitalBuffer digbuff;
        private BackgroundWorker bw;//loads stimuli into the buffer when needed
        private Task digitalOutputTask, buffLoadTask;
        private DigitalSingleChannelWriter digitalOutputWriter;

        // Stimulation Constants
        internal Int32 BUFFSIZE; // Number of samples delivered to DAQ per buffer load
        internal int STIM_SAMPLING_FREQ;

        //Event Handling
        //internal delegate void ProgressChangedHandler(object sender, EventArgs e, int percentage);
        //internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender, EventArgs e);
        internal event AllFinishedHandler AlertAllFinished;

        internal File2Dig(string digfile, int STIM_SAMPLING_FREQ, Int32 BUFFSIZE, Task digitalOutputTask,
            Task buffLoadTask, DigitalSingleChannelWriter digitalOutputWriter, ulong numEventPerLoad)
        {
            this.digfile = digfile;
            this.BUFFSIZE = BUFFSIZE;
            this.digitalOutputTask = digitalOutputTask;
            this.buffLoadTask = buffLoadTask;
            this.digitalOutputWriter = digitalOutputWriter;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
            this.numEventPerLoad = numEventPerLoad;
            this.lastLoad = false;

            // Instatiate a DigitalBuffer object
            digbuff = new DigitalBuffer(BUFFSIZE, STIM_SAMPLING_FREQ, (int)numEventPerLoad);

        }

        internal void Stop()
        {
            digbuff.Stop();
        }

        internal void Setup()
        {
            // Load the stimulus buffer
            digbuff.DigitalQueueLessThanThreshold += new DigitalQueueLessThanThresholdHandler(AppendDigBufferAtThresh);
            // Stop the StimBuffer When its finished
            digbuff.DigitalOutputComplete +=new DigitalOutputCompleteHandler(DigbuffComplete);
            // Alert that digbuff just completed a DAQ bufferload
            digbuff.DigitalDAQLoadCompleted += new DigitalDAQLoadCompletedHandler(DigbuffDAQLoadCompleted);
            
            //open .olstim file
            oldigfile = new StreamReader(digfile);
            line = oldigfile.ReadLine(); // one read to get through header

            line = oldigfile.ReadLine(); // this read has the number of stimuli
            numDigEvent = Convert.ToUInt64(line); // find the number of stimuli specified in the file
            digbuff.setNumberofEvents(numDigEvent);

            line = oldigfile.ReadLine(); // this read has the final stimulus time
            double finalEventTime = Convert.ToDouble(line); // find the number of stimuli specified in the file
            digbuff.CalculateLoadsRequired(finalEventTime); // inform the stimbuffer how many DAQ loads it needs to take care of
            
            //Compute the amount of bufferloads needed to take care of this stimulation experiment
            numBuffLoadsRequired = digbuff.numBuffLoadsRequired;

            // Half the size of the largest stimulus data array that your computer will have to put in memory
            int numFullLoads = (int)Math.Floor((double)numDigEvent / (double)numEventPerLoad);

            
            if (2*numEventPerLoad >= numDigEvent)
            {
                // Load the stimuli
                LoadDigEvent(oldigfile, (int)numEventPerLoad);

                // Append the first stimuli to the stim buffer
                Console.WriteLine("All in one digital load");
                digbuff.append(DigitalDataChunk); // Append all the stimuli
                numLoadsCompleted = numDigEvent;
                lastLoad = true;
                digbuff.setup(digitalOutputWriter, digitalOutputTask, buffLoadTask);

            }
            else
            {
                // Load the first stimuli
                LoadDigEvent(oldigfile, (int)numEventPerLoad);

                // Append the first stimuli to the stim buffer
                digbuff.append(DigitalDataChunk);//append first N stimuli
                numLoadsCompleted++;
                digbuff.setup(digitalOutputWriter, digitalOutputTask, buffLoadTask);

            }

        }

        internal void Start()
        {
            digbuff.start();
        }

        internal void AppendDigBufferAtThresh(object sender, EventArgs e)
        {
            if (numDigEvent - (numLoadsCompleted * numEventPerLoad) > numEventPerLoad)
            {
                
                LoadDigEvent(oldigfile, (int)numEventPerLoad);
                digbuff.append(DigitalDataChunk); //add N more stimuli
                numLoadsCompleted++;
                Console.WriteLine("file2dig: normal load numstimperload:" + numEventPerLoad + " numLoadsCompleted:" + numLoadsCompleted);
            }
            else
            {
                if (!lastLoad)
                {
                    // load the last few stimuli
                    Console.WriteLine("file2dig: last load");
                    LoadDigEvent(oldigfile, (int)(numDigEvent - numLoadsCompleted * numEventPerLoad));
                    digbuff.append(DigitalDataChunk); //add N more stimuli
                    lastLoad = true;
                }
            }
        }

        internal void DigbuffComplete(object sender, EventArgs e)
        {
            Console.WriteLine("DIGITAL-OUTPUT STOP CALLED");
            AlertAllFinished(this, e);
        }

        internal void DigbuffDAQLoadCompleted(object sender, EventArgs e)
        {
            // Report protocol progress
            int percentComplete = (int)Math.Round((double)100 * (digbuff.numBuffLoadsCompleted) / numBuffLoadsRequired);
            //AlertProgChanged(this, e, percentComplete);
        }

        internal void LoadDigEvent(StreamReader oldigFile, int numEventToRead)
        {
            int j = 0;
            DigitalDataChunk = new List<DigitalData>();

            while ( j < numEventToRead)
            {
                
                line = oldigFile.ReadLine();
                if (line == null)
                    break;
                        
                // load stim time
                EventTime = Convert.ToUInt64(line);

                // load stime chan
                line = oldigFile.ReadLine();
                Byte = Convert.ToUInt32(line);

                //Append digital data
                DigitalDatum = new DigitalData(EventTime, Byte);
                DigitalDataChunk.Add(DigitalDatum);

                j++;
            }

        }
    }
}
