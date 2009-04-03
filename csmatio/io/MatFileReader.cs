using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

using zlib;

using csmatio.common;
using csmatio.types;

//using ICSharpCode.SharpZipLib.GZip;

namespace csmatio.io
{
	/// <summary>
	/// MAT-file reader.  Reads MAT-file into <c>MLArray</c> objects.
	/// </summary>
	/// <example>
	/// <code>
	///		// read in the file
	///		MatFileReader mfr = new MatFileReader( "mat_file.mat" );
	///		
	///		// Get array of a name "my_array" from file
	///		MLArray mlArrayRetrived = mfr.GetMLArray( "my_array" );
	///		
	///		// or get the collection of all arrays that were stored in the file
	///		Hashtable content = mfr.Content;
	/// </code>
	/// </example>
	/// <author>David Zier (<a href="mailto:david.zier@gmail.com">david.zier@gmail.com</a>)</author>
	public class MatFileReader
	{
		/// <summary>
		/// MAT-file header
		/// </summary>
		private MatFileHeader _matFileHeader;
		/// <summary>
		/// Contianer for read <c>MLArray</c>s
		/// </summary>
		private Dictionary<string,MLArray> _data;
//		/// <summary>
//		/// Tells how bytes are organized in the buffer
//		/// </summary>
//		private ByteOrder _byteData;
		/// <summary>
		/// Array name filter
		/// </summary>
		private MatFileFilter _filter;


		/// <summary>
		/// Creates instance of <c>MatFileReader</c> and reads MAT-file with then name
		/// <c>fileName</c>.
		/// </summary>
		/// <remarks>
		/// This method reads MAT-file without filtering.
		/// </remarks>
		/// <param name="fileName">The name of the MAT-file to open</param>
		public MatFileReader( string fileName ) :
			this( fileName, new MatFileFilter() )
		{
		}


		/// <summary>
		/// Creates instance of <c>MatFileReader</c> and reads MAT-file with then name
		/// <c>fileName</c>.
		/// </summary>
		/// <remarks>
		/// Results are filtered by <c>MatFileFilter</c>.  Arrays that do not meet
		/// filter match condition will not be available in results.
		/// </remarks>
		/// <param name="fileName">The name of the MAT-file to open</param>
		/// <param name="filter"><c>MatFileFilter</c></param>
		public MatFileReader( string fileName, MatFileFilter filter )
		{
			_filter = filter;
			_data = new Dictionary<string,MLArray>();
			
			// Try to open up the file as read-only
			FileStream dataIn;
			try 
			{
				dataIn = new FileStream( fileName, FileMode.Open, FileAccess.Read );
			}
			catch( FileNotFoundException )
			{
				throw new MatlabIOException("Could not open the file '" + fileName + "' for reading!" );
			}

			// try and read in the file until completed
			try
			{
				ReadHeader( dataIn );

				for(;;)
				{
					ReadData( dataIn );
				}
			}
			catch( EndOfStreamException )
			{
				// Catch to break out of the for loop
			}
			catch( IOException e )
			{
				throw new MatlabIOException( "Error in reading MAT-file '" + fileName + "':\n" + e.ToString() );
			}

			dataIn.Close();
		}
		
		/// <summary>
		/// Gets MAT-file header.
		/// </summary>
		public MatFileHeader MatFileHeader
		{
			get{ return _matFileHeader; }
		}

		/// <summary>
		/// Returns list of <c>MLArray</c> objects that were inside the MAT-file
		/// </summary>
		public List<MLArray> Data
		{
			get{ return new List<MLArray>( _data.Values ); }
		}

		/// <summary>
		/// Returns the value to which the read file maps the specific array name.
		/// </summary>
		/// <remarks>
		/// Returns <c>null</c> if the file contains no content for this name.
		/// </remarks>
		/// <param name="name">Array name</param>
		/// <returns>The <c>MLArray</c> to which this file maps the specific name,
		/// or null if the file contains no content for this name</returns>
		public MLArray GetMLArray( string name )
		{
			return _data.ContainsKey( name ) ? (MLArray)_data[ name ] : null;
		}

		/// <summary>
		/// Returns a map of <c>MLArray</c> objects that were inside MAT-file.
		/// </summary>
		/// <remarks>MLArrays are keyed with the MLArrays' name.</remarks>
		public Dictionary<string,MLArray> Content
		{
			get{ return _data; }
		}

