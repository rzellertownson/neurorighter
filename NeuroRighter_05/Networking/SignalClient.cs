/* 
 * SignalClient
 * Performs TCP/IP communication / data exchange with remote client (i.e. Matlab)
 * Used by SignalServer
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
using System.Net.Sockets;
using System.Threading;
using NeuroRighter.Bufferization;

namespace NeuroRighter.Networking
{
    class SignalClient
    {
        TcpClient myClient;
        string clientIP;
        NetworkStream clientStream;
        Thread myClientThread;
        SignalServer myServer;

        DataBuffer dataBuffer;
        List<int[]> newDataPointsList = null;
        int dataChunkLen;
        byte[] sendataBuffer;
        bool[] selectedChannels;
        int selectedChannelsCnt;

        bool applyDecimation = false;
        int newSamplingRate = 0;
        int sampleStep = 0;
        int decChunkLen = 0;

        bool applyFiltering = false;
        ButterworthFilter[] dataFilter = null;
        double lowCut;
        double highCut;
        int order;

        public SignalClient(TcpClient myClient, SignalServer myServer)
        {
            this.myServer = myServer;
            this.myClient = myClient;
            clientIP = myClient.Client.RemoteEndPoint.ToString();
            clientStream = myClient.GetStream();
            clientStream.ReadTimeout = 5000;
            myClientThread = new Thread(new ThreadStart(processRequests));
            myClientThread.Start();
        }

        ~SignalClient()
        {
            if (myClient.Connected)
                myClient.Client.Disconnect(false);
            myClientThread.Abort();
            if (newDataPointsList != null)
                dataBuffer.detachClient(newDataPointsList);
            myServer.reportStatus(clientIP, "disconnected");
        }

        void processRequests()
        {
            myServer.reportStatus(clientIP, "connected");
            byte[] message;
            string cmd;
            while (true)
            {
                if (!readMessageOfSpecLen(1, out message))
                    break;
                cmd = Encoding.ASCII.GetString(message, 0, 1);
                try
                {
                    switch (cmd)
                    {
                        case "b": cmdSelectData();
                            break;
                        case "i": cmdInfo();
                            break;
                        case "s": cmdSetup();
                            break;
                        case "r": cmdGetData();
                            break;
                        case "f": cmdFilter();
                            break;
                        case "d": cmdDecimate();
                            break;
                    }
                }
                catch
                {
                    break;
                }
            }
            myServer.reportStatus(clientIP, "disconnected");
        }

        void cmdSelectData()
        {
            byte[] bufferType;
            if (!readMessageOfSpecLen(3, out bufferType))
                return;
            switch (Encoding.ASCII.GetString(bufferType))
            {
                case "lfp":
                    dataBuffer = myServer.getBuf(DataType.lfp);
                    break;
                case "raw":
                    dataBuffer = myServer.getBuf(DataType.raw);
                    break;
            }
        }

        void cmdInfo()
        {
            byte[] info_nChan = BitConverter.GetBytes(dataBuffer.numChannels);
            byte[] info_sRate = BitConverter.GetBytes(dataBuffer.samplingRate);
            clientStream.Write(info_nChan, 0, info_nChan.Length);
            clientStream.Write(info_sRate, 0, info_sRate.Length);
        }

        void cmdSetup()
        {
            byte[] options;
            if (!readMessageOfSpecLen(4 + dataBuffer.numChannels, out options))
                return;
            int dataChunkLen_msec = BitConverter.ToInt32(options, 0);
            clientStream.ReadTimeout = (int)((double)dataChunkLen_msec * 3);
            selectedChannels = new bool[dataBuffer.numChannels];
            selectedChannelsCnt = 0;
            for (int c = 0; c < dataBuffer.numChannels; c++)
            {
                selectedChannels[c] = BitConverter.ToBoolean(options, 4 + c);
                if (selectedChannels[c])
                    selectedChannelsCnt++;
            }
            dataChunkLen = (int)((double)dataChunkLen_msec / 1000 * dataBuffer.samplingRate);
            sendataBuffer = new byte[selectedChannelsCnt * dataChunkLen * sizeof(double)];
            if (applyFiltering)
                resetFilter();
         }

        void cmdGetData()
        {
            myServer.reportStatus(clientIP, "transferring");

            int i, j, k, c, cs, np;

            if (newDataPointsList == null)
                newDataPointsList = dataBuffer.connectClient();
            int newPointsCnt;
            do
            {
                Thread.Sleep(5);
                newPointsCnt = 0;
                foreach (int[] newP in newDataPointsList)
                    newPointsCnt = newPointsCnt + newP[1];
            } while (newPointsCnt < dataChunkLen);

            double[][] tmpBuf = new double[selectedChannelsCnt][];
            for (c = 0; c < selectedChannelsCnt; c++)
                tmpBuf[c] = new double[dataChunkLen];

            np = 0;
            dataBuffer.rwLock.EnterReadLock();
            while (newDataPointsList.Count > 0 && np < dataChunkLen)
            {
                int[] newP = newDataPointsList[0];
                for (k = newP[0]; np < dataChunkLen && k < newP[0] + newP[1]; k++, np++)
                    for (c = 0, cs = 0; c < dataBuffer.numChannels; c++)
                        if (selectedChannels[c])
                        {
                            tmpBuf[cs][np] = dataBuffer.data[k, c];
                            cs++;
                        }
                if (np >= dataChunkLen && k - newP[0] > 0)
                {
                    newDataPointsList[0][0] = k;
                    newDataPointsList[0][1] = k - newP[0];
                }
                else
                    newDataPointsList.RemoveAt(0);
            }
            dataBuffer.rwLock.ExitReadLock();

            if (applyFiltering)
            {
                for (c = 0; c < selectedChannelsCnt; c++)
                    dataFilter[c].filterData(tmpBuf[c]);
            }

            int copyLen;
            if (applyDecimation)
            {
                for (c = 0; c < selectedChannelsCnt; c++)
                    for (i = 0, j = 0; i < dataChunkLen; i += sampleStep, j++)
                        tmpBuf[c][j] = tmpBuf[c][i];
                copyLen = decChunkLen;
            }
            else
                copyLen = dataChunkLen;

            for (c = 0; c < selectedChannelsCnt; c++)
                Buffer.BlockCopy(tmpBuf[c], 0, sendataBuffer, c * copyLen * sizeof(double), copyLen * sizeof(double));

            clientStream.Write(sendataBuffer, 0, sendataBuffer.Length);
        }

        void cmdFilter()
        {
            byte[] fOptions;
            if (!readMessageOfSpecLen(2 * sizeof(double) + sizeof(int), out fOptions))
                return;
            lowCut = BitConverter.ToDouble(fOptions, 0);
            highCut = BitConverter.ToDouble(fOptions, sizeof(double));
            order = BitConverter.ToInt32(fOptions, sizeof(double) * 2);
            resetFilter();
            applyFiltering = true;
        }

        void resetFilter()
        {
            dataFilter = new ButterworthFilter[selectedChannelsCnt];
            for (int c = 0; c < selectedChannelsCnt; c++)
                dataFilter[c] = new ButterworthFilter(order, dataBuffer.samplingRate, lowCut, highCut, dataChunkLen);
        }

        void cmdDecimate()
        {
            byte[] decOptions;
            if (!readMessageOfSpecLen(sizeof(Int32), out decOptions))
                return;
            newSamplingRate = BitConverter.ToInt32(decOptions, 0);
            decChunkLen = (int)((double)dataChunkLen * newSamplingRate / dataBuffer.samplingRate);
            sampleStep = (int)((double)dataChunkLen / decChunkLen);
            sendataBuffer = new byte[decChunkLen * selectedChannelsCnt * sizeof(double)];
            applyDecimation = true;
        }

        private bool readMessageOfSpecLen(int toReadCnt, out byte[] data)
        {
            int bytesReadJust = 0;
            int bytesReadTotal = 0;
            data = new byte[toReadCnt];
            try
            {
                while (bytesReadTotal < toReadCnt)
                {
                    bytesReadJust = clientStream.Read(data, bytesReadTotal, toReadCnt - bytesReadTotal);
                    if (bytesReadJust == 0)
                        return false;
                    bytesReadTotal += bytesReadJust;
                }
                return true;
            }
            catch { return false; };
        }

    }
}
