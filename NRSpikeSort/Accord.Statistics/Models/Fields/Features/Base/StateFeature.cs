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

namespace Accord.Statistics.Models.Fields.Features
{

    /// <summary>
    ///   Abstract class for CRF's State features.
    /// </summary>
    /// 
    public abstract class StateFeature : IFeature
    {


        /// <summary>
        ///   Computes the feature for the given parameters.
        /// </summary>
        /// 
        /// <param name="previous">The previous state.</param>
        /// <param name="current">The current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="index">The index of the current observation.</param>
        /// 
        double IFeature.Compute(int previous, int current, int[] observations, int index)
        {
            return Compute(current, observations, index);
        }


        /// <summary>
        ///   Computes the state feature for the given state parameters.
        /// </summary>
        /// 
        /// <param name="currentState">The current state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="index">The index for the current observation.</param>
        /// 
        public abstract double Compute(int currentState, int[] observations, int index);

    }
}
