// Copyright (c) 2008-2012 Potter Lab
//
// This file is part of NeuroRighter.
//
// NeuroRighter is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// NeuroRighter is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with NeuroRighter.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.dbg
{
    ///<summary>Class for housing debugging methods and properties.</summary>
    class ExecptDBG
    {
        //Constructor
        internal ExecptDBG(){}

        internal void DisplayInnerException(Exception e, object reflectedClass)
        {
            try
            {
                Console.WriteLine("\r\n\t **** OUTER-EXCEPTION START ****");
                Console.WriteLine("\tMESSAGE: " + e.Message);
                Console.WriteLine("\tSOURCE: " + e.Source);
                Console.WriteLine("\tTARGET: " + e.TargetSite);
                Console.WriteLine("\tSTACK: " + e.StackTrace);
                Console.WriteLine("\t\t****  OUTER-EXCEPTION END  ****\r\n");

                if (e.InnerException != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("\t\t**** INNER-EXCEPTION START ****");
                    Console.WriteLine("\t\tTYPE THAT THREW EXCEPTION: " +
                                      reflectedClass.ToString());
                    Console.WriteLine("\t\tINNEREXCEPTION MESSAGE: " +
                                      e.InnerException.Message);
                    Console.WriteLine("\t\tINNEREXCEPTION SOURCE: " +
                                      e.InnerException.Source);
                    Console.WriteLine("\t\tINNEREXCEPTION STACK: " +
                                      e.InnerException.StackTrace);
                    Console.WriteLine("\t\tINNEREXCEPTION TARGETSITE: " +
                                      e.InnerException.TargetSite);
                    Console.WriteLine("\t\t****  INNEREXCEPTION END  ****");
                }

                Console.WriteLine();
            }
            catch
            {
                // Shows fusion log when assembly cannot be located
                Console.WriteLine(e.ToString());
            }
        }
    }
}
