using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pnpCL;
using System.Windows.Forms;

namespace RileyClosedLoops
{
    public class BakkumExpt:pnpClosedLoopAbs
    {
        public override void close()
        {
            
        }

        public override void run()
        {
            MessageBox.Show("bakkum!!");
        }
    }
}