		/// <summary>
		/// Decompresses (inflates) bytes from input stream.
		/// </summary>
		/// <remarks>
		/// Stream marker is being set at +<code>numOfBytes</code> position of the stream.
		/// </remarks>
		/// <param name="buf">Input byte buffer stream.</param>
		/// <param name="numOfBytes">The number of bytes to be read.</param>
		/// <returns>new <c>ByteBuffer</c> with the inflated block of data.</returns>
		/// <exception cref="IOException">When an error occurs while reading or inflating the buffer.</exception>
		private Stream Inflate( Stream buf, int numOfBytes )
		{
            if (buf.Length - buf.Position < numOfBytes)
            {
                throw new MatlabIOException("Compressed buffer length miscalculated!");
            }

            MemoryStream inflatedStream = new MemoryStream();
            zlib.ZOutputStream zis = new ZOutputStream(inflatedStream);
            //System.IO.Compression.GZipStream gzs = new GZipStream(deflatedStream, CompressionMode.Decompress, true);

            byte[] data = new byte[128];
            try
            {
                int bytesTotal = 0;
                int bytesRead = 0;
                while (bytesTotal < numOfBytes )
                {
                    int len = Math.Min(data.Length, numOfBytes - bytesTotal);
                    bytesRead = buf.Read(data, 0, len);
                    if (bytesRead == 0)
                        break;
                    zis.Write(data, 0, bytesRead);
                    bytesTotal += bytesRead;
                }
            }
            catch (IOException e)
            {
                throw new MatlabIOException("Could not decompress data: " + e);
            }

            inflatedStream.Position = 0;

            return inflatedStream;
		}

		/// <summary>
		/// Reads data from the <c>BinaryReader</c> stream. Searches for either
		/// <c>miCOMPRESSED</c> data or <c>miMATRIX</c> data.
		/// </summary>
		/// <remarks>
		/// Compressed data is inflated and the product is recursively passed back
		/// to this same method.
		/// </remarks>
		/// <param name="buf">The input <c>BinaryReader</c> stream.</param>
		private void ReadData( Stream buf )
		{
            // read data
            ISMatTag tag = new ISMatTag(buf);
            switch (tag.Type)
            {
                case MatDataTypes.miCOMPRESSED:
                    // inflate and recur
                    ReadData(Inflate(buf, tag.Size));
                    break;
                case MatDataTypes.miMATRIX:
                    // read in the matrix
                    int pos = (int)buf.Position;
                    int red, toread;

                    MLArray element = ReadMatrix(buf, true);

                    if (element != null)
                    {
                        _data.Add(element.Name, element);
                    }
                    else
                    {
                        red = (int)buf.Position - pos;
                        toread = tag.Size - red;
                        buf.Position = buf.Position + toread;
                    }
                    red = (int)buf.Position - pos;

                    toread = tag.Size - red;

                    if (toread != 0)
                    {
                        throw new MatlabIOException("Matrix was not read fully! " + toread + " remaining in the buffer.");
                    }
                    break;
                default:
                    throw new MatlabIOException("Incorrect data tag: " + tag);

            }
		}

