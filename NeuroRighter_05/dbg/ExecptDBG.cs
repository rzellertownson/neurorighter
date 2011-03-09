using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.dbg
{
    ///<summary>Class for housing debugGing methods and other stuffs like that.</summary>
    ///<author>Jon Newman</author>
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
