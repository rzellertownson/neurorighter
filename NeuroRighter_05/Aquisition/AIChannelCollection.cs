using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments;
using NationalInstruments.DAQmx;
using NeuroRighter.Properties;
using System.Collections.Specialized;

namespace NeuroRighter.Aquisition
{
    /// <summary>
    /// This class contains methods for creating and returning the tasks needed to create data streams for spike aquisition
    /// and aux analog data in NR.
    /// </summary>
    class NRAIChannelCollection
    {

        private int numDevices;
        private int numChannelsPerDev;
        private StringCollection physicalChannels;

        public NRAIChannelCollection(int numDevices, int numChannelsPerDev)
        {
            this.numDevices = numDevices;
            this.numChannelsPerDev = numChannelsPerDev;
        }

        public NRAIChannelCollection(StringCollection physicalChannels)
        {
            this.physicalChannels = physicalChannels;
        }

        internal void SetupSpikeCollection(ref List<Task> AITaskCollection)
        {

            for (int i = 0; i < numDevices; ++i)
            {
                // Create Task for analog input
                AITaskCollection.Add(new Task("spikeInTask_" + i));

                //Create virtual channels for analog input
                for (int j = 0; j < numChannelsPerDev; ++j)
                {
                    AITaskCollection[i].AIChannels.CreateVoltageChannel(Properties.Settings.Default.AnalogInDevice[i] + "/ai" + j.ToString(),
                        "", AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
                }
            }
        }

        internal void SetupAuxCollection(ref Task auxAITask)
        {
            //Create virtual channels for analog input
            for (int j = 0; j < physicalChannels.Count; ++j)
            {
                auxAITask.AIChannels.CreateVoltageChannel(physicalChannels[j],
                    "", AITerminalConfiguration.Nrse, -10.0, 10.0, AIVoltageUnits.Volts);
            }
        }
    }
}
