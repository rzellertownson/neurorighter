﻿// DEFS.CS
// Copyright (c) 2008-2011 John Rolston
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using NationalInstruments;
using NationalInstruments.DAQmx;
using rawType = System.Double;
using NationalInstruments.Analysis.Dsp.Filters;
using csmatio.types;
using NeuroRighter.Aquisition;
using NeuroRighter.Filters;
using NeuroRighter.SpikeDetection;
using NeuroRighter.FileWriting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using NeuroRighter.Server;
using NeuroRighter.DataTypes;
using NeuroRighter.Output;
using NeuroRighter.Log;
using ConsoleWidget;

namespace NeuroRighter
{
    ///<summary>Declarations for the NeuroRighter UI.</summary>
    ///<author>Jon Newman</author>
    sealed internal partial class NeuroRighter
    {
        # region Internal Variables
        internal DataSrv datSrv;
        internal StimSrv stimSrv;
        internal Logger Debugger;

        #endregion

        #region Private_Variables
        private List<Task> spikeTask;  //NI Tasks for reading data
        private Task lfpTask;
        private Task eegTask;
        private Task videoTask;  //To synch up Cineplex system
        private Task triggerTask; //To trigger everything simultaneously, using AO
        private Task stimDigitalTask;
        private Task stimPulseTask;
        private Task stimTimeTask; //Records timing of stim pulses
        private Task stimIvsVTask; //Determines whether stim is current or voltage controlled
        private Task spikeOutTask;
        private Task auxAnInTask; // For recording AI channels
        private Task auxDigInTask;
        private List<AnalogMultiChannelReader> spikeReader;
        
        private AnalogUnscaledReader lfpReader;
        private AnalogUnscaledReader eegReader;
        private DigitalSingleChannelWriter triggerWriter;
        private DigitalSingleChannelWriter stimDigitalWriter;
        private AnalogMultiChannelWriter stimPulseWriter;
        private AnalogMultiChannelReader stimTimeReader;
        private DigitalSingleChannelWriter stimIvsVWriter;
        private AnalogMultiChannelReader auxAnReader;
        private DigitalSingleChannelReader auxDigReader;
        private AsyncCallback spikeCallback;
        private AsyncCallback lfpCallback;
        private AsyncCallback eegCallback;
        private AsyncCallback auxAnCallback;
        private AsyncCallback auxDigCallback;
        private bool taskRunning;  //Shows whether data are being acquired or not
        //private bool shouldContinue;  //Allows repeated data aq.
        private string filenameOutput;
        private string filenameBase;
        private string originalNameBase;

        // Buffers
        private List<AnalogWaveform<double>[]> spikeData;
        //private AnalogWaveform<double>[] stimDataTmp;
        private double[,] auxAnData;
        private int spikeBufferLength;  //How much data is acquired per read
        private int lfpBufferLength;
        private int eegBufferLength;
        private int eegDownsample;
        private BesselBandpassFilter[] eegFilter;
        private ButterworthFilter[] spikeFilter; //Testing my own filter
        private ButterworthFilter[] lfpFilter;
        private int[] numSpkWfms;   //Num. of plotted spike waveforms per channel
        private short[,] lfpData;
        private short[,] eegData;
        private rawType[][] filtSpikeData;
        private rawType[][] filtLFPData;
        private rawType[][] filtEEGData;
        private rawType[][] finalLFPData; //Stores filtered and downsampled LFP
        private double[] stimDataBuffer; //Stores some of the old samples for the next read (to avoid reading only half an encoding during a buffer read)
        private uint lastDigState = 0; // stores the digtial state after a port change occurs
        private uint[] auxDigData;
        //private DigitalWaveform auxDigitalWaveform;
        //private List<StimulusOutEvent> stimulations;
        //{
        //    get { return _stimulations; }
        //}
        //Locations of threshold crossings
        

