using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NeuroRighter.Properties;

namespace NeuroRighter.Aquisition
{
    /// <summary>
    /// This class contains methods for creating and returning the tasks needed to create data streams for spike aquisition in NR.
    /// </summary>
    class SpikeTaskSetup
    {
        internal List<Task> AITaskCollection = new List<Task>();

        public SpikeTaskSetup(int numDevices, int numChannelsPerDev )
        {
            for (int i = 0; i < numDevices; ++i)
            {
                // Create Task for analog input
                AITaskCollection.Add(new Task("analogInTask_" + i));

                //Create virtual channels for analog input
                for (int j = 0; j < numChannelsPerDev; ++j)
                {
                    AITaskCollection[i].AIChannels.CreateVoltageChannel(Properties.Settings.Default.AnalogInDevice[i] + "/ai" + j.ToString(),
                        "", AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                }
            }
        }

        internal List<Task> GetAITaskCollection()
        {
            return AITaskCollection;
        }
    }
}