		/// <summary>
		/// Reads <c>miMATRIX</c> from the input stream.
		/// </summary>
		/// <remarks>
		/// If reading was not finished (which is normal for filtered results)
		/// returns <c>null</c>.
		/// </remarks>
		/// <param name="buf">The <c>BinaryReader</c> input stream.</param>
		/// <param name="isRoot">When <c>true</c> informs that if this is a top level
		/// matrix.</param>
		/// <returns><c>MLArray</c> or <c>null</c> if matrix does not match <c>filter</c></returns>
		private MLArray ReadMatrix( Stream buf, bool isRoot )
		{
			// result
			MLArray mlArray = null;
			ISMatTag tag;

			// read flags
			int[] flags = ReadFlags( buf );
			int attributes = (flags.Length != 0 ) ? flags[0] : 0;
			int nzmax = ( flags.Length != 0 ) ? flags[1] : 0;
			int type = attributes & 0xff;

			// read Array dimension
			int[] dims = ReadDimension( buf );

			// read Array name
			string name = ReadName( buf );

			// If this array is filtered out return immediately
			if( isRoot && !_filter.Matches(name) )
			{
				return null;
			}

			// read data
            switch (type)
            {
                case MLArray.mxSTRUCT_CLASS:
                    MLStructure mlStruct = new MLStructure(name, dims, type, attributes);

                    BinaryReader br = new BinaryReader(buf);

                    // field name length - this subelement always uses the compressed data element format
                    tag = new ISMatTag(br.BaseStream);
                    int maxlen = br.ReadInt32();

                    // Read fields data as Int8
                    tag = new ISMatTag(br.BaseStream);
                    // calculate number of fields
                    int numOfFields = tag.Size / maxlen;

                    // padding after field names
                    int padding = (tag.Size % 8) != 0 ? 8 - tag.Size % 8 : 0;

                    string[] fieldNames = new string[numOfFields];
                    for (int i = 0; i < numOfFields; i++)
                    {
                        byte[] names = new byte[maxlen];
                        br.Read(names, 0, names.Length);
                        fieldNames[i] = ZeroEndByteArrayToString(names);
                    }
                    br.ReadBytes(padding);

                    // read fields
                    for (int index = 0; index < mlStruct.M * mlStruct.N; index++)
                    {
                        for (int i = 0; i < numOfFields; i++)
                        {
                            // read matrix recursively
                            tag = new ISMatTag(br.BaseStream);
                            if (tag.Size > 0)
                            {
                                MLArray fieldValue = ReadMatrix(br.BaseStream, false);
                                mlStruct[fieldNames[i], index] = fieldValue;
                            }
                            else
                            {
                                mlStruct[fieldNames[i], index] = new MLEmptyArray();
                            }
                        }
                    }
                    mlArray = mlStruct;
                    //br.Close();
                    break;
                case MLArray.mxCELL_CLASS:
                    MLCell cell = new MLCell(name, dims, type, attributes);
                    for (int i = 0; i < cell.M * cell.N; i++)
                    {
                        tag = new ISMatTag(buf);
                        if (tag.Size > 0)
                        {
                            MLArray cellmatrix = ReadMatrix(buf, false);
                            cell[i] = cellmatrix;
                        }
                        else
                        {
                            cell[i] = new MLEmptyArray();
                        }
                    }
                    mlArray = cell;
                    break;
                case MLArray.mxDOUBLE_CLASS:
                    mlArray = new MLDouble(name, dims, type, attributes);
                    //read real
                    tag = new ISMatTag(buf);
                    tag.ReadToByteBuffer(((MLNumericArray<double>)mlArray).RealByteBuffer,
                        (ByteStorageSupport)mlArray);

                    // read complex
                    if (mlArray.IsComplex)
                    {
                        tag = new ISMatTag(buf);
                        tag.ReadToByteBuffer(((MLNumericArray<double>)mlArray).ImaginaryByteBuffer,
                            (ByteStorageSupport)mlArray);
                    }
                    break;
                case MLArray.mxUINT8_CLASS:
                    mlArray = new MLUInt8(name, dims, type, attributes);
                    //read real
                    tag = new ISMatTag(buf);
                    tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).RealByteBuffer,
                        (ByteStorageSupport)mlArray);

