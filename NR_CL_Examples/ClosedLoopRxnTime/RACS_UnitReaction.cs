using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter.Server;
using NeuroRighter.DataTypes;
using NeuroRighter.NeuroRighterTask;
using NeuroRighter.Network;

namespace NR_CL_Examples
{
    /// <summary>
    /// Communicate via TCP with RACS server for low latency stimulation.
    /// </summary>
    class RACS_UnitReaction : NRTask
    {
        // DEBUG
        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Users\Jon\Desktop\NR-CL-Examples\internal-asdr.txt", false);

        private int[] units = {21, 23}; // unit that we will react to with a digital pulse
        private ulong lastSampleRead = 0;
        protected EventBuffer<SpikeEvent> newSpikes;

        // TCP socket info. Of course, on the RT machine you need to have a TCP SERVER running that is capable of 
        // interpreting the commands you send. For the case of RACS this is conained in the perl scrips included
        // in the ./perl folder
        private TCPClient RACSCommunicator;
        const string server = "128.61.139.90"; // RACS IP
        const string port = "4545"; // RACS Port #

        // RACS command
        string cmdG = "0 digi 71 0 digi 0 400"; //"0 digi 71 0 digi 0 400"
        string cmdT = "0 digi 84 0 digi 0 400"; //"0 digi 84 0 digi 0 400"

        protected override void Setup()
        {
            // Get TCP socket setup
            RACSCommunicator = new TCPClient(server, port);
            try
            {
                RACSCommunicator.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to RACS Sever: " + ex.Message);
            }

            Console.WriteLine("Starting RT network mediated stimulator.");
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

            for (int i = 0; i < unitGSpikes.Count; i++)
            {
                // Use the TCP socket to send a command to RACS telling it to produce a digital pulse
                RACSCommunicator.SendString(cmdG);
            }
            for (int i = 0; i < unitTSpikes.Count; i++)
            {
                // Use the TCP socket to send a command to RACS telling it to produce a digital pulse
                RACSCommunicator.SendString(cmdT);
            }
        }

        protected override void Cleanup()
        {
            RACSCommunicator.Close();
        }
    }
}
