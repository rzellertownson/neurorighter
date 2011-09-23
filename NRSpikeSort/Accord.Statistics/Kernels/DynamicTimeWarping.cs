// Accord Statistics Library
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

namespace Accord.Statistics.Kernels
{
    using Accord.Math;
    using System;

    /// <summary>
    ///   Dynamic Time Warping Sequence Kernel.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The Dynamic Time Warping Sequence Kernel is a sequence kernel, accepting
    ///   vector sequences of variable size as input. Despite the sequences being
    ///   variable in size, the vectors contained in such sequences should have its
    ///   size fixed and should be informed at the construction of this kernel.</para>
    /// <para>
    ///   The conversion of the DTW global distance to a dot product uses a combination
    ///   of a technique known as spherical normalization and the polynomial kernel. The
    ///   degree of the polynomial kernel and the alpha for the spherical normalization
    ///   should be given at the construction of the kernel. For more information,
    ///   please see the referenced papers shown below.</para>
    ///   
    /// <para>
    ///   <list type="bullet">
    ///   References:
    ///     <item><description>
    ///     V. Wan, J. Carmichael; Polynomial Dynamic Time Warping Kernel Support
    ///     Vector Machines for Dysarthric Speech Recognition with Sparse Training
    ///     Data. Interspeech'2005 - Eurospeech - 9th European Conference on Speech
    ///     Communication and Technology. Lisboa, 2005.</description></item>
    ///   </list></para>
    /// 
    /// </remarks>
    /// 
    [Serializable]
    public sealed class DynamicTimeWarping : IKernel
    {
        private double alpha = 1.0; // spherical projection distance
        private int length = 1;     // length of the feature vectors
        private int degree = 1;     // polynomial kernel degree

        /// <summary>
        ///   Gets or sets the length for the feature vectors
        ///   contained in each sequence used by the kernel.
        /// </summary>
        /// 
        public int Length
        {
            get { return length; }
            set { length = value; }
        }

        /// <summary>
        ///   Gets or sets the hypersphere ratio.
        /// </summary>
        /// 
        public double Alpha
        {
            get { return alpha; }
            set { alpha = value; }
        }

        /// <summary>
        ///   Gets or sets the polynomial degree for this kernel.
        /// </summary>
        /// 
        public int Degree
        {
            get { return degree; }
            set { degree = value; }
        }

        /// <summary>
        ///   Constructs a new Dynamic Time Warping kernel.
        /// </summary>
        /// <param name="length">
        ///    The length of the feature vectors
        ///    contained in each sequence.
        /// </param>
        /// 
        public DynamicTimeWarping(int length)
        {
            this.length = length;
        }

        /// <summary>
        ///   Constructs a new Dynamic Time Warping kernel.
        /// </summary>
        /// <param name="length">
        ///    The length of the feature vectors
        ///    contained in each sequence.
        /// </param>
        /// <param name="alpha">
        ///    The hypersphere ratio. Default value is 1.
        /// </param>
        /// 
        public DynamicTimeWarping(int length, double alpha)
        {
            this.length = length;
            this.alpha = alpha;
        }

        /// <summary>
        ///   Constructs a new Dynamic Time Warping kernel.
        /// </summary>
        /// <param name="length">
        ///    The length of the feature vectors
        ///    contained in each sequence.
        /// </param>
        /// <param name="alpha">
        ///    The hypersphere ratio. Default value is 1.
        /// </param>
        /// <param name="degree">
        ///    The degree of the kernel. Default value is 1 (linear kernel).
        /// </param>
        /// 
        public DynamicTimeWarping(int length, double alpha, int degree)
        {
            this.alpha = alpha;
            this.degree = degree;
            this.length = length;
        }


