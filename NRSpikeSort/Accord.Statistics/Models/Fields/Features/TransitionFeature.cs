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
    ///   Edge feature for Hidden Markov Model state transition probabilities.
    /// </summary>
    /// 
    public class TransitionFeature : EdgeFeature
    {
        private int prev;
        private int next;

        /// <summary>
        ///   Constructs a initial state transition feature.
        /// </summary>
        /// 
        /// <param name="state">The destination state.</param>
        /// 
        public TransitionFeature(int state)
        {
            this.prev = -1;
            this.next = state;
        }


        /// <summary>
        ///   Constructs a state transition feature.
        /// </summary>
        /// 
        /// <param name="previous">The previous state.</param>
        /// <param name="next">The next state.</param>
        /// 
        public TransitionFeature(int previous, int next)
        {
            this.prev = previous;
            this.next = next;
        }

        /// <summary>
        /// Computes the state transition feature for the given edge parameters.
        /// </summary>
        /// 
        /// <param name="previous">The originating state.</param>
        /// <param name="current">The destination state.</param>
        /// <param name="observations">The observations.</param>
        /// <param name="index">The index for the current observation.</param>
        /// 
        public override double Compute(int previous, int current, int[] observations, int index)
        {
            if (this.prev == previous && this.next == current)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }

    }
}
