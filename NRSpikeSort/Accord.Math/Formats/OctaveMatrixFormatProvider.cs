// Accord Math Library
// The Accord.NET Framework
// http://accord-net.origo.ethz.ch
//
// Copyright © César Souza, 2009-2011
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Math.Formats
{
    using System;
    using System.Globalization;

    /// <summary>
    ///   Format provider for the matrix format used by Octave.
    /// </summary>
    /// 
    public sealed class OctaveMatrixFormatProvider : MatrixFormatProviderBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="OctaveMatrixFormatProvider"/> class.
        /// </summary>
        public OctaveMatrixFormatProvider(CultureInfo culture)
            : base(culture)
        {
            FormatMatrixStart = "[";
            FormatMatrixEnd = "]";
            FormatRowStart = String.Empty;
            FormatRowEnd = String.Empty;
            FormatColStart = String.Empty;
            FormatColEnd = String.Empty;
            FormatRowDelimiter = "; ";
            FormatColDelimiter = " ";

            ParseMatrixStart = "[";
            ParseMatrixEnd = "]";
            ParseRowStart = String.Empty;
            ParseRowEnd = String.Empty;
            ParseColStart = String.Empty;
            ParseColEnd = String.Empty;
            ParseRowDelimiter = "; ";
            ParseColDelimiter = " ";
        }

        /// <summary>
        ///   Gets the IMatrixFormatProvider which uses the CultureInfo used by the current thread.
        /// </summary>
        /// 
        public static OctaveMatrixFormatProvider CurrentCulture
        {
            get { return currentCulture; }
        }

        /// <summary>
        ///   Gets the IMatrixFormatProvider which uses the invariant system culture.
        /// </summary>
        /// 
        public static OctaveMatrixFormatProvider InvariantCulture
        {
            get { return invariantCulture; }
        }


        private static readonly OctaveMatrixFormatProvider invariantCulture =
            new OctaveMatrixFormatProvider(CultureInfo.InvariantCulture);

        private static readonly OctaveMatrixFormatProvider currentCulture =
            new OctaveMatrixFormatProvider(CultureInfo.CurrentCulture);

    }
}