        // Recording Parameters
        private NRAIChannelCollection stimTimeChanSet;
        private NRAIChannelCollection auxChanSet;
        private bool twoAITasksOnSingleBoard = false;
        private double spikeSamplingRate;
        private double lfpSamplingRate;
        private double eegSamplingRate;
        private int[] numSpikeReads; //Number of times the spike buffer has been read (for adding time stamps)
        private List<int> numStimReads;
        private double stimJump;  //Num. of indices to jump ahead during stim reads (make sure to round before using)
        private List<double[]> scalingCoeffsSpikes; //Scaling coefficients for NI-DAQs
        private double[] scalingCoeffsLFPs;
        private double[] scalingCoeffsEEG;
        private rawType[] thrSALPA; //Thresholds for SALPA
        private SALPA3 SALPAFilter;
        private int numChannels;
        private int numChannelsPerDev;
        private int[] currentRef;
        private DateTime stimStopTime;
        private ArrayList stimList; //List of stimPulse objects
        private System.Threading.Timer stimTimer;
        private double[] eegOffset;
        private double eegDisplayGain;
        private SerialPort serialOut; //Serial port to control programmable referencing
        private List<StimTick> stimIndices; //For sending stim times to SALPA (or any other routine that wants them)
        private PlotData spikePlotData;
        private PlotData lfpPlotData;
        private PlotData muaPlotData;
        private PlotData eegPlotData;
        private EventPlotData waveformPlotData;
        private Filters.Referencer referncer;
        private DateTime experimentStartTime;
        private Filters.ArtiFilt artiFilt;
        private ChannelOutput BNCOutput;
        private DateTime timedRecordingStopTime;
        private Filters.MUAFilter muaFilter;
        private double[][] muaData;
        private int SALPA_WIDTH;
        private bool isNormalRecording = true;
        private bool isCLRecording = false;
        private bool[] bwIsRunning;

        //Plots
        private List<Microsoft.Xna.Framework.Color> NRBrainbow;
        private List<Microsoft.Xna.Framework.Color> NRSnipBrainbow;
        private List<Microsoft.Xna.Framework.Color> NRUnitBrainbow;
        private GridGraph spikeGraph;
        private SnipGridGraph spkWfmGraph;
        private RowGraph lfpGraph;
        private RowGraph muaGraph;
        private RowGraph eegGraph;
        private ScatterGraphController auxInputGraphController;
        private short recordingLEDState = 0;
        private delegate void plotData_dataAcquiredDelegate(object item); //Used for plotting callbacks, thread-safety
        private delegate void crossThreadFormUpdateDelegate(int item); //Used for making cross thread calls from stimbuffer, file2stim, etc to NR
        private int numSnipsDisplayed;
        private bool updateAuxGraph = false;

        // Spike detection
        private SpikeDetSettings spikeDet;
        private static readonly object rawSpikeObj = new object();
        private static readonly object salpaSpikeObj = new object();
        private static readonly object finalSpikeObj = new object();

        // File Writing
        private int[] trackingReads;
        private int[] trackingProc;
        private int trackingDigReads;
        private RawScale neuralDataScaler;
        private RawScale auxDataScaler;
        private bool[] firstRawWrite;
        RecordingSetup recordingSettings;
        
        // Open-loop AO/DO 
        private OpenLoopOut openLoopSynchronizedOutput;
        private bool repeatOpenLoopProtocol;
        private double numOpenLoopRepeats;
        private double numOpenLoopsPerformed = 0;

        // Closed-loop AO/DO
        private ClosedLoopOut closedLoopSynchronizedOutput;
        
        // Console Control
        private ConsoleControl nrConsole;

        #endregion

        #region DebugVariables
#if (USE_LOG_FILE)
        internal StreamWriter logFile; //internal so other classes can access
#endif

        #endregion

        #region Constants
        private const int NUM_SECONDS_TRAINING = 3; //Num. seconds to train noise levels
        private int STIM_SAMPLING_FREQ = 100000; //Resolution at which stim pulse waveforms are generated
        private const int STIM_PADDING = 10; //Num. 0V samples on each side of stim. waveform 
        private const int STIM_BUFFER_LENGTH = 20;  //#pts. to keep in stim time reading buffer
        private int STIMBUFFSIZE = 10000; // Number of samples delivered to DAQ per buffer load during stimulation from file
        private const double VOLTAGE_EPSILON = 1E-7; //If two samples are within 100 nV, I'll call them "same"
        private const int MUA_DOWNSAMPLE_FACTOR = 50;
        private const short CHAN_INDEX_START = 0;
        private const long DAQ_BUFFER_SIZE_SAMPLES = 1000000;
         
        #endregion
    }
}
