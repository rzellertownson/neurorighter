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
    ///   State feature for Hidden Markov Model symbol emission probabilities.
    /// </summary>
    /// 
    public class EmissionFeature : StateFeature
    {

       int state;
       int symbol;

       /// <summary>
       ///   Constructs a new symbol emission feature.
       /// </summary>
       /// 
       /// <param name="state">The state for the emission.</param>
       /// <param name="symbol">The emission symbol.</param>
       /// 
       public EmissionFeature(int state, int symbol)
       {
           this.state = state;
           this.symbol = symbol;
       }

       /// <summary>
       ///   Computes the state feature for the given state parameters.
       /// </summary>
       /// 
       /// <param name="currentState">The current state.</param>
       /// <param name="observations">The observations.</param>
       /// <param name="index">The index for the current observation.</param>
       /// 
        public override double Compute(int currentState, int[] observations, int index)
        {
            if (currentState == this.state && observations[index] == this.symbol)
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