                    // read complex
                    if (mlArray.IsComplex)
                    {
                        tag = new ISMatTag(buf);
                        tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).ImaginaryByteBuffer,
                            (ByteStorageSupport)mlArray);
                    }
                    break;
                case MLArray.mxINT8_CLASS:
                    mlArray = new MLInt8(name, dims, type, attributes);
                    //read real
                    tag = new ISMatTag(buf);
                    tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).RealByteBuffer,
                        (ByteStorageSupport)mlArray);

                    // read complex
                    if (mlArray.IsComplex)
                    {
                        tag = new ISMatTag(buf);
                        tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).ImaginaryByteBuffer,
                            (ByteStorageSupport)mlArray);
                    }
                    break;
                case MLArray.mxUINT64_CLASS:
                    mlArray = new MLUInt64(name, dims, type, attributes);
                    //read real
                    tag = new ISMatTag(buf);
                    tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).RealByteBuffer,
                        (ByteStorageSupport)mlArray);

                    // read complex
                    if (mlArray.IsComplex)
                    {
                        tag = new ISMatTag(buf);
                        tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).ImaginaryByteBuffer,
                            (ByteStorageSupport)mlArray);
                    }
                    break;
                case MLArray.mxINT64_CLASS:
                    mlArray = new MLInt64(name, dims, type, attributes);
                    //read real
                    tag = new ISMatTag(buf);
                    tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).RealByteBuffer,
                        (ByteStorageSupport)mlArray);

                    // read complex
                    if (mlArray.IsComplex)
                    {
                        tag = new ISMatTag(buf);
                        tag.ReadToByteBuffer(((MLNumericArray<byte>)mlArray).ImaginaryByteBuffer,
                            (ByteStorageSupport)mlArray);
                    }
                    break;
                case MLArray.mxCHAR_CLASS:
                    MLChar mlchar = new MLChar(name, dims, type, attributes);
                    //read real
                    tag = new ISMatTag(buf);
                    char[] ac = tag.ReadToCharArray();
                    for (int i = 0; i < ac.Length; i++)
                    {
                        mlchar.SetChar(ac[i], i);
                    }
                    mlArray = mlchar;
                    break;
                case MLArray.mxSPARSE_CLASS:
                    MLSparse sparse = new MLSparse(name, dims, attributes, nzmax);

                    // read ir (row indices)
                    tag = new ISMatTag(buf);
                    int[] ir = tag.ReadToIntArray();
                    // read jc (column indices)
                    tag = new ISMatTag(buf);
                    int[] jc = tag.ReadToIntArray();

                    // read pr (real part)
                    tag = new ISMatTag(buf);
                    double[] ad1 = tag.ReadToDoubleArray();
                    int n = 0;
                    for (int i = 0; i < ir.Length; i++)
                    {
                        if (i < sparse.N)
                        {
                            n = jc[i];
                        }
                        sparse.SetReal(ad1[i], ir[i], n);
                    }

                    //read pi (imaginary part)
                    if (sparse.IsComplex)
                    {
                        tag = new ISMatTag(buf);
                        double[] ad2 = tag.ReadToDoubleArray();

                        int n1 = 0;
                        for (int i = 0; i < ir.Length; i++)
                        {
                            if (i < sparse.N)
                            {
                                n1 = jc[i];
                            }
                            sparse.SetImaginary(ad2[i], ir[i], n1);
                        }
                    }
                    mlArray = sparse;
                    break;
                default:
                    throw new MatlabIOException("Incorrect Matlab array class: " + MLArray.TypeToString(type));
            }
            return mlArray;
		}

        /// <summary>
        /// Converts a byte array to <c>string</c>.  It assumes that the string ends with \0 value.
        /// </summary>
        /// <param name="bytes">Byte array containing the string.</param>
        /// <returns>String retrieved from byte array</returns>
        private string ZeroEndByteArrayToString(byte[] bytes)
		{
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++ )
                sb.Append((char)bytes[i]);
            return sb.ToString();
		}

        /// <summary>
        /// Reads Matrix flags.
        /// </summary>
        /// <param name="buf"><c>BinaryReader</c> input stream</param>
        /// <returns>Flags int array</returns>
        private int[] ReadFlags(Stream buf)
		{
			ISMatTag tag = new ISMatTag( buf );

			int[] flags = tag.ReadToIntArray();

			return flags;
		}

		/// <summary>
		/// Reads Matrix dimensions.
		/// </summary>
		/// <param name="buf"><c>BinaryReader</c> input stream</param>
		/// <returns>Dimensions int array</returns>
		private int[] ReadDimension( Stream buf )
		{
			ISMatTag tag = new ISMatTag( buf );
			int[] dims = tag.ReadToIntArray();
			return dims;
		}

		/// <summary>
		/// Reads Matrix name.
		/// </summary>
		/// <param name="buf"><c>BinaryReader</c> input stream</param>
		/// <returns><c>string</c></returns>
		private string ReadName( Stream buf )
		{
			string s;

			ISMatTag tag = new ISMatTag( buf );
			char[] ac = tag.ReadToCharArray();
			s = new string(ac);

			return s;
		}

		/// <summary>
		/// Reads MAT-file header.
		/// </summary>
		/// <param name="buf"><c>BinaryReader</c> input stream</param>
		private void ReadHeader( Stream buf )
		{
			string description;
			int version;
            BinaryReader br = new BinaryReader(buf);
			byte[] endianIndicator = new byte[2];

			//descriptive text 116 bytes
			byte[] descriptionBuffer = new byte[116];
			br.Read( descriptionBuffer, 0, descriptionBuffer.Length );

			description = ZeroEndByteArrayToString( descriptionBuffer );

			if( !description.StartsWith("MATLAB 5.0 MAT-file") )
			{
				throw new MatlabIOException("This is not a valid MATLAB 5.0 MAT-file.");
			}

			// subsyst data offset 8 bytes
			br.ReadBytes( 8 );

			byte[] bversion= new byte[2];
			//version 2 bytes
			br.Read( bversion, 0, bversion.Length );

			//endian indicator 2 bytes
			br.Read( endianIndicator, 0, endianIndicator.Length );

			// Program reading the MAT-file must perform byte swapping to interpret the data
			// in the MAT-file correctly
			if( (char)endianIndicator[0] == 'I' && (char)endianIndicator[1] == 'M' )
			{
				// We have a Little Endian MAT-file
				version = bversion[1] & 0xff | bversion[0] << 8;
			}
			else
			{
				// right now, this version of CSMatIO does not support Big Endian
				throw new MatlabIOException("This version of CSMatIO does not support Big Endian.");
			}

			_matFileHeader = new MatFileHeader( description, version, endianIndicator );
		}

		/// <summary>
		/// TAG operator.  Facilitates reading operations.
		/// </summary>
		/// <remarks>
		/// <i>Note: Reading from buffer and/or stream modifies it's position.</i>
		/// </remarks>
		/// <author>David Zier (<a href="mailto:david.zier@gmail.com">david.zier@gmail.com</a>)</author>
		private class ISMatTag : MatTag
		{
			/// <summary>
			/// A <c>ByteBuffer</c> object for this tag
			/// </summary>
			public BinaryReader Buf;
			private int padding;

			/// <summary>
			/// Create an ISMatTag from a <c>ByteBuffer</c>.
			/// </summary>
			/// <param name="buf"><c>ByteBuffer</c></param>
			public ISMatTag( Stream buf ) :
				base( 0, 0 )
			{
				Buf = new BinaryReader(buf);
				int tmp = Buf.ReadInt32();

				bool compressed;
				// data not packed in the tag
				if( tmp >> 16 == 0 )
				{
					_type = tmp;
					_size = Buf.ReadInt32();
					compressed = false;
				}
				else // data _packed_ in the tag (compressed)
				{
					_size = tmp >> 16;  // 2 more significant bytes
					_type = tmp & 0xffff; // 2 less significant bytes
					compressed = true;
				}
				
                
                padding = GetPadding( _size, compressed );
			}

            ///// <summary>
            ///// Gets the size of the <c>ISMatTag</c>
            ///// </summary>
            //public int Size
            //{
            //    get { return (int)Buf.BaseStream.Length; }
            //}

			/// <summary>
			/// Read MAT-file tag to a byte buffer.
			/// </summary>
			/// <param name="buff"><c>ByteBuffer</c></param>
			/// <param name="storage"><c>ByteStorageSupport</c></param>
			public void ReadToByteBuffer( ByteBuffer buff, ByteStorageSupport storage )
			{
				MatFileInputStream mfis = new MatFileInputStream( Buf, _type );
				int elements = _size / SizeOf();
				mfis.ReadToByteBuffer( buff, elements, storage );
				//skip padding
				if( padding > 0 )
					Buf.ReadBytes( padding );
			}

			/// <summary>
			/// Read MAT-file tag to a <c>byte</c> array
			/// </summary>
			/// <returns><c>byte[]</c></returns>
			public byte[] ReadToByteArray()
			{
				// allocate memory for array elements
				int elements = _size / SizeOf();
				byte[] ab = new byte[elements];

				MatFileInputStream mfis = new MatFileInputStream( Buf, _type );

				for( int i = 0; i < elements; i++ )
					ab[i] = mfis.ReadByte();

				// skip padding
				if( padding > 0 )
					Buf.ReadBytes( padding );
				return ab;
			}

			/// <summary>
			/// Read MAT-file tag to a <c>double</c> array
			/// </summary>
			/// <returns><c>double[]</c></returns>
			public double[] ReadToDoubleArray()
			{
				// allocate memory for array elements
				int elements = _size / SizeOf();
				double[] ad = new double[elements];

				MatFileInputStream mfis = new MatFileInputStream( Buf, _type );

				for( int i = 0; i < elements; i++ )
					ad[i] = mfis.ReadDouble();

				// skip padding
				if( padding > 0 )
					Buf.ReadBytes( padding );
				return ad;
			}

			/// <summary>
			/// Read MAT-file tag to a <c>int</c> array
			/// </summary>
			/// <returns><c>int[]</c></returns>
			public int[] ReadToIntArray()
			{
				// allocate memory for array elements
				int elements = _size / SizeOf();
				int[] ai = new int[elements];

				MatFileInputStream mfis = new MatFileInputStream( Buf, _type );

				for( int i = 0; i < elements; i++ )
					ai[i] = mfis.ReadInt();

				// skip padding
				if( padding > 0 )
					Buf.ReadBytes( padding );
				return ai;
			}

			/// <summary>
			/// Read MAT-file tag to a <c>char</c> array
			/// </summary>
			/// <returns><c>int[]</c></returns>
			public char[] ReadToCharArray()
			{
				// allocate memory for array elements
				int elements = _size / SizeOf();
				char[] ac = new char[elements];

				MatFileInputStream mfis = new MatFileInputStream( Buf, _type );

				for( int i = 0; i < elements; i++ )
					ac[i] = mfis.ReadChar();

				// skip padding
				if( padding > 0 )
					Buf.ReadBytes( padding );
				return ac;
			}
		}
	}
}
