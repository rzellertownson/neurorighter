using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NeuroRighter.Output;
using NeuroRighter.DataTypes;
using NeuroRighter.DatSrv;
using System.Threading;
using NationalInstruments.DAQmx;
using NationalInstruments.UI.WindowsForms;
using NationalInstruments.UI;
using simoc.plotting;
using simoc.UI;
using simoc.spk2obs;
using simoc.targetfunc;
using simoc.srv;
using simoc.obs2filt;
using simoc.filt2out;
using simoc.extensionmethods;
using simoc.filewriting;
using simoc.persistantstate;
using System.IO.Ports;

namespace simoc
{
    public partial class Simoc : ClosedLoopExperiment
    {

        private void ObservableSwitched()
        {
            simocVariableStorage.ResetRunningObsAverage();
        }

        private void TargetFunctionSwitched(object sender, TargetEventArgs e)
        {
            //
            ulong[] now = DatSrv.SpikeSrv.EstimateAvailableTimeRange();

            if (e.ResetEventTime)
            {
                simocVariableStorage.LastTargetSwitchedSec = (double)now[1] / DatSrv.SpikeSrv.SampleFrequencyHz;
                simocVariableStorage.TargetOn = false;
            }

            // Reset integral error
            simocVariableStorage.GenericDouble3 = 0;
        }

        private void SetupSerialComm()
        {
            try
            {
                System.ComponentModel.IContainer components = new System.ComponentModel.Container();
                serialPort1 = new SerialPort(components);
                serialPort1.PortName = "COM3";
                serialPort1.BaudRate = 9600;
                serialPort1.Open();

                if (!serialPort1.IsOpen)
                {
                    Console.WriteLine("Failed to connect to device.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Starting serial-comm mediated stimulator.");

            // Turns on the serial port
            serialPort1.DtrEnable = true;

            // callback for text coming back from the arduino
            serialPort1.DataReceived += OnReceived;

            //// give it 2 secs to start up the sketch
            //System.Threading.Thread.Sleep(2000);

            Console.WriteLine("Serial-communication established.");
        }

        private void CloseSerialEventHandler()
        {
            if (serialPort1 != null)
                serialPort1.Close();
        }

        private void OnReceived(object sender, SerialDataReceivedEventArgs c)
        {
            try
            {
                // write out text coming back from the arduino
                Console.Write(serialPort1.ReadExisting());
            }
            catch (Exception exc)
            {
                Console.Write(exc.Message);
            }
        }

    }
}
