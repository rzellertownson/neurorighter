using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using NeuroRighter.dbg;


namespace NeuroRighter.Output
{
    class File2Stim
    {
        RealTimeDebugger debugger;
        StreamReader olstimfile; // The stream reader for the .olstim file being used
        internal string stimfile; // ascii file containing all nessesary stimulation info as produced by the matlab script makestimfile.m
        internal string line; // line from the .olstim file
        internal int wavesize; // the number of samples per stimulation waveform
        internal int numstim; // number of stimuli specified in open-loop file
        internal int numStimPerLoad = 50; // Number of stimuli loaded per read of the olstim file
        internal int numLoadsCompleted = 0; // Number loads completed
        internal ulong numBuffLoadsRequired; // Number of DAQ loads needed to complete an openloop experiment
        internal bool lastLoad;

        internal double[] cannedWaveform; // if the user is just repeating the same waveform, then this holds that info
        internal ulong[] TimeVector; //interstim times (NX1 vector)
        internal int[] ChannelVector; // stimulation locations (NX1 vector)
        internal double[,] WaveMatrix; // stimulation waveforms (NXM vector, M samples per waveform)

        internal StimBuffer stimbuff;
        private BackgroundWorker bw;//loads stimuli into the buffer when needed
        private Task buffLoadTask;//stimDigitalTask, stimAnalogTask,
        string masterLoad;
        private Task masterTask;
        //private DigitalSingleChannelWriter stimDigitalWriter;
        //private AnalogMultiChannelWriter stimAnalogWriter;

        // Stimulation Constants
        internal Int32 BUFFSIZE; // Number of samples delivered to DAQ per buffer load
        internal int STIM_SAMPLING_FREQ;

        //Event Handling
        //internal delegate void ProgressChangedHandler(object sender, EventArgs e, int percentage);
        //internal event ProgressChangedHandler AlertProgChanged;
        internal delegate void AllFinishedHandler(object sender, EventArgs e);
        internal event AllFinishedHandler AlertAllFinished;

        //internal File2Stim(string stimfile, int STIM_SAMPLING_FREQ, Int32 BUFFSIZE, Task stimDigitalTask,
        //    Task stimAnalogTask, Task buffLoadTask, DigitalSingleChannelWriter stimDigitalWriter,
        //    AnalogMultiChannelWriter stimAnalogWriter, RealTimeDebugger debugger)
        //{

        //    this.stimfile = stimfile;
            
        //    //Get references to tasks
        //    this.BUFFSIZE = BUFFSIZE;
        //    this.stimDigitalTask = stimDigitalTask;
        //    this.stimAnalogTask = stimAnalogTask;
        //    this.buffLoadTask = buffLoadTask;
        //    this.stimDigitalWriter = stimDigitalWriter;
        //    this.stimAnalogWriter = stimAnalogWriter;
        //    this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
        //    this.debugger = debugger;

        //    stimbuff = new StimBuffer(BUFFSIZE, STIM_SAMPLING_FREQ, 2, numStimPerLoad);
        //}

        internal File2Stim(string stimfile, int STIM_SAMPLING_FREQ, Int32 BUFFSIZE, Task buffLoadTask, Task masterTask, string masterLoad, RealTimeDebugger debugger, double[] cannedWave)
        {

            this.stimfile = stimfile;

            //Get references to tasks
            this.BUFFSIZE = BUFFSIZE;
            
            this.buffLoadTask = buffLoadTask;
            this.masterTask = masterTask;
            this.masterLoad = masterLoad;
            this.STIM_SAMPLING_FREQ = STIM_SAMPLING_FREQ;
            this.cannedWaveform = cannedWave;
            this.debugger = debugger;

            stimbuff = new StimBuffer(BUFFSIZE, STIM_SAMPLING_FREQ, 2, numStimPerLoad);
        }

        internal void Stop()
        {
            stimbuff.Stop();
        }

