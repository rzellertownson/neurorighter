using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace simoc.UI
{
    public class TargetEventArgs : EventArgs
    {
        private bool resetEventTime;

        public TargetEventArgs(bool resetEventTime)
        {
            this.resetEventTime = resetEventTime; 
        }

        public bool ResetEventTime
        {
            get
            {
                return resetEventTime;
            }
        }

    }
}
