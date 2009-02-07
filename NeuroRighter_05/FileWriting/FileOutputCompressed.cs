// NeuroRighter v0.04
// Copyright (c) 2008 John Rolston
//
// This file is part of NeuroRighter v0.04.
//
// NeuroRighter v0.04 is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter v0.04 is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter v0.04.  If not, see <http://www.gnu.org/licenses/>. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using NationalInstruments;
using NationalInstruments.DAQmx;

namespace NeuroRighter
{
    internal sealed class FileOutputCompressed : FileOutput
    {
        internal FileOutputCompressed(string filenameBase, int numChannels, int samplingRate, int fileType, Task recordingTask, string extension) :
            base(filenameBase, numChannels, samplingRate, fileType, recordingTask, extension)
        {

        }

        protected override Stream createStream(string filename, int bufferSize)
        {
            FileStream fStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, false);
            return new GZipStream(fStream, CompressionMode.Compress, false);
        }
    }
}
