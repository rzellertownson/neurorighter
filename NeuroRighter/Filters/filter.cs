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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter.Filters
{
    using rawType = System.Double;
    abstract class filter
    {
        //note that this means that the default filter just passes data through, and that methods that do not implement the filterData
        //method will appear to do so in the command line.
        virtual public void filterData(rawType[] data) { }

        public void mein(string[] args)//filter data 
        {

            if (args.Length <2)
            {
                System.Console.Out.WriteLine("Not enough input arguments.");
                return;
            }
            StreamReader sr = new StreamReader(args[0]);
            StreamWriter sw = new StreamWriter(args[1]);
            string in_line = sr.ReadLine();
            string out_line;
            rawType[] data;
				//Continue to read until you reach end of file

            //filter data from ascii file one line at a time
            while (in_line != null)
            {
                //convert string to raw
                //System.Console.Out.WriteLine(in_line);

                string[] words = in_line.Split(' ');
                //System.Console.Out.WriteLine("here?");
                data = new rawType[words.Length-1];
                //System.Console.Out.WriteLine("here?");
                for(int i = 0;i<words.Length-1;i++)//-1 for the last one
                {
                    //System.Console.Out.WriteLine(":"+ words[i]);
                    data[i] = (rawType)Convert.ToDouble(words[i]);
                }
                
                //filter data
                filterData(data);

                //convert raw to string
                out_line = "";
                for (int i = 0; i < data.Length; i++)
                {
                    out_line += Convert.ToString(data[i]) + " ";
                }

                //output processed data in string form to output file.
                sw.WriteLine(out_line);
                in_line = sr.ReadLine();
            }
            //close file streams
            sw.Close();
            sr.Close();
        }
    }
}