        internal bool Setup()
        {
            // Load the stimulus buffer
            stimbuff.QueueLessThanThreshold += new QueueLessThanThresholdHandler(AppendStimBufferAtThresh);
            // Stop the StimBuffer When its finished
            stimbuff.StimulationComplete +=new StimulationCompleteHandler(StimbuffStimulationComplete);
            // Alert that stimbuff just completed a DAQ bufferload
            stimbuff.DAQLoadCompleted += new DAQLoadCompletedHandler(StimbuffDAQLoadCompleted);
            
            //open .olstim file
            olstimfile = new StreamReader(stimfile);
            line = olstimfile.ReadLine(); // one read to get through header

            line = olstimfile.ReadLine(); // this read has the number of stimuli
            numstim = Convert.ToInt32(line); // find the number of stimuli specified in the file

            line = olstimfile.ReadLine(); // this read has the final stimulus time
            double finalStimTime = Convert.ToDouble(line); // find the number of stimuli specified in the file
            stimbuff.CalculateLoadsRequired(finalStimTime); // inform the stimbuffer how many DAQ loads it needs to take care of
            
            //Compute the amount of bufferloads needed to take care of this stimulation experiment
            numBuffLoadsRequired = 3 + (uint)Math.Ceiling(finalStimTime*STIM_SAMPLING_FREQ / (double)stimbuff.GetBufferSize());

            line = olstimfile.ReadLine(); // this read has the number samples in each stimulus
            wavesize = Convert.ToInt32(line); // find the number of stimuli specified in the file

            // make sure that the user is getting stimulus waveform from the GUI if they did not provide a waveform
            if (wavesize == 0 && cannedWaveform == null)
            {
                string WaveformError = "Your .olstim file does not appear to have stimulus waveform data in it. " +
                                             "You can provide waveforms in the file following the makestimfile.m instructions. " +
                                             "Additionally, you can create a waveform using the GUI in the manual stimulation box" +
                                             "and select to use that for all stimuli with the checkbox in the open-loop stimulation box.";
                MessageBox.Show(WaveformError);
                return true;   // Tell everyone there was an error
            }
            else if (wavesize > 0 && cannedWaveform != null)
            {
                string WaveformError = "Your .olstim file has stimulus waveform data in it, but you are trying to provide a  " +
                                             "a waveform made using the GUI in the manual stimulation box. Please provide a .olstim file" +
                                             "without waveform data to use the GUI to make your stimulus waveform.";
                MessageBox.Show(WaveformError);
                return true; // Tell everyone there was an error
            }
            else if (wavesize == 0)
            {
                wavesize = cannedWaveform.Length;
            }

            // Half the size of the largest stimulus data array that your computer will have to put in memory
            int numFullLoads = (int)Math.Floor((double)numstim / (double)numStimPerLoad);

            
            if (2*numStimPerLoad > numstim)
            {
                // Create Stimulus data arrays, just load all the data because the file is not that big
                TimeVector = new ulong[numstim];
                ChannelVector = new int [numstim];
                WaveMatrix = new double [numstim, wavesize];

                // Load the stimuli
                LoadStimWithWave(olstimfile,numstim);

                // Append the first stimuli to the stim buffer
                Console.WriteLine("File2Stim : Only a single load is needed because there are less than " + 2 * numStimPerLoad + " stimuli");
                stimbuff.Append(TimeVector, ChannelVector, WaveMatrix);//append first N stimuli
                numLoadsCompleted = numstim;
                lastLoad = true;
                stimbuff.Setup(buffLoadTask,debugger,masterTask);// (stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask, buffLoadTask, debugger);

            }
            else
            {

                // Create Stimulus data arrays
                TimeVector = new ulong [numStimPerLoad];
                ChannelVector = new int [numStimPerLoad];
                WaveMatrix = new double[numStimPerLoad, wavesize];

                // Load the first stimuli
                LoadStimWithWave(olstimfile,numStimPerLoad);

                // Append the first stimuli to the stim buffer
                stimbuff.Append(TimeVector, ChannelVector, WaveMatrix);//append first N stimuli
                numLoadsCompleted++;
                stimbuff.Setup(buffLoadTask, debugger, masterTask);
                //stimbuff.Setup(stimAnalogWriter, stimDigitalWriter, stimDigitalTask, stimAnalogTask, buffLoadTask, debugger);

            }

            return false; // Tell everyone there were no errors
        }

        internal void Start()
        {
            stimbuff.Start();
        }

        internal void Kill()
        {
            stimbuff.Kill();
        }

        internal void AppendStimBufferAtThresh(object sender, EventArgs e)
        {
            if (numstim - (numLoadsCompleted * numStimPerLoad) > numStimPerLoad)
            {
                Console.WriteLine("file2stim4: normal load numstimperload:" + numStimPerLoad + " numLoadsCompleted:" + numLoadsCompleted);
                LoadStimWithWave(olstimfile, numStimPerLoad);
                stimbuff.Append(TimeVector, ChannelVector, WaveMatrix); //add N more stimuli
                numLoadsCompleted++;
            }
            else
            {
                if (!lastLoad)
                {

                    // load the last few stimuli
                    Console.Write("file2stim4: last load");
                    TimeVector = new ulong[numstim - numLoadsCompleted * numStimPerLoad];
                    ChannelVector = new int[numstim - numLoadsCompleted * numStimPerLoad];
                    WaveMatrix = new double[numstim - numLoadsCompleted * numStimPerLoad, wavesize];
                    LoadStimWithWave(olstimfile, numstim - numLoadsCompleted * numStimPerLoad);
                    stimbuff.Append(TimeVector, ChannelVector, WaveMatrix); //add N more stimuli

                    lastLoad = true;
                }
            }
        }

        internal void StimbuffStimulationComplete(object sender, EventArgs e)
        {
            Console.WriteLine("STIMULATION STOP CALLED");
            AlertAllFinished(this, e);
        }

        internal void StimbuffDAQLoadCompleted(object sender, EventArgs e)
        {
            // Report protocol progress
            int percentComplete = (int)Math.Round((double)100 * (stimbuff.numBuffLoadsCompleted) / numBuffLoadsRequired);
            //AlertProgChanged(this, e, percentComplete);
        }

        internal void LoadStimWithWave(StreamReader olstimFile, int numStimToRead)
        {
            int j = 0;
            char delimiter = ' ';
            string[] splitWave;
            while ( j <= numStimToRead - 1)
            {
                line = olstimFile.ReadLine();
                if (line == null)
                    break;
                        
                // load stim time
                TimeVector[j] = Convert.ToUInt64(line);

                // load stime chan
                line = olstimFile.ReadLine();
                ChannelVector[j] = Convert.ToInt32(line);
                
                // load stim waveform
                if (cannedWaveform == null)
                {
                    line = olstimFile.ReadLine();
                    splitWave = line.Split(delimiter);
                    for (int i = 0; i < splitWave.Length; ++i)
                    {
                        WaveMatrix[j, i] = Convert.ToDouble(splitWave[i]);
                    }
                }
                else
                { 
                    for (int i = 0; i < cannedWaveform.Length; ++i)
                    {
                        WaveMatrix[j, i] = cannedWaveform[i];
                    }
                }
                j++;
            }

        }

    }
}
