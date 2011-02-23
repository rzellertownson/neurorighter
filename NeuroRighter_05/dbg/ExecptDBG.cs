using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.dbg
{
    ///<summary>Class for housing debuging methods and other stuffs like that.</summary>
    ///<author>Jon Newman</author>
    class ExecptDBG
    {
        //Constructor
        internal ExecptDBG()
        {
        }

        internal void DisplayInnerException(Exception e, object reflectedClass)
        {
            Console.WriteLine("MESSAGE: " + e.Message);
            Console.WriteLine("SOURCE: " + e.Source);
            Console.WriteLine("TARGET: " + e.TargetSite);
            Console.WriteLine("STACK: " + e.StackTrace + "\r\n");

            if (e.InnerException != null)
            {
                Console.WriteLine();
                Console.WriteLine("\t**** INNEREXCEPTION START ****");
                Console.WriteLine("\tTYPE THAT THREW EXCEPTION: " +
                                  reflectedClass.ToString());
                Console.WriteLine("\tINNEREXCEPTION MESSAGE: " +
                                  e.InnerException.Message);
                Console.WriteLine("\tINNEREXCEPTION SOURCE: " +
                                  e.InnerException.Source);
                Console.WriteLine("\tINNEREXCEPTION STACK: " +
                                  e.InnerException.StackTrace);
                Console.WriteLine("\tINNEREXCEPTION TARGETSITE: " +
                                  e.InnerException.TargetSite);
                Console.WriteLine("\t****  INNEREXCEPTION END  ****");
            }

            Console.WriteLine();

            // Shows fusion log when assembly cannot be located
            Console.WriteLine(e.ToString());
        }
    }
}
