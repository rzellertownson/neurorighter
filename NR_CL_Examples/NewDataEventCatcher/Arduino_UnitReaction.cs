using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.NeuroRighterTask;
using NeuroRighter.DataTypes;
using NeuroRighter.Server;
using System.IO.Ports;

namespace ClassLibrary1
{
    /// <summary>
    /// This is a copy of the original Arduino-based CL reaction time NRTask, 
    /// but using data driven events to trigger a response from the plugin.
    /// </summary>
    class Arduino_UnitReaction : NRTask
    {
        private int[] units = { 40, 90 }; // unit that we will react to with a digital pulse
        protected EventBuffer<SpikeEvent> newSpikes;

        // Serial comm 
        private SerialPort serialPort1;

        protected override void Setup()
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

            // give it 2 secs to start up the sketch
            System.Threading.Thread.Sleep(2000);

            // Subscribe to the NewData event on the spikes input server
            NRDataSrv.SpikeSrv.NewData += new EventDataSrv<NeuroRighter.DataTypes.SpikeEvent>.NewDataHandler(SpikeSrv_NewData);

            Console.WriteLine("Serial-communication established.");
        }

        protected override void Loop(object sender, EventArgs e)
        {
            // Do nothing
        }

        protected override void Cleanup()
        {
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

        // Here is the NewData event handler
        private void SpikeSrv_NewData(object sender, NewDataEventArgs eArgs)
        {
            // Get the new spikes
            newSpikes = NRDataSrv.SpikeSrv.ReadFromBuffer(eArgs.FirstNewSample, eArgs.LastNewSample);

            // Is my unit in here?
            List<SpikeEvent> unitGSpikes = new List<SpikeEvent>(0);
            List<SpikeEvent> unitTSpikes = new List<SpikeEvent>(0);
            unitGSpikes = newSpikes.Buffer.Where(x => x.Unit == units[0]).ToList();
            unitTSpikes = newSpikes.Buffer.Where(x => x.Unit == units[1]).ToList();

            for (int i = 0; i < unitGSpikes.Count; i++)
            {
                // Use the serial port to send a command to the Arduino 
                serialPort1.Write(new byte[] { 1 }, 0, 1);
            }
            for (int i = 0; i < unitTSpikes.Count; i++)
            {
                // Use the serial port to send a command to the Arduino 
                serialPort1.Write(new byte[] { 2 }, 0, 1);
            }

        }
    }
}
