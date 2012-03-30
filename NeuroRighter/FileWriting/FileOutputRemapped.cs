using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;
using ExtensionMethods;

namespace NeuroRighter.FileWriting
{
    internal class FileOutputRemapped : FileOutput
    {
        internal FileOutputRemapped(string filenameBase, int numChannels, int samplingRate, int fileType, Task recordingTask, string extension, double preampgain) :
            base( filenameBase,  numChannels, samplingRate, fileType,  recordingTask, extension, preampgain) {}

        internal override void read(short[,] data, int numChannelsData, int startChannelData, int length)
        {
            unsafe
            {
                fixed (short* pdata = &data[0, 0], pbuffer = &_buffer[0, 0])
                {
                    for (int c = startChannelData; c < startChannelData + numChannelsData; ++c)
                    {
                        int baseOfDimData = (c - startChannelData) * length; //BaseOfDim is in ref to input data, which has no channel offset
                        int baseOfDimBuffer = (MEAChannelMappings.channel2LinearCR(c) -1)* BUFFER_LENGTH;

                        //Check to see if we'll loop back to front of buffer here, rather than wasting an if statement in the loop
                        if (_currentLocationRead[c] + length < BUFFER_LENGTH)
                        {
                            //Check for buffer overrun
                            if (_currentLocationWrite[c] > _currentLocationRead[c])
                            {
                                if (_currentLocationWrite[c] - _currentLocationRead[c] < length)
                                    System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }

                            //We can copy blithely, without worry of looping back around
                            for (int i = 0; i < length; ++i)
                            {
                                pbuffer[baseOfDimBuffer + _currentLocationRead[c]++] = pdata[baseOfDimData + i];
                                //if (_currentLocationRead[c] != _currentLocationWrite[c]) { /* do nothing */}
                                //else
                                //    System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }
                        }
                        else
                        {
                            //Check for buffer overruns
                            if (_currentLocationWrite[c] > _currentLocationRead[c])
                            {
                                //Since we're guaranteed to go to end of buffer, if write head is higher, we will overrun
                                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }
                            int firstDistance = BUFFER_LENGTH - _currentLocationRead[c];
                            for (int i = 0; i < firstDistance; ++i)
                            {
                                pbuffer[baseOfDimBuffer + _currentLocationRead[c]++] = pdata[baseOfDimData + i];
                            }
                            _currentLocationRead[c] = 0; //Reset read head

                            if (_currentLocationWrite[c] < length - firstDistance)
                            {
                                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            }
                            for (int i = firstDistance; i < length; ++i)
                            {
                                pbuffer[baseOfDimBuffer + _currentLocationRead[c]++] = pdata[baseOfDimData + i];
                            }


                            //for (int i = 0; i < length; ++i)
                            //{
                            //    pbuffer[baseOfDimBuffer + _currentLocationRead[c]] = pdata[baseOfDimData + i];
                            //    if (++_currentLocationRead[c] < BUFFER_LENGTH) { /* do nothing */ }
                            //    else { _currentLocationRead[c] = 0; }
                            //    if (_currentLocationRead[c] != _currentLocationWrite[c]) { /* do nothing */}
                            //    else
                            //        System.Windows.Forms.MessageBox.Show("Buffer Overrun");
                            //}
                        }
                    }
                }
            }

            //for (int c = 0; c < numChannelsData; ++c)
            //{
            //    //Check to see if we'll loop back to front of buffer here, rather than wasting an if statement in the loop
            //    if (_currentLocationRead[startChannelData + c] + length < BUFFER_LENGTH)
            //    {
            //        //We can copy blithely, without worry of looping back around
            //        for (int i = 0; i < length; ++i)
            //        {
            //            _buffer[startChannelData + c][_currentLocationRead[startChannelData + c]++] = data[c, i];
            //            if (_currentLocationRead[c] != _currentLocationWrite[c]) { /* do nothing */}
            //            else
            //                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
            //        }
            //    }
            //    else
            //    {
            //        for (int i = 0; i < length; ++i)
            //        {
            //            _buffer[startChannelData + c][_currentLocationRead[startChannelData + c]] = data[c, i];
            //            if (++_currentLocationRead[startChannelData + c] < BUFFER_LENGTH) { /* do nothing */ }
            //            else { _currentLocationRead[startChannelData + c] = 0; }
            //            if (_currentLocationRead[c] != _currentLocationWrite[c]) { /* do nothing */}
            //            else
            //                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
            //        }
            //    }
            //}
        }

        internal override void read(short data, int channel)
        {
            _buffer[MEAChannelMappings.channel2LinearCR(channel)-1, _currentLocationRead[channel]] = data;
            if (++_currentLocationRead[channel] < BUFFER_LENGTH) { /* do nothing */ }
            else { _currentLocationRead[channel] = 0; }
            if (_currentLocationRead[channel] != _currentLocationWrite[channel]) { /*do nothing*/}
            else
                System.Windows.Forms.MessageBox.Show("Buffer Overrun");
        }

    }
}
