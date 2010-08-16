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
                clientThreads.Add(clientThread);
                clientThread.Start(client);
            }
            tcpListener.Stop();
        }

 
        private void processClient(object tcpClient)
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            myClients.Add(((TcpClient)tcpClient));
            string clientIP = ((TcpClient)tcpClient).Client.RemoteEndPoint.ToString();
            clientStatus(clientIP, "connected");
            NetworkStream clientStream = ((TcpClient)tcpClient).GetStream();
            byte[] message = new byte[4 + dBuf.numChannels];
            int bR;
            List<int[]> myList = null;
            int dataChunkLen = dBuf.samplingRate;
            byte[] sendBuf = new byte[dBuf.numChannels * dataChunkLen * sizeof(double)];
            bool[] selectedChannels = new bool[dBuf.numChannels];
            int selectedChannelsCnt = dBuf.numChannels;
            bool applyFilter = false;
            ButterworthFilter dataFilter = null;
            bool decimate = false;
            int newSamplingRate = 0;
            int sampleStep = 0;
            int decChunkLen = 0;
//            DateTime now, prev;
//            List<System.TimeSpan> dt = new List<TimeSpan>();
            int i, j;
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
                    selectedChannelsCnt = 0;
                    for (int c = 0; c < dBuf.numChannels; c++)
                    {
                        selectedChannels[c] = BitConverter.ToBoolean(message, 4 + c);
                        if (selectedChannels[c])
                            selectedChannelsCnt++;
                    }
                    dataChunkLen = (int)((double)dataChunkLen_msec / 1000 * dBuf.samplingRate);
                    sendBuf = new byte[selectedChannelsCnt * dataChunkLen * sizeof(double)];
                    myList = null;
                }
                if (cmd == "f")
                {
                    try { bR = clientStream.Read(message, 0, 2 * sizeof(double)); } catch { break; }; if (bR == 0) { break; }
                    double lowCut = BitConverter.ToDouble(message, 0);
                    double highCut = BitConverter.ToDouble(message, sizeof(double));
                    dataFilter = new ButterworthFilter(1, dBuf.samplingRate, lowCut, highCut, sendBuf.Length);
                    applyFilter = true;
                }
                if (cmd == "d")
                {
                    try { bR = clientStream.Read(message, 0, sizeof(Int32)); } catch { break; }; if (bR == 0) { break; }
                    newSamplingRate = BitConverter.ToInt32(message, 0);
                    decChunkLen = (int)((double)dataChunkLen * newSamplingRate / dBuf.samplingRate);
                    sampleStep = (int)((double)dataChunkLen / decChunkLen);
                    sendBuf = new byte[decChunkLen * selectedChannelsCnt * sizeof(double)];
                    decimate = true;
                }
                if (cmd == "r")
                {
//                    now = DateTime.Now;
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
                    i = 0;
                    int np = 0;
                    double[][] tmpBuf = new double[selectedChannelsCnt][];
                    for (int c = 0; c < selectedChannelsCnt; c++)
                        tmpBuf[c] = new double[dataChunkLen];
                    dBuf.rwLock.EnterReadLock();
                    while (myList.Count > 0 && np < dataChunkLen)
                    {
                        int[] newP = myList[0];
                        int k;
                        for (k = newP[0]; np < dataChunkLen && k < newP[0] + newP[1]; k++, np++)
                            for (int c = 0, cs = 0; c < dBuf.numChannels; c++)
                                if (selectedChannels[c])
                                {
                                    tmpBuf[cs][np] = dBuf.data[k, c];
                                    //byte[] tmpBuf = BitConverter.GetBytes(dBuf.data[k, c]);
                                    //for (int ti = 0; ti < 8; ti++, i++)
                                    //    sendBuf[i] = tmpBuf[ti];
                                    cs++;
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

                    if (applyFilter)
                    {
                        for (int c = 0; c < tmpBuf.GetLength(0); c++)
                            dataFilter.filterData(tmpBuf[c]);
                    }
                    int copyLen;
                    if (decimate)
                    {
                        for (int c = 0; c < tmpBuf.GetLength(0); c++)
                            for (i = 0, j = 0; i < dataChunkLen; i += sampleStep, j++)
                                tmpBuf[c][j] = tmpBuf[c][i];
                        copyLen = decChunkLen;
                    }
                    else
                        copyLen = dataChunkLen;
                    for (int c = 0; c < tmpBuf.GetLength(0); c++)
                        Buffer.BlockCopy(tmpBuf[c], 0, sendBuf, c * copyLen * sizeof(double), copyLen * sizeof(double));

                    try { clientStream.Write(sendBuf, 0, sendBuf.Length); } catch { break; };
//                    dt.Add(DateTime.Now - now);
                    continue;
                }
            }
            clientStatus(clientIP, "disconnected");
            if (myList != null)
                dBuf.detachClient(myList);
            if (((TcpClient)tcpClient).Connected)
                ((TcpClient)tcpClient).Client.Disconnect(false);
            myClients.Remove(((TcpClient)tcpClient));
        }



    }
}
