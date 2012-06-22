using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.NeuroRighterTask;
using NeuroRighter.Server;

namespace NewDataEventCatcher
{
    /// <summary>
    /// This NRTask uses data driven events to trigger a response from the plugin. When new spikes are available on the DataSrv.SpikeSrv server object,
    /// it fires a NewData event. We can subscribe an event handler to this function within the NRTask as shown below. 
    /// This is the lowest latency option for producing feedback.
    /// </summary>
    public class NRNewDataCatcher : NRTask
    {
        protected override void Setup()
        {
            // Subscribe to the NewData event on the spikes input server
            NRDataSrv.SpikeSrv.NewData += new EventDataSrv<NeuroRighter.DataTypes.SpikeEvent>.NewDataHandler(SpikeSrv_NewData);


        }

        protected override void Loop(object sender, EventArgs e)
        {
            // Do nothing
        }

        protected override void Cleanup()
        {
            // Do nothing
        }

        // Here is the NewData event handler
        private void SpikeSrv_NewData(object sender, NewDataEventArgs eArgs)
        {
            // Write a line to the console saying what the first and last index of the new spikes are.
            Console.WriteLine("New Spikes added to buffer with min index :" + eArgs.FirstNewSample + " and max index: " + eArgs.LastNewSample);
        }
    }
}
