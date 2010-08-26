/* 
 * SignalServer
 * This class is a TCP/IP server for transferring recorded/simulated data
 *  over the network. Useful for communicating with Matlab.
 * 
 * Contributed by:
 * Alexandra Elbakyan
 * <mindwrapper@gmail.com>
 * August 2010
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NeuroRighter.Bufferization;
using System.Threading;
using System.ComponentModel;

namespace NeuroRighter.Networking
{
    class SignalServer
    {

        public delegate void clientStatusHandler(string clientIP, string status);
        public event clientStatusHandler clientStatus;

        DataBuffer dBuf;
        TcpListener tcpListener;

        Thread listenThread;
        List<SignalClient> myClients;

        private volatile bool shouldStop;

        public SignalServer(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            myClients = new List<SignalClient>();
        }

        public void connectBuffer(DataBuffer dataBuf)
        {
            this.dBuf = dataBuf;
        }

        public void startListening()
        {
            shouldStop = false;
            listenThread = new Thread(new ThreadStart(waitingForClients));
            listenThread.Start();
        }

        public void stopListening()
        {
            shouldStop = true;
            myClients.Clear();
            tcpListener.Stop();
            listenThread.Abort();
        }

        private void waitingForClients()
        {
            tcpListener.Start();
            while (true)
            {
                while (!tcpListener.Pending() && !shouldStop)
                    Thread.Sleep(10);
                if (shouldStop)
                    break;
                SignalClient client = new SignalClient(tcpListener.AcceptTcpClient(), this, dBuf);
            }
            tcpListener.Stop();
        }

        public void reportStatus(string clientIP, string status)
        {
            clientStatus(clientIP, status);
        }

    }
}
