using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.NeuroRighterTask;
using NeuroRighter.DataTypes;
using NeuroRighter.Server;
using NeuroRighter.Network;

namespace NR_CL_Examples
{
    class StimSrv_UnitReaction : NRTask
    {
        // DEBUG
        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Jon\Desktop\NR-CL-Examples\internal-asdr.txt", false);

        private int[] units = { 21, 23 }; // unit that we will react to with a digital pulse
        private ulong lastSampleRead = 0;
        protected EventBuffer<SpikeEvent> newSpikes;
        ulong nextAvailableSample;

        protected override void Setup()
        {
            nextAvailableSample = 0;
        }

        protected override void Loop(object sender, EventArgs e)
        {
            // First, figure out what history of spikes we have
            ulong[] spikeTimeRange = NRDataSrv.SpikeSrv.EstimateAvailableTimeRange();

            // Do is there any new data yet?
            if (spikeTimeRange[1] > lastSampleRead)
            {
                // Try to get the number of spikes within the available time range
                newSpikes = NRDataSrv.SpikeSrv.ReadFromBuffer(lastSampleRead, spikeTimeRange[1]);

                // Update the last sample read
                lastSampleRead = spikeTimeRange[1];
            }
            else
            {
                return;
            }

            // Is my unit in here?
            List<SpikeEvent> unitGSpikes = new List<SpikeEvent>(0);
            List<SpikeEvent> unitTSpikes = new List<SpikeEvent>(0);
            unitGSpikes = newSpikes.Buffer.Where(x => x.Unit == units[0]).ToList();
            unitTSpikes = newSpikes.Buffer.Where(x => x.Unit == units[1]).ToList();

            // Get the current buffer sample and make sure that we are going
            // to produce stimuli that are in the future
            ulong currentLoad = NRStimSrv.StimOut.GetNumberBuffLoadsCompleted() + 1;
            nextAvailableSample = currentLoad * (ulong)NRStimSrv.GetBuffSize();

            // Create the output buffer
            List<DigitalOutEvent> DigitalOutBuffer = new List<DigitalOutEvent>();

            for (int i = 0; i < unitGSpikes.Count; i++)
            {
                // Use the native digital output server to send digital change
                DigitalOutBuffer.Add(new DigitalOutEvent(nextAvailableSample, 71));
                SpikeEvent sG = unitGSpikes[0];
                DigitalOutBuffer.Add(new DigitalOutEvent(nextAvailableSample + 10, (uint)sG.SampleIndex));
                //DigitalOutBuffer.Add(new DigitalOutEvent(nextAvailableSample + 20, 0));
            }
            for (int i = 0; i < unitTSpikes.Count; i++)
            {
                // Use the native digital output server to send digital change
                DigitalOutBuffer.Add(new DigitalOutEvent(nextAvailableSample, 84));
                SpikeEvent sT = unitTSpikes[0];
                DigitalOutBuffer.Add(new DigitalOutEvent(nextAvailableSample+10, (uint)sT.SampleIndex));
                //DigitalOutBuffer.Add(new DigitalOutEvent(nextAvailableSample + 20, 0));
            }
            
            if (DigitalOutBuffer.Count > 0)
                NRStimSrv.DigitalOut.WriteToBuffer(DigitalOutBuffer);
        }

        protected override void Cleanup()
        {
            Console.WriteLine("Terminating protocol...");
        }
    }
}