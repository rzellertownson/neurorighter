// NeuroRighter
// Copyright (c) 2008-2009 John Rolston
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

//#define DEBUG_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;

//This class serves to replicate the experiment in 
//Bakkum, Chao, Potter 2008

namespace NeuroRighter
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal class BakkumExpt
    {
        private StimTrain CPS;
        private List<StimTrain> PTS;
        private List<StimTrain> SBS; //correspondes 1-to-1 with PTS
        private List<List<Int32>> PTSInterTrainIntervals;
        private List<SpikeWaveform> waveforms;
        private List<double> probabilities; //For PTSes
        private double[] T;
        private MEAChannelMappings meaMap;
        private double desiredDirection; //in radians
        private int lastPTSIndex = -1;
        private StreamWriter outputFileWriter;
        private AutoResetEvent _blockExecution = new AutoResetEvent(false);
        private double voltage;

        private Task stimDigitalTask, stimAnalogTask;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimAnalogWriter;

        private const Int32 NUM_CPS_PULSES = 6;
        private const Int32 NUM_PTS_PULSES = 6;
        private Int32[] INTERPULSE_INTERVAL_RANGE = { 200, 400 }; //in milliseconds [x, y]
        private const Int32 INTERPULSE_INTERVAL_TRAIN = 10; //in milliseconds, for PTS, SBS
        private const Int32 PULSE_WIDTH = 400; //in us, per phase
        private const Double PULSE_AMPLITUDE = 0.3; //in volts
        private const Int32 NUM_PTS_TRAINS = 100;
        private const Double MAX_PROBABILITY = 0.5 - double.Epsilon; //add epsilons to ensure our precision is okay
        private const Double MIN_PROBABILITY = 0.002 + double.Epsilon;

        private const Double DIRECTION_TOLERANCE = Math.PI / 6; //in radians

        //State variables
        private Boolean isCancelled;

        private BackgroundWorker bw;

        private Random rand;

        //'availableContextChannels' is a list of channels from which to construct CPS, 1-based
        //'availableProbeChannels' is ibid for the final probe pulse
        internal BakkumExpt(List<Int32> availableContextChannels, List<Int32> availableProbeChannels,
            Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
            : this(availableContextChannels, availableProbeChannels, PULSE_AMPLITUDE,
                stimDigitalTask, stimAnalogTask, stimDigitalWriter, stimAnalogWriter) { }

        internal BakkumExpt(List<Int32> availableContextChannels, List<Int32> availableProbeChannels, double voltage, 
            Task stimDigitalTask, Task stimAnalogTask, DigitalSingleChannelWriter stimDigitalWriter,
            AnalogMultiChannelWriter stimAnalogWriter)
        {
            waveforms = new List<SpikeWaveform>(100);
            this.voltage = voltage;

            //Initialize random number generator
            rand = new Random();

            meaMap = new MEAChannelMappings();

            //Get references to tasks
            this.stimDigitalTask = stimDigitalTask;
            this.stimAnalogTask = stimAnalogTask;
            this.stimDigitalWriter = stimDigitalWriter;
            this.stimAnalogWriter = stimAnalogWriter;

            #region ConstructCPS
            //********************
            //Construct CPS
            //********************

            //Declare vars
            List<int> CPSChannels = new List<int>(NUM_CPS_PULSES + 1);
            List<int> interpulseIntervals = new List<int>(NUM_CPS_PULSES);

            //Pick context electrodes
            for (int i = 0; i < NUM_CPS_PULSES; ++i)
            {
                int index = (int)Math.Floor(availableContextChannels.Count * rand.NextDouble());
                CPSChannels.Add(availableContextChannels[index]);
                availableContextChannels.RemoveAt(index); //To ensure no repeats, and to ensure PTSes don't include these electrodes
            }

            //Pick probe electrode
            CPSChannels.Add(availableProbeChannels[(int)Math.Floor(availableProbeChannels.Count * rand.NextDouble())]);

            //Pick interpulse intervals
            int range = INTERPULSE_INTERVAL_RANGE[1] - INTERPULSE_INTERVAL_RANGE[0] + 1; //+1 since range is inclusive
            for (int i = 0; i < NUM_CPS_PULSES; ++i)
                interpulseIntervals.Add(INTERPULSE_INTERVAL_RANGE[0] + (int)Math.Floor(range * rand.NextDouble()));

            //Make CPS
            CPS = new StimTrain(PULSE_WIDTH, voltage, CPSChannels, interpulseIntervals);
            CPS.populate(); //Fill samples (might take a while)
            #endregion

            #region ConstructPTSes
            //******************
            //Construct PTSes,SBSes
            //******************

            //Declare vars
            PTS = new List<StimTrain>(NUM_PTS_TRAINS);
            resetProbabilities();
            SBS = new List<StimTrain>(NUM_PTS_TRAINS);
            PTSInterTrainIntervals = new List<List<Int32>>(NUM_PTS_TRAINS);
            for (int i = 0; i < NUM_PTS_TRAINS; ++i)
                PTSInterTrainIntervals.Add(new List<Int32>());
            List<Int32> withinPTSIntervals = new List<int>(NUM_PTS_PULSES - 1);
            for (int i = 0; i < NUM_PTS_PULSES - 1; ++i)
                withinPTSIntervals.Add(INTERPULSE_INTERVAL_TRAIN);

            //Choose channels for each PTS
            for (int i = 0; i < NUM_PTS_TRAINS; ++i)
            {
                List<int> channels = new List<int>(NUM_PTS_PULSES);

                for (int j = 0; j < NUM_PTS_PULSES; ++j)
                    channels.Add(availableContextChannels[(int)Math.Floor(availableContextChannels.Count * rand.NextDouble())]);

                PTS.Add(new StimTrain(PULSE_WIDTH, voltage, channels, withinPTSIntervals));
                //Hold off on populating till later
                SBS.Add(new StimTrain(PULSE_WIDTH, voltage, channels, withinPTSIntervals));
                SBS[i].shuffleChannelOrder();

                //Make intervals
                int total = 0;
                do
                {
                    int val = INTERPULSE_INTERVAL_RANGE[0] + (int)Math.Floor(range * rand.NextDouble());
                    PTSInterTrainIntervals[i].Add(val);
                    total += val;
                } while (total < 4200); //less than 4.2s
            }
            #endregion

            T = new double[4];
        }

        //TODO: start function w/ switch statements for different experimental parts;
        //should be a finite state machine essentially
        internal void start()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "txt";
            sfd.Filter = "Text files (*.txt)|*.txt";
            sfd.FileName = "Experiment001";

            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                outputFileWriter = new StreamWriter(sfd.OpenFile());

                bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(bw_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                bw.WorkerSupportsCancellation = true;
                bw.RunWorkerAsync();
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            outputFileWriter.Flush();
            outputFileWriter.Close();
            outputFileWriter.Dispose();
        }

        internal void stop() { isCancelled = true; }

        private void bw_DoWork(Object sender, DoWorkEventArgs e)
        {
            TimeSpan SBS_PRE_EXPT_LENGTH = new TimeSpan(4, 0, 0); //hours, minutes, seconds, usually 4 hrs
            TimeSpan SBS_PRE_CL_LENGTH = new TimeSpan(0, 30, 0);    //30 min
            TimeSpan SBS_POST_CL_LENGTH = new TimeSpan(1, 0, 0);    //1 hr
            TimeSpan SBS_ONLY_MEASURE_T = new TimeSpan(0, 30, 0); //30 min
            TimeSpan CL_LENGTH = new TimeSpan(2, 0, 0); //2 hrs

            //Print file header info
            outputFileWriter.WriteLine("Closed-loop Learning Experiment\r\n\r\nProgrammed by John Rolston (rolston2@gmail.com)\r\n\r\n");

            //Get starting time
            outputFileWriter.WriteLine("Starting time: " + DateTime.Now);
            outputFileWriter.Flush();
            DateTime endTime = DateTime.Now + SBS_PRE_EXPT_LENGTH;

            Boolean isDone = false;
            stimAnalogTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;
            stimDigitalTask.Timing.SampleQuantityMode = SampleQuantityMode.FiniteSamples;

            #region SBS_Pre-experiment
            //SBS-pre-experiment
            while (!isDone && !isCancelled)
            {
                //Deliver SBS
                deliverSBS();

                //Deliver CPS
                deliverCPS();

                //Check to see if time's up
                if (DateTime.Now > endTime)
                    isDone = true;
            }
            #endregion

            #region Closed-loop_Training
            List<double> desiredDirections = new List<double>(4);
            desiredDirections.Add(Math.PI / 4);
            desiredDirections.Add(3 * Math.PI / 4);
            desiredDirections.Add(5 * Math.PI / 4);
            desiredDirections.Add(7 * Math.PI / 4);

            for (int d = 0; d < desiredDirections.Count; ++d)
            {
                //Set a goal direction
                desiredDirection = desiredDirections[d];

                #region Pre-Closed-loop_SBS
                //Do pre-CL SBS
                endTime = DateTime.Now + SBS_PRE_CL_LENGTH;
                isDone = false;
                while (!isDone && !isCancelled)
                {
                    //Deliver SBS
                    deliverSBS();

                    //Deliver CPS
                    deliverCPS();

                    //Check to see if time's up
                    if (DateTime.Now > endTime)
                        isDone = true;
                }
                #endregion

                #region SBS_Measure_T

                endTime = DateTime.Now + SBS_ONLY_MEASURE_T;
                isDone = false;

                CPS.unpopulate();
                CPS.populate(true); //populate so that it measures CA

                List<MEAChannelMappings.Coords> CAList = new List<MEAChannelMappings.Coords>(100);

                while (!isDone && !isCancelled)
                {
                    //Deliver SBS
                    deliverSBS();

                    //Deliver CPS
                    deliverCPS();

                    //Wait until spikes are measured
                    _blockExecution.WaitOne(); 

                    //Measure CA
                    CAList.Add(CA(false));

                    //Check to see if time's up
                    if (DateTime.Now > endTime) isDone = true;
                }

                //Update T
                double offsetX = 0;
                double offsetY = 0;

                double scaleX = 0;
                double scaleY = 0;
                outputFileWriter.WriteLine("Measured CAs, during pre-experiment (x,y):");
                int usedCAs = 0;
                for (int i = 0; i < CAList.Count; ++i)
                {
                    if (Double.IsNaN(CAList[i].x) || Double.IsNaN(CAList[i].y))
                    {
                        outputFileWriter.WriteLine("\t" + i + ": NaN (ignored)");
                    }
                    else
                    {
                        offsetX += CAList[i].x;
                        offsetY += CAList[i].y;
                        outputFileWriter.WriteLine("\t" + i + ": " + CAList[i].x + ", " + CAList[i].y);
                        ++usedCAs;
                    }
                }
                outputFileWriter.WriteLine("\n");
                offsetX /= usedCAs;
                offsetY /= usedCAs;

                for (int i = 0; i < CAList.Count; ++i)
                {
                    if (!(Double.IsNaN(CAList[i].x)) && !(Double.IsNaN(CAList[i].y)))
                    {
                        scaleX += Math.Abs(CAList[i].x - offsetX);
                        scaleY += Math.Abs(CAList[i].y - offsetY);
                    }
                }
                scaleX /= usedCAs;
                scaleY /= usedCAs;

                T[0] = 1.0 / scaleX;
                T[1] = -offsetX; //this differs from Doug's code a bit
                T[2] = 1.0 / scaleY;
                T[3] = -offsetY;

                outputFileWriter.WriteLine("Transform matrix, T = " + T[0] + "\t" + T[1] + "\t" + T[2] + "\t" + T[3] + "\n");

                //Clear CAList
                CAList.Clear();
                #endregion

                #region Closed-loop
                //Do closed-loop portion
                outputFileWriter.WriteLine("Training Round " + d + ", Desired Direction (rads) = " + desiredDirection);
                outputFileWriter.WriteLine("Time = " + DateTime.Now + "\n");
                outputFileWriter.Flush();

                endTime = DateTime.Now + CL_LENGTH;
                isDone = false;
                while (!isDone && !isCancelled)
                {
                    //Do CPS
                    deliverCPS();

                    //Wait until measurement is done
                    _blockExecution.WaitOne();

                    //Check output direction, after listening to 100 ms of spikes
                    if (goingCorrectDirection())
                    {
                        outputFileWriter.WriteLine("Time: " + DateTime.Now + "\r\nO Moving in correct direction.");
                        updateProbabilities(true, lastPTSIndex);
                        deliverSBS();
                    }

                    //Else, it's incorrect: deliver PTS
                    else
                    {
                        outputFileWriter.WriteLine("Time: " + DateTime.Now + "\r\nX Moving in incorrect direction.");
                        updateProbabilities(false, lastPTSIndex);
                        //Select and deliver PTS
                        deliverPTS();
                    }

                    outputFileWriter.Flush();
                    if (DateTime.Now > endTime) isDone = true;
                }

                //Reset probabilities of each PTS, since we're done with this experiment
                resetProbabilities();
                #endregion

                #region Post-Closed-loop_SBS
                //Do post-closed-loop SBS
                endTime = DateTime.Now + SBS_POST_CL_LENGTH;
                isDone = false;
                while (!isDone && !isCancelled)
                {
                    //Deliver SBS
                    deliverSBS();

                    //Deliver CPS
                    deliverCPS();

                    //Check to see if time's up
                    if (DateTime.Now > endTime)
                        isDone = true;
                }
                #endregion
            }
            #endregion
        }

        private void deliverCPS()
        {
            //Assumes CPS is initialized, does not check

            //Do CPS
            stimAnalogTask.Timing.SamplesPerChannel = CPS.analogPulse.GetLength(1);
            stimDigitalTask.Timing.SamplesPerChannel = CPS.digitalData.GetLength(0);

            stimAnalogWriter.WriteMultiSample(true, CPS.analogPulse);
            if (Properties.Settings.Default.StimPortBandwidth == 32)
                stimDigitalWriter.WriteMultiSamplePort(true, CPS.digitalData);
            else if (Properties.Settings.Default.StimPortBandwidth == 8)
                stimDigitalWriter.WriteMultiSamplePort(true, StimPulse.convertTo8Bit(CPS.digitalData));
            stimDigitalTask.WaitUntilDone();
            stimAnalogTask.WaitUntilDone();
            stimAnalogTask.Stop();
            stimDigitalTask.Stop();

            //Wait 100 ms while listening to spikes
            //System.Threading.Thread.Sleep(100);
            //Not necessary with waitone() construct
        }

        private void deliverSBS()
        {
            //Code assumes rand is initialized, does not check
            int index = (int)Math.Floor(SBS.Count * rand.NextDouble());

            for (int i = 0; i < PTSInterTrainIntervals[index].Count; ++i)
            {
                SBS[index].shuffleChannelOrder();
                SBS[index].populate();

                //Setup pulse timing
                stimAnalogTask.Timing.SamplesPerChannel = SBS[index].analogPulse.GetLength(1);
                stimDigitalTask.Timing.SamplesPerChannel = SBS[index].digitalData.GetLength(0);

                stimAnalogWriter.WriteMultiSample(true, SBS[index].analogPulse);
                stimDigitalWriter.WriteMultiSamplePort(true, SBS[index].digitalData);

                SBS[index].unpopulate();
                stimDigitalTask.WaitUntilDone();
                stimAnalogTask.WaitUntilDone();
                stimAnalogTask.Stop();
                stimDigitalTask.Stop();

                //Wait for intertrain time
                System.Threading.Thread.Sleep(PTSInterTrainIntervals[index][i]);
            }

            lastPTSIndex = -1; //This makes it so that probability updates don't do anything when the last stim sequence was an SBS
        }

        private void deliverPTS()
        {
            //First, select appropriate PTS
            //Begin by making a random double
            double r = rand.NextDouble();

            //Now, cycle through probabilities, till we exceed the drawn number
            double currPD = 0.0; //current probability density, as in how much we've covered of the PDF so far
            int index = -1; //This refers to selected PTS
            while (currPD < r)
                currPD += probabilities[++index];

            //Deliver PTS
            for (int i = 0; i < PTSInterTrainIntervals[index].Count; ++i)
            {
                PTS[index].populate();

                //Setup pulse timing
                stimAnalogTask.Timing.SamplesPerChannel = PTS[index].analogPulse.GetLength(1);
                stimDigitalTask.Timing.SamplesPerChannel = PTS[index].digitalData.GetLength(0);

                stimAnalogWriter.WriteMultiSample(true, PTS[index].analogPulse);
                stimDigitalWriter.WriteMultiSamplePort(true, PTS[index].digitalData);

                PTS[index].unpopulate();
                stimDigitalTask.WaitUntilDone();
                stimAnalogTask.WaitUntilDone();
                stimAnalogTask.Stop();
                stimDigitalTask.Stop();

                //Wait for intertrain time
                //System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
                //st.Start();
                System.Threading.Thread.Sleep(PTSInterTrainIntervals[index][i]);
                //st.Stop();
                //outputFileWriter.WriteLine("Desired interval: " + PTSInterTrainIntervals[index][i] + ", observed: " + st.ElapsedMilliseconds);
                //outputFileWriter.Flush();
            }
            lastPTSIndex = index;
        }

        private void resetProbabilities()
        {
            if (probabilities == null)
                probabilities = new List<double>(NUM_PTS_TRAINS);
            else
                probabilities.Clear();
            for (int i = 0; i < NUM_PTS_TRAINS; ++i)
                probabilities.Add(1.0 / (double)NUM_PTS_TRAINS);
        }

        private void updateProbabilities(bool isCorrect, int index)
        {
            outputFileWriter.WriteLine("\tPTS Index: " + index);
            
            if (index >= 0) //Prevents updates if last pulse train was an SBS
            {
                outputFileWriter.WriteLine("\t\tPrior probability: " + probabilities[index]);
                if (isCorrect)
                {
                    if (probabilities[index] < MAX_PROBABILITY)
                    {
                        double newProb = 1.5 / (0.5 + 1 / probabilities[index]);
                        if (newProb > MAX_PROBABILITY) newProb = MAX_PROBABILITY;
                        double dp = newProb - probabilities[index]; //diff b/w new and old prob for winning PTS

                        List<int> lowIndices = new List<int>(5); //list of indices where subtracting probability would push them below min
                        List<int> goodIndices = new List<int>(NUM_PTS_TRAINS - 1);
                        for (int i = 0; i < index; ++i) goodIndices.Add(i);
                        for (int i = index + 1; i < NUM_PTS_TRAINS; ++i) goodIndices.Add(i);

                        int previousCount = -1;
                        int currGood;

                        while (previousCount < lowIndices.Count) //Repeat tests until we're stable
                        {
                            previousCount = lowIndices.Count;
                            currGood = goodIndices.Count;

                            for (int i = goodIndices.Count - 1; i >= 0; --i)
                            {
                                if (probabilities[i] - dp / currGood < MIN_PROBABILITY)
                                {
                                    lowIndices.Add(goodIndices[i]);
                                    goodIndices.RemoveAt(i);
                                }
                            }
                        }

                        if (goodIndices.Count > 0)
                        {
                            for (int i = 0; i < goodIndices.Count; ++i)
                                probabilities[goodIndices[i]] -= dp / goodIndices.Count;

                            probabilities[index] = newProb;
                        }
                    }
                }
                else //Direction was not correct
                {
                    if (probabilities[index] > MIN_PROBABILITY)
                    {
                        double newProb = 0.5 / (-0.5 + 1 / probabilities[index]);
                        if (newProb < MIN_PROBABILITY) newProb = MIN_PROBABILITY;
                        double dp = -newProb + probabilities[index]; //diff b/w new and old prob for winning PTS

                        List<int> lowIndices = new List<int>(5); //list of indices where subtracting probability would push them below min
                        List<int> goodIndices = new List<int>(NUM_PTS_TRAINS - 1);
                        for (int i = 0; i < index; ++i) goodIndices.Add(i);
                        for (int i = index + 1; i < NUM_PTS_TRAINS; ++i) goodIndices.Add(i);

                        int previousCount = -1;
                        int currGood;

                        while (previousCount < lowIndices.Count) //Repeat tests until we're stable
                        {
                            previousCount = lowIndices.Count;
                            currGood = goodIndices.Count;

                            for (int i = goodIndices.Count - 1; i >= 0; --i)
                            {
                                if (probabilities[i] + dp / currGood > MAX_PROBABILITY)
                                {
                                    lowIndices.Add(goodIndices[i]);
                                    goodIndices.RemoveAt(i);
                                }
                            }
                        }

                        if (goodIndices.Count > 0)
                        {
                            for (int i = 0; i < goodIndices.Count; ++i)
                                probabilities[goodIndices[i]] += dp / goodIndices.Count;

                            probabilities[index] = newProb;
                        }
                    }
                }
                outputFileWriter.WriteLine("\t\tNew probability: " + probabilities[index]);
            }
        }


        internal void linkToSpikes(NeuroRighter nr) { nr.spikesAcquired += new NeuroRighter.spikesAcquiredHandler(spikeAcquired); }
        private void spikeAcquired(object sender, bool inTrigger)
        {
            NeuroRighter nr = (NeuroRighter)sender;
            lock (this)
            {
                lock (nr)
                {
                    //Add all waveforms to local buffer
                    while (nr.waveforms.Count > 0)
                    {
                        waveforms.Add(nr.waveforms[0]);
#if (DEBUG_LOG)
                        nr.logFile.WriteLine("[BakkumExpt] Waveform added, index: " + nr.waveforms[0].index + "\n\r\tTime: " 
                            + DateTime.Now.Minute + ":" + DateTime.Now.Second + ":" + DateTime.Now.Millisecond);
                        nr.logFile.Flush();
#endif
                        nr.waveforms.RemoveAt(0);
                    }
                }
                if (!inTrigger)
                    _blockExecution.Set();
            }
        }

        private MEAChannelMappings.Coords CA(bool useTransformMatrix)
        {
            MEAChannelMappings.Coords xy = new MEAChannelMappings.Coords(0, 0);
            double[] firingRate = new double[64];
            double norm = 0; //sum of all firing rates, for normalization
            while (waveforms.Count > 0)
            {
                ++firingRate[waveforms[0].channel]; //channels are 0-based
                waveforms.RemoveAt(0);
            }
            for (int i = 0; i < MEAChannelMappings.usedChannels.Length; ++i)
                norm += firingRate[MEAChannelMappings.usedChannels[i]];

            //Calculate CA
            for (int i = 0; i < MEAChannelMappings.usedChannels.Length; ++i)
            {
                xy.x += firingRate[MEAChannelMappings.usedChannels[i]] * meaMap.CoordFromChannel[MEAChannelMappings.usedChannels[i]].x;
                xy.y += firingRate[MEAChannelMappings.usedChannels[i]] * meaMap.CoordFromChannel[MEAChannelMappings.usedChannels[i]].y;
            }

            //normalize to firing rate
            xy.x /= norm;
            xy.y /= norm;

            //Do transformation if applicable
            if (useTransformMatrix)
            {
                //Account for T
                //T entries are x scale, x offset, y scale, y offset
                xy.x += T[1];
                xy.y += T[3];
                xy.x *= T[0];
                xy.y *= T[2];
            }

            return xy;
        }

        private Boolean goingCorrectDirection()
        {
            MEAChannelMappings.Coords xy;
            lock (this)
            {
                xy = CA(true); //true since we want to account for transform
            }
            double currDirection = Math.Atan2(xy.y , xy.x);
            if (currDirection < 0) currDirection += 2 * Math.PI; //ensure range is [0, 2PI)
            outputFileWriter.WriteLine("Current direction (rads) = " + currDirection);

            if (currDirection < desiredDirection + DIRECTION_TOLERANCE && currDirection > desiredDirection - DIRECTION_TOLERANCE)
                return true;
            else
                return false;
        }
    }
}
