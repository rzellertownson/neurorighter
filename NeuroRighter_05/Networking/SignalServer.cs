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
        List<TcpClient> myClients;
        List<Thread> clientThreads;

        private volatile bool shouldStop;

        public SignalServer(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            myClients = new List<TcpClient>();
            clientThreads = new List<Thread>();
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
            foreach (TcpClient client in myClients)
                client.Client.Disconnect(false);
            foreach (Thread clientThread in clientThreads)
                clientThread.Abort();
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
                TcpClient client = tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(processClient));
                myClients.Add(client);
                clientThreads.Add(clientThread);
                clientThread.Start(client);
            }
            tcpListener.Stop();
        }

        private void processClient(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            string clientIP = tcpClient.Client.RemoteEndPoint.ToString();
            //clientIP = clientIP.Remove(clientIP.IndexOf(':'));
            clientStatus(clientIP, "connected");
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] message = new byte[4 + dBuf.numChannels];
            int bR;
            List<int[]> myList = null;
            int dataChunkLen = dBuf.samplingRate;
            byte[] sendBuf = new byte[dBuf.numChannels * dataChunkLen * sizeof(double)];
            while (!shouldStop)
            {
                bR = 0;
                try { bR = clientStream.Read(message, 0, 1); } catch { break; }; if (bR == 0) { break; }
                ASCIIEncoding encoder = new ASCIIEncoding();
                string cmd = encoder.GetString(message, 0, bR);
                if (cmd == "i")
                {
                    byte[] info_nChan = BitConverter.GetBytes(dBuf.numChannels);
                    byte[] info_sRate = BitConverter.GetBytes(dBuf.samplingRate);
                    try { clientStream.Write(info_nChan, 0, info_nChan.Length); } catch { break; };
                    try { clientStream.Write(info_sRate, 0, info_sRate.Length); } catch { break; };
                    continue;
                }
                if (cmd == "s")
                {
                    try { bR = clientStream.Read(message, 0, 4 + dBuf.numChannels); } catch { break; }; if (bR == 0) { break; }
                    int dataChunkLen_msec = BitConverter.ToInt32(message, 0);
                    dataChunkLen = (int)((double)dataChunkLen_msec / 1000 * dBuf.samplingRate);
                    sendBuf = new byte[dBuf.numChannels * dataChunkLen * sizeof(double)];
                    myList = null;
                }
                if (cmd == "r")
                {
                    clientStatus(clientIP, "transferring");
                    if (myList == null)
                        myList = dBuf.connectClient();
                    int newPointsCnt;
                    do
                    {
                        Thread.Sleep(5);
                        newPointsCnt = 0;
                        foreach (int[] newP in myList)
                            newPointsCnt = newPointsCnt + newP[1];
                    } while (newPointsCnt < dataChunkLen);
                    int i = 0;
                    int np = 0;
                    dBuf.rwLock.EnterReadLock();
                    while (myList.Count > 0 && np < dataChunkLen)
                    {
                        int[] newP = myList[0];
                        int k;
                        for (k = newP[0]; np < dataChunkLen && k < newP[0] + newP[1]; k++, np++)
                            for (int c = 0; c < dBuf.numChannels; c++)
                            {
                                byte[] tmpBuf = BitConverter.GetBytes(dBuf.data[c, k]);
                                for (int ti = 0; ti < 8; ti++, i++)
                                    sendBuf[i] = tmpBuf[ti];
                            }
                        if (np >= dataChunkLen && k-newP[0] > 0)
                        {
                            myList[0][0] = k;
                            myList[0][1] = k - newP[0];
                        }
                        else
                            myList.RemoveAt(0);
                    }
                    dBuf.rwLock.ExitReadLock();
                    try { clientStream.Write(sendBuf, 0, sendBuf.Length); } catch { break; };
                    continue;
                }
            }
            clientStatus(clientIP, "disconnected");
            if (myList != null)
                dBuf.detachClient(myList);
            if (tcpClient.Connected)
                tcpClient.Client.Disconnect(false);
            myClients.Remove(tcpClient);
        }

    }
}
