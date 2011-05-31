// PROGRAM.CS
// Copyright (c) 2008-2011 John Rolston
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
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace NeuroRighter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        static void Main()
        {
            Type reflectedClass = typeof(NeuroRighter);
            using (Process p = Process.GetCurrentProcess())
                p.PriorityClass = ProcessPriorityClass.High;
            Thread thrd = Thread.CurrentThread;

            thrd.Priority = ThreadPriority.BelowNormal;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new NeuroRighter());
            }

            //Using reflection, you invoke a method that generates an exception. 
            //You want to obtain the real exception object and its information in order to diagnose and fix the problem.
            //The real exception and its information can be obtained through the InnerException property of
            //the TargetInvocationException exception that is thrown by MethodInfo.Invoke. -J.N.
            catch (Exception startEx)
            {
                dbg.ExecptDBG startErrorHandler = new dbg.ExecptDBG();
                startErrorHandler.DisplayInnerException(startEx, reflectedClass);
            }






        }
    }
}