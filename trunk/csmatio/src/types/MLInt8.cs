using System;
using csmatio.common;

namespace csmatio.types
{
	/// <summary>
	/// This class represents an Int8 array (matrix)
	/// </summary>
	/// <author>David Zier (david.zier@gmail.com)</author>
	public class MLInt8 : MLNumericArray<short>
	{
		/// <summary>
		/// Normally this constructor is used only by <c>MatFileReader</c> and <c>MatFileWriter</c>
		/// </summary>
		/// <param name="Name">Array name</param>
		/// <param name="Dims">Array dimensions</param>
		/// <param name="Type">Array type: here <c>mxDOUBLE_CLASS</c></param>
		/// <param name="Attributes">Array flags</param>
		public MLInt8( string Name, int[] Dims, int Type, int Attributes ) :
			base( Name, Dims, Type, Attributes ) {}

		/// <summary>
		/// Create a <c>MLUInt8</c> array with given name and dimensions.
		/// </summary>
		/// <param name="Name">Array name</param>
		/// <param name="Dims">Array dimensions</param>
		public MLInt8( string Name, int[] Dims ) :
            base(Name, Dims, MLArray.mxINT16_CLASS /*Must be UINT8 for Matlab to read correctly */, 0) { }

		/// <summary>
		/// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
		/// construct a 2D real matrix from a one-dimensional packed array.
		/// </summary>
		/// <param name="Name">Array name</param>
		/// <param name="vals">One-dimensional array of doubles, packed by columns</param>
		/// <param name="m">Number of rows</param>
        public MLInt8(string Name, sbyte[] vals, int m)
            :
            base(Name, MLArray.mxINT16_CLASS, SByteToShort(vals), m) { }

		/// <summary>
		/// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
		/// construct a 2D real matrix from <c>byte[][]</c>.
		/// </summary>
		/// <remarks>Note: Array is converted to <c>byte[]</c></remarks>
		/// <param name="Name">Array name</param>
		/// <param name="vals">Two-dimensional array of values</param>
        public MLInt8(string Name, sbyte[][] vals)
            :
			this( Name, Byte2DToByte( vals ), vals.Length ) {}

        /// <summary>
        /// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
        /// construct a 2D imaginary matrix from a one-dimensional packed array.
        /// </summary>
        /// <param name="Name">Array name</param>
        /// <param name="Real">One-dimensional array of <c>byte</c> for <i>real</i> values, packed by columns</param>
        /// <param name="Imag">One-dimensional array of <c>byte</c> for <i>imaginary</i> values, packed by columns</param>
        /// <param name="M">Number of rows</param>
        public MLInt8(string Name, sbyte[] Real, sbyte[] Imag, int M)
            :
            base(Name, MLArray.mxINT16_CLASS, SByteToShort(Real), SByteToShort(Imag), M) { }


        /// <summary>
        /// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
        /// construct a 2D imaginary matrix from a one-dimensional packed array.
        /// </summary>
        /// <param name="Name">Array name</param>
        /// <param name="Real">One-dimensional array of <c>byte</c> for <i>real</i> values, packed by columns</param>
        /// <param name="Imag">One-dimensional array of <c>byte</c> for <i>imaginary</i> values, packed by columns</param>
        public MLInt8(string Name, sbyte[][] Real, sbyte[][] Imag)
            :
            this(Name, Byte2DToByte(Real), Byte2DToByte(Imag), Real.Length) { }

		/// <summary>
		/// Creates a generic byte array.
		/// </summary>
		/// <param name="m">The number of columns in the array</param>
		/// <param name="n">The number of rows in the array</param>
		/// <returns>A generic array.</returns>
        public override short[] CreateArray(int m, int n)
		{
            return new short[m * n];
		}

		/// <summary>
		/// Gets a two-dimensional array.
		/// </summary>
		/// <returns>2D real array.</returns>
        public short[][] GetArray()
		{
            short[][] result = new short[M][];

			for( int m = 0; m < M; m++ )
			{
                result[m] = new short[N];

				for ( int n = 0; n < N; n++ )
				{
                    result[m][n] = (short)GetReal(m, n);
				}
			}
			return result;
		}

		/// <summary>
		/// Converts byte[][] to byte[]
		/// </summary>
		/// <param name="dd"></param>
		/// <returns></returns>
        private static sbyte[] Byte2DToByte(sbyte[][] dd)
		{
            sbyte[] d = new sbyte[dd.Length * dd[0].Length];
			for( int n = 0; n < dd[0].Length; n++ )
			{
				for( int m = 0; m < dd.Length; m++ )
				{
					d[m+n*dd.Length] = dd[m][n];
				}
			}
			return d;
		}

        /// <summary>
        /// Converts an sbyte[] to a short[]
        /// </summary>
        /// <param name="dd"></param>
        /// <returns></returns>
        private static short[] SByteToShort(sbyte[] dd)
        {
            short[] d = new short[dd.Length];
            for (int i = 0; i < dd.Length; i++)
                d[i] = (short)dd[i];
            return d;
        }

		/// <summary>
		/// Gets a byte array from a numeric object.
		/// </summary>
		/// <param name="val">The numeric object to convert into a byte array.</param>
        public override byte[] GetByteArray(object val)
		{
            return BitConverter.GetBytes( (short)val );
		}

		/// <summary>
		/// Gets the number of bytes allocated for a type
		/// </summary>
		unsafe public override int GetBytesAllocated
		{
			get
			{
                return sizeof(short);
			}
		}
		/// <summary>
		/// Builds a numeric object from a byte array.
		/// </summary>
		/// <param name="bytes">A byte array containing the data.</param>
		/// <returns>A numeric object</returns>
        public override object BuildFromBytes(byte[] bytes)
		{
			if( bytes.Length != GetBytesAllocated )
				throw new ArgumentException(
					"To build from a byte array, I need an array of size: " + GetBytesAllocated );
            return BitConverter.ToInt16(bytes, 0);
		}

		/// <summary>
		/// Gets the type of numeric object that this byte storage represents
		/// </summary>
		public override Type GetStorageType
		{
			get
			{
                return typeof(short);
			}
		}

        ///// <summary>
        ///// Override to accelerate the performance
        ///// </summary>
        ///// <param name="buffer"></param>
        ///// <param name="index"></param>
        ///// <returns></returns>
        //protected override sbyte _Get( ByteBuffer buffer, int index )
        //{
        //    return (sbyte)buffer.Get(index);
        //}
	}
}
