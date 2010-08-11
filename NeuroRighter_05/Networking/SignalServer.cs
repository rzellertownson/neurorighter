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

namespace NeuroRighter.Networking
{
    class SignalServer
    {

        DataBuffer dBuf;
        TcpListener tcpListener;
        Thread listenThread;

        public SignalServer(DataBuffer dataBuf, int port)
        {
            this.dBuf = dataBuf;
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();
            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            byte[] message = new byte[32];
            int bytesRead;
            List<int[]> myList = null;
            int dataChunkLen = dBuf.samplingRate;
            byte[] sendBuf = new byte[dBuf.numChannels * dataChunkLen * sizeof(double)];
            while (true)
            {
                bytesRead = 0;
                try
                {
                    bytesRead = clientStream.Read(message, 0, 1);
                }
                catch
                {
                    break;
                }
                if (bytesRead == 0)
                {
                    break;
                }
                ASCIIEncoding encoder = new ASCIIEncoding();
                string cmd = encoder.GetString(message, 0, bytesRead);
                if (cmd == "i")
                {
                    byte[] info_nChan = BitConverter.GetBytes(dBuf.numChannels);
                    byte[] info_sRate = BitConverter.GetBytes(dBuf.samplingRate);
                    clientStream.Write(info_nChan, 0, info_nChan.Length);
                    clientStream.Write(info_sRate, 0, info_sRate.Length);
                    continue;
                }
                if (cmd == "s" || cmd == "c")
                {
                    if (cmd == "s")
                    {
                        clientStream.Read(message, 0, 4);
                        int dataChunkLen_msec = BitConverter.ToInt32(message, 0);
                        dataChunkLen = (int)((double)dataChunkLen_msec / 1000 * dBuf.samplingRate);
                        clientStream.Read(message, 0, dBuf.numChannels);
                        myList = dBuf.connectClient();
                        sendBuf = new byte[dBuf.numChannels * dataChunkLen * sizeof(double)];
                    }
                    int newPointsCnt;
                    do
                    {
                        Thread.Sleep(50);
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
                    clientStream.Write(sendBuf, 0, sendBuf.Length);
                    continue;
                }
            }
            if (myList != null)
                dBuf.detachClient(myList);
            tcpClient.Close();
        }

    }
}
