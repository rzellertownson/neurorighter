/* 
 * DataBuffer
 * Implements circular buffer for storing several last seconds of incoming data.
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
using System.Threading;

namespace NeuroRighter.Bufferization
{
    public enum DataType { raw, lfp };
    public delegate DataBuffer getBuffer(DataType bufferType);

    public class DataBuffer
    {
        public double[,] data;
        public List<List<int[]>> freshData;
        public ReaderWriterLockSlim rwLock;

        public int numChannels;
        public int samplingRate;

        int bufferSize;
        int freeSpaceIndex;
        int freeSpaceIndex_byte;

        public DataBuffer(int numChannels, int samplingRate, int bufferLengthSec)
        {
            this.numChannels = numChannels;
            this.samplingRate = samplingRate;
            bufferSize = samplingRate * bufferLengthSec;
            data = new double[bufferSize * 2, numChannels];
            freeSpaceIndex = 0;
            freeSpaceIndex_byte = 0;
            freshData = new List<List<int[]>>();
            rwLock = new ReaderWriterLockSlim();
        }

        public List<int[]> connectClient()
        {
            freshData.Add(new List<int[]>());
            return freshData.Last();
        }

        public void detachClient(List<int[]> clientList)
        {
            freshData.Remove(clientList);
        }

        public void yumData(double[,] newData)
        {
            if (freeSpaceIndex > bufferSize)
            {
                freeSpaceIndex = 0;
                freeSpaceIndex_byte = 0;
            }
            int newDataLength = newData.GetLength(0);
            int newDataLength_byte = numChannels * newDataLength * sizeof(double);

//            int bufferSize_byte = bufferSize * 2 * sizeof(double);

            rwLock.EnterWriteLock();
            Buffer.BlockCopy(newData, 0, data, freeSpaceIndex_byte, newDataLength_byte);
//            for (int c = 0; c < numChannels; c++)
//                Buffer.BlockCopy(newData, c * newDataLength_byte, data, c * bufferSize_byte + freeSpaceIndex_byte, newDataLength_byte);       
//            for (int c = 0; c < numChannels; c++)
//                for (int d = 0; d < newDataLength; d++)
//                    data[c, freeSpaceIndex + d] = newData[c, d];
            foreach (List<int[]> freshDataList in freshData)
            {
                if (freshDataList.Count > 0 && freshDataList.Last()[0] + freshDataList.Last()[1] == freeSpaceIndex)
                    freshDataList.Last()[1] = freshDataList.Last()[1] + newDataLength;
                else
                    freshDataList.Add(new int[] { freeSpaceIndex, newDataLength });
            }
            rwLock.ExitWriteLock();
            freeSpaceIndex = freeSpaceIndex + newDataLength;
            freeSpaceIndex_byte = freeSpaceIndex_byte + newDataLength_byte;
        }

    }
}
