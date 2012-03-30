using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExtensionMethods
{
    internal class InVivoChannelMappings
    {
        /// <summary>
        /// Takes the hardware channel and outputs the display channel number ((row-1)*8+col)
        /// </summary>
        /// <param name="index">0-based channel number to map</param>
        /// <returns>1-based mapped channel number</returns>
        internal static short Channel2LinearCR(int index)
        {
            switch (index)
            {
                case 0: return 1;
                case 1: return 2;
                case 2: return 3;
                case 3: return 4;
                case 4: return 5;
                case 5: return 6;
                case 6: return 7;
                case 7: return 8;
                case 8: return 9;
                case 9: return 10;
                case 10: return 11;
                case 11: return 12;
                case 12: return 13;
                case 13: return 14;
                case 14: return 15;
                case 15: return 16;
                case 16: return 17;
                case 17: return 18;
                case 18: return 19;
                case 19: return 20;
                case 20: return 21;
                case 21: return 22;
                case 22: return 23;
                case 23: return 24;
                case 24: return 25;
                case 25: return 26;
                case 26: return 27;
                case 27: return 28;
                case 28: return 29;
                case 29: return 30;
                case 30: return 31;
                case 31: return 32;
                case 32: return 33;
                case 33: return 34;
                case 34: return 35;
                case 35: return 36;
                case 36: return 37;
                case 37: return 38;
                case 38: return 39;
                case 39: return 40;
                case 40: return 41;
                case 41: return 42;
                case 42: return 43;
                case 43: return 44;
                case 44: return 45;
                case 45: return 46;
                case 46: return 47;
                case 47: return 48;
                case 48: return 49;
                case 49: return 50;
                case 50: return 51;
                case 51: return 52;
                case 52: return 53;
                case 53: return 54;
                case 54: return 55;
                case 55: return 56;
                case 56: return 57;
                case 57: return 58;
                case 58: return 59;
                case 59: return 60;
                case 60: return 61;
                case 61: return 62;
                case 62: return 63;
                case 63: return 64;
                default: return -1;
            }
        }



    }
}
