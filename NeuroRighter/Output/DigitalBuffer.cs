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
using NeuroRighter.dbg;

namespace NeuroRighter.Output
{
    // called when the 2+requested number of buffer loads have occured
    //internal delegate void StimulationCompleteHandler(object sender, EventArgs e);
    // called when the Queue falls below a user defined threshold
    //internal delegate void QueueLessThanThresholdHandler(object sender, EventArgs e);
    // called when the stimBuffer finishes a DAQ load
    //internal delegate void DAQLoadCompletedHandler(object sender, EventArgs e);

    public class DigitalBuffer : NROutBuffer<DigitalOutEvent>
    {
        internal DigitalBuffer(int INNERBUFFSIZE, int STIM_SAMPLING_FREQ, int queueThreshold)
            : base(INNERBUFFSIZE, STIM_SAMPLING_FREQ, queueThreshold) { }

        // Intneral Properties
       
       

        internal void Setup(DigitalSingleChannelWriter digitalOutputWriter, Task digitalOutputTask, Task buffLoadTask, RealTimeDebugger Debugger)
        {
            //encapsulate the tasks and writer given into arrays
            DigitalSingleChannelWriter[] digitalWriters = new DigitalSingleChannelWriter[1];
            digitalWriters[0] = digitalOutputWriter;

            Task[] digitalTasks = new Task[1];
            digitalTasks[0]=digitalOutputTask;

            base.Setup(new AnalogMultiChannelWriter[0],digitalWriters,new Task[0],digitalTasks,buffLoadTask,Debugger);
            

        }



        protected override void writeEvent(DigitalOutEvent stim, ref List<double[,]> anEventValues, ref List<uint[]> digEventValues)
        {
            anEventValues = null;
            digEventValues = new List<uint[]>();
            digEventValues.Add(new uint[1]);
            digEventValues.ElementAt(0)[0] = stim.Byte;
            //Console.Write(stim.Byte.ToString() + " ");
        }

       

       

       

       

        
        
        

       
    }

}