        /// <summary>
        ///   Dynamic Time Warping kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        /// 
        public double Function(double[] x, double[] y)
        {
            if (x == y) return 1.0;

            // TODO: As a performance improvement, projected values
            //  could be cached to avoid unecessary computations.

            // Compute the cosine of the global distance
            double distance = D(snorm(x), snorm(y));
            double cos = System.Math.Cos(distance);

            // Return cos for the linear kernel, cos^n for polynomial
            return (degree == 1) ? cos : System.Math.Pow(cos, degree);
        }


        /// <summary>
        ///   Global distance D(X,Y) between two sequences of vectors.
        /// </summary>
        /// <param name="X">A sequence of vectors.</param>
        /// <param name="Y">A sequence of vectors.</param>
        /// <returns>The global distance between X and Y.</returns>
        /// 
        private double D(double[] X, double[] Y)
        {
            // Get the number of vectors in each sequence. The vectors
            // have been projected, so the length is augmented by one.
            int n = X.Length / (length + 1);
            int m = Y.Length / (length + 1);

            // Application of the Dynamic Time Warping
            // algorithm by using dynamic programming.
            double[,] DTW = new double[n + 1, m + 1];

            for (int i = 1; i <= n; i++)
                DTW[i, 0] = double.PositiveInfinity;

            for (int i = 1; i <= m; i++)
                DTW[0, i] = double.PositiveInfinity;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    double cost = d(X, i - 1, Y, j - 1, length + 1);
                    DTW[i, j] = cost + Math.Min(Math.Min(
                        DTW[i - 1, j],      // insertion
                        DTW[i, j - 1]),     // deletion
                        DTW[i - 1, j - 1]   // match
                    );
                }
            }

            return DTW[n, m]; // return the minimum global distance
        }

        /// <summary>
        ///   Local distance d(x,y) between two vectors.
        /// </summary>
        /// <param name="X">A sequence of fixed-length vectors X.</param>
        /// <param name="Y">A sequence of fixed-length vectors Y.</param>
        /// <param name="ix">The index of the vector in the sequence x.</param>
        /// <param name="iy">The index of the vector in the sequence y.</param>
        /// <param name="length">The fixed-length of the vectors in the sequences.</param>
        /// <returns>The local distance between x and y.</returns>
        /// 
        private static double d(double[] X, int ix, double[] Y, int iy, int length)
        {
            double p = 0; // the product <x,y>

            // Get the vectors' starting positions in the sequences
            int i = ix * length;
            int j = iy * length;

            // Compute the inner product between the vectors
            for (int k = 0; k < length; k++)
                p += X[i++] * Y[j++];

            // Assert the value is in the [-1;+1] range
            if (p > +1.0) p = +1.0;
            else if (p < -1.0) p = -1.0;

            // Return the arc-cosine of the inner product
            return Math.Acos(p);
        }


        /// <summary>
        ///   Projects vectors from a sequence of vectors into
        ///   a hypersphere, augmenting their size in one unit
        ///   and normalizing them to be unit vectors.
        /// </summary>
        /// <param name="x">A sequence of vectors.</param>
        /// <returns>A sequence of vector projections.</returns>
        private double[] snorm(double[] x)
        {
            // Get the number of vectors in the sequence
            int n = x.Length / length;

            // Create the augmented sequence projection
            double[] xs = new double[x.Length + n];

            // For each vector in the sequence
            for (int j = 0; j < n; j++)
            {
                // Compute its starting position in the
                //  source and destination sequences
                int src = j * length;
                int dst = j * (length + 1);

                // Compute augmented vector norm
                double norm = alpha * alpha;
                for (int i = src; i < src + length; i++)
                    norm += x[i] * x[i];
                norm = System.Math.Sqrt(norm);

                // Normalize the augmented vector and
                //  copy to the destination sequence
                xs[dst + length] = alpha / norm;
                for (int i = dst; i < dst + length; i++, src++)
                    xs[i] = x[src] / norm;
            }

            return xs; // return the projected sequence

            // Remarks: the above could be done much more
            // efficiently using unsafe pointer arithmetic.
        }

    }
}
