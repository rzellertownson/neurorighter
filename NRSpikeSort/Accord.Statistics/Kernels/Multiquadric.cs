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
    using System;

    /// <summary>
    ///   Multiquadric (and inverse multiquadric) Kernel.
    /// </summary>
    /// 
    /// <remarks>
    ///   The multiquadric kernel is only positive semi-definite.
    /// </remarks>
    /// 
    [Serializable]
    public sealed class Multiquadric : IKernel
    {

        private bool inverse;
        private double constant;


        /// <summary>
        ///   Gets or sets the kernel's constant value.
        /// </summary>
        /// 
        public double Constant
        {
            get { return constant; }
            set { constant = value; }
        }

        /// <summary>
        ///   Gets or sets whether this is a standard
        ///   or inverse multi-quadric kernel.
        ///   
        /// </summary>
        public bool Inverse
        {
            get { return inverse; }
            set { inverse = value; }
        }

        /// <summary>
        ///   Constructs a new Multiquadric Kernel.
        /// </summary>
        /// 
        /// <param name="inverse">True for the Inverse Multiquadric Kernel, false otherwise.</param>
        /// <param name="constant">The constant term theta.</param>
        /// 
        public Multiquadric(bool inverse, double constant)
        {
            this.inverse = inverse;
            this.constant = constant;
        }

        /// <summary>
        ///   Constructs a new Multiquadric Kernel.
        /// </summary>
        /// 
        public Multiquadric()
            : this(false, 1)
        {
        }

        /// <summary>
        ///   (Inverse) Multiquadric Kernel function.
        /// </summary>
        /// 
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        /// 
        public double Function(double[] x, double[] y)
        {
            double norm = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                double d = x[i] - y[i];
                norm += d * d;
            }

            double beta = norm + constant * constant;

            return inverse ? 1.0 / beta : beta;
        }

    }
}
