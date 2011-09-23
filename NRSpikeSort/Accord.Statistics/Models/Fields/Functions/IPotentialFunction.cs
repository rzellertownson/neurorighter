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

namespace Accord.Statistics.Models.Fields.Functions
{
    using Accord.Statistics.Models.Fields.Features;

    /// <summary>
    ///   Common interface for CRF's Potential functions.
    /// </summary>
    /// 
    public interface IPotentialFunction
    {
        /// <summary>
        ///   Gets the number of model states
        ///   assumed by this function.
        /// </summary>
        /// 
        int States { get; }

        /// <summary>
        ///   Gets or sets the set of weights for each feature function.
        /// </summary>
        /// 
        /// <value>The weights for each of the feature functions.</value>
        /// 
        double[] Weights { get; set; }

        /// <summary>
        ///   Gets the feature functions composing this potential function.
        /// </summary>
        /// 
        IFeature[] Features { get; }


        /// <summary>
        ///   Computes the potential function given the specified parameters.
        /// </summary>
        /// 
        /// <param name="previous">Previous state.</param>
        /// <param name="state">Current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="index">The index for the current observation.</param>
        /// 
        double Compute(int previous, int state, int[] observations, int index);

        /// <summary>
        ///   Computes the log of the potential function given the specified parameters.
        /// </summary>
        /// 
        /// <param name="previous">Previous state.</param>
        /// <param name="state">Current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="index">The index for the current observation.</param>
        /// 
        double LogCompute(int previous, int state, int[] observations, int index);
        
    }
}
