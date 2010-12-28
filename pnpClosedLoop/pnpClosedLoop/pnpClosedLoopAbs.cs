using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeuroRighter;

namespace pnpCL
{
    //abstract class for all plug and play closed loop experiments
    abstract public class pnpClosedLoopAbs
    {
        //object for recording, stimming, and GUI-ing
        protected ClosedLoopExpt CLE;

        //get the actual object
        public void grab(ClosedLoopExpt CLE)
        {
            this.CLE = CLE;
        }
        
        //abstract run that must be implemented by each protocol individually.
         abstract public void run();
        
        //what to do when the experiment ends or is shut down- shut down file streams and whatnot
         abstract public void close();
    }
}
