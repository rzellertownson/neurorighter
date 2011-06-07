using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using NationalInstruments.DAQmx;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using NeuroRighter.DataTypes;

namespace NeuroRighter.Output
{

    // called when the 2+requested number of buffer loads have occured
    internal delegate void AuxOutputCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    internal delegate void AuxQueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    internal delegate void AuxDAQLoadCompletedHandler(object sender, EventArgs e);

    public class AuxBuffer : NROutBuffer<AuxOutEvent>
    {
        internal AuxBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
            : base(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold) { }

        internal void Setup(AnalogMultiChannelWriter auxOutputWriter, Task auxOutputTask, Task buffLoadTask)
        {
            AnalogMultiChannelWriter[] analogWriters = new AnalogMultiChannelWriter[1];
            analogWriters[0] = auxOutputWriter;

            Task[] analogTasks = new Task[1];
            analogTasks[0] = auxOutputTask;

            base.Setup(analogWriters,new DigitalSingleChannelWriter[0], analogTasks, new Task[0],  buffLoadTask);
            

        }
        //with this version only one channel can have a non-zero voltage at a time-  eventually might want to switch to a version where keep voltages from previous
        protected override void writeEvent(AuxOutEvent stim, ref List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            anEventValues = new List<double[,]>();
            anEventValues.Add(new double[4,1]);
            
                anEventValues.ElementAt(0)[stim.eventChannel-1, 0] = stim.eventVoltage;
            
            
            digEventValues = null;
        }
        

        

        

        

        

       

        

        

       
        
    }
}
