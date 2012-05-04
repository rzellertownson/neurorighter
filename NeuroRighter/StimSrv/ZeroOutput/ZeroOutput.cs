using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace NeuroRighter.StimSrv
{
    /// <summary>
    /// <title> ZeroOutputs</title>
    /// Class for zeroing the outputs of the NI cards in a given system. This class
    /// has methods for zeroing analog outputs and digital output ports.
    /// This class should  generally should be usedfollowing any type of
    /// stimulation method or experiment involving output so that the user is sure the
    /// system is "shut down".
    /// <author> Jon Newman </author>
    /// </summary>
    class ZeroOutput
    {
        //private int clearingBufferSize;
        //private int clearingSampleRate;

        public ZeroOutput() //int clearingBufferSize, int clearingSampleRate
        {
            //this.clearingBufferSize = clearingBufferSize;
            //this.clearingSampleRate = clearingSampleRate;
        }

        internal void ZeroAOChanOnDev(string dev, int[] channelsToZero)
        {
            lock (this)
            {
                try
                {
                    // Create an analog out task for a given device for all 4 channels.
                    // Write clearingBufferSize zeros to that port. Wait
                    // until this is finished and destroy the clearning Task.
                    Task analogClearingTask = new Task("AnalogClear");

                    foreach (int chan in channelsToZero)
                        analogClearingTask.AOChannels.CreateVoltageChannel(
                            "/" + dev + "/ao" + chan, "", -10.0, 10.0, AOVoltageUnits.Volts);

                    //analogClearingTask.Timing.ConfigureSampleClock("/" + dev + "/PF",
                    //    clearingSampleRate,
                    //    SampleClockActiveEdge.Rising,
                    //    SampleQuantityMode.FiniteSamples,
                    //    clearingBufferSize);
                    analogClearingTask.Timing.ReferenceClockSource = ("/" + dev + "/PFI2");
                    analogClearingTask.Timing.ReferenceClockRate = 10e6;

                    AnalogMultiChannelWriter analogClearingWriter = new
                        AnalogMultiChannelWriter(analogClearingTask.Stream);

                    double[] zeroData = new double[channelsToZero.Length];
                    analogClearingWriter.BeginWriteSingleSample(false, zeroData, null, null);
                    analogClearingTask.Control(TaskAction.Verify);
                    analogClearingTask.Start();
                    //analogClearingWriter.WriteSingleSample(true, zeroData);
                    //analogClearingWriter.WriteMultiSample(true, zeroData);
                    analogClearingTask.WaitUntilDone(30);
                    analogClearingTask.Stop();
                    analogClearingTask.Dispose();
                    analogClearingTask = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not zero analog outputs on device: " + dev);
                    Console.WriteLine(e.Message);
                }
            }
        }

        internal void ZeroPortOnDev(string dev, int port)
        {
            lock (this)
            {
                try
                {
                    // Create a digital out task for a given device and port.
                    // Write clearingBufferSize zeros to that port. Wait
                    // until this is finished and destroy the clearning Task.
                    Task digitalClearingTask = new Task("DigiClear");
                    digitalClearingTask.DOChannels.CreateChannel("/" + dev + "/Port" + port, "", 
                        ChannelLineGrouping.OneChannelForAllLines);
                    //digitalClearingTask.Timing.ConfigureSampleClock("100KHzTimeBase",
                    //    clearingSampleRate,
                    //    SampleClockActiveEdge.Rising,
                    //    SampleQuantityMode.FiniteSamples,
                    //    clearingBufferSize);
                    DigitalSingleChannelWriter digitalClearingWriter = new DigitalSingleChannelWriter(digitalClearingTask.Stream);
                    digitalClearingWriter.BeginWriteSingleSamplePort(true,0,null,null);
                    digitalClearingTask.WaitUntilDone(30);
                    digitalClearingTask.Stop();
                    digitalClearingTask.Dispose();
                    digitalClearingTask = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(" Could not zero digital output on device: " 
                        + dev + "/" + port);
                }
            }
        }

       
    }
}
