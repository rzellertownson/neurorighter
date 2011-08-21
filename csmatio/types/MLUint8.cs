using System;
using csmatio.common;

namespace csmatio.types
{
	/// <summary>
	/// This class represents an UInt8 (byte) array (matrix)
	/// </summary>
	/// <author>David Zier (david.zier@gmail.com)</author>
	public class MLUInt8 : MLNumericArray<byte>
	{
		/// <summary>
		/// Normally this constructor is used only by <c>MatFileReader</c> and <c>MatFileWriter</c>
		/// </summary>
		/// <param name="Name">Array name</param>
		/// <param name="Dims">Array dimensions</param>
		/// <param name="Type">Array type: here <c>mxUINT8_CLASS</c></param>
		/// <param name="Attributes">Array flags</param>
		public MLUInt8( string Name, int[] Dims, int Type, int Attributes ) :
			base( Name, Dims, Type, Attributes ) {}

		/// <summary>
		/// Create a <c>MLUInt8</c> array with given name and dimensions.
		/// </summary>
		/// <param name="Name">Array name</param>
		/// <param name="Dims">Array dimensions</param>
		public MLUInt8( string Name, int[] Dims ) :
			base( Name, Dims, MLArray.mxUINT8_CLASS, 0 ) {}

		/// <summary>
		/// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
		/// construct a 2D real matrix from a one-dimensional packed array.
		/// </summary>
		/// <param name="Name">Array name</param>
		/// <param name="vals">One-dimensional array of doubles, packed by columns</param>
		/// <param name="m">Number of rows</param>
		public MLUInt8( string Name, byte[] vals, int m ) :
			base( Name, MLArray.mxUINT8_CLASS, vals, m ) {}

		/// <summary>
		/// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
		/// construct a 2D real matrix from <c>byte[][]</c>.
		/// </summary>
		/// <remarks>Note: Array is converted to <c>byte[]</c></remarks>
		/// <param name="Name">Array name</param>
		/// <param name="vals">Two-dimensional array of values</param>
		public MLUInt8( string Name, byte[][] vals ) :
			base( Name, MLArray.mxUINT8_CLASS, Byte2DToByte( vals ), vals.Length ) {}

        /// <summary>
        /// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
        /// construct a 2D imaginary matrix from a one-dimensional packed array.
        /// </summary>
        /// <param name="Name">Array name</param>
        /// <param name="Real">One-dimensional array of <c>byte</c> for <i>real</i> values, packed by columns</param>
        /// <param name="Imag">One-dimensional array of <c>byte</c> for <i>imaginary</i> values, packed by columns</param>
        /// <param name="M">Number of rows</param>
        public MLUInt8(string Name, byte[] Real, byte[] Imag, int M)
            :
            base(Name, MLArray.mxINT64_CLASS, Real, Imag, M) { }


        /// <summary>
        /// <a href="http://math.nist.gov/javanumerics/jama/">Jama</a> [math.nist.gov] style:
        /// construct a 2D imaginary matrix from a one-dimensional packed array.
        /// </summary>
        /// <param name="Name">Array name</param>
        /// <param name="Real">One-dimensional array of <c>byte</c> for <i>real</i> values, packed by columns</param>
        /// <param name="Imag">One-dimensional array of <c>byte</c> for <i>imaginary</i> values, packed by columns</param>
        public MLUInt8(string Name, byte[][] Real, byte[][] Imag)
            :
            this(Name, Byte2DToByte(Real), Byte2DToByte(Imag), Real.Length) { }

		/// <summary>
		/// Creates a generic byte array.
		/// </summary>
		/// <param name="m">The number of columns in the array</param>
		/// <param name="n">The number of rows in the array</param>
		/// <returns>A generic array.</returns>
		public override byte[] CreateArray( int m, int n )
		{
			return new byte[m*n];
		}

		/// <summary>
		/// Gets a two-dimensional array.
		/// </summary>
		/// <returns>2D real array.</returns>
		public byte[][] GetArray()
		{
			byte[][] result = new byte[M][];

			for( int m = 0; m < M; m++ )
			{
				result[m] = new byte[ N ];

				for ( int n = 0; n < N; n++ )
				{
					result[m][n] = (byte)GetReal(m,n);
				}
			}
			return result;
		}

        /// <summary>
        /// Converts byte[][] to byte[]
        /// </summary>
        /// <param name="dd"></param>
        /// <returns></returns>
        private static byte[] Byte2DToByte(byte[][] dd)
		{
			byte[] d = new byte[ dd.Length*dd[0].Length ];
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
		/// Gets a byte array from a numeric object.
		/// </summary>
		/// <param name="val">The numeric object to convert into a byte array.</param>
		public override byte[] GetByteArray(object val)
		{
			return new byte[] { (byte)val };
		}

		/// <summary>
		/// Gets the number of bytes allocated for a type
		/// </summary>
		unsafe public override int GetBytesAllocated
		{
			get
			{
				return sizeof(byte);
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
			return bytes[0];
		}

		/// <summary>
		/// Gets the type of numeric object that this byte storage represents
		/// </summary>
		public override Type GetStorageType
		{
			get
			{
				return typeof( byte );
			}
		}

		/// <summary>
		/// Override to accelerate the performance
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		protected override byte _Get( ByteBuffer buffer, int index )
		{
			return buffer.Get(index);
		}
	}
}

