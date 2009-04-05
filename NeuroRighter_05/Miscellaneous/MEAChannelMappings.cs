using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeuroRighter
{

    /// <author>John Rolston (rolston2@gmail.com)</author>
    internal class MEAChannelMappings
    {
        internal static short[] usedChannels = new short[] {0,1,2,3,4,5,6,7,8,9,
                                                   10,11,12,13,15,16,17,18,19,
                                                   20,21,22,23,24,25,26,27,28,29,
                                                   30,31,32,33,34,35,36,37,38,39,
                                                   40,41,42,43,48,49,
                                                   50,51,52,53,54,55,56,57,58,59,
                                                   60,61,62,63};
        //Mapping for recording (Changed for new setup)
        internal static short[,] ch2rc = new short[,] { 
                    {6, 8}, //1
                    {7, 8}, //2
                    {6, 6}, //3
                    {7, 6}, //4
                    {5, 5},
                    {8, 5},
                    {1, 1},
                    {8, 1}, //8
                    {6, 7},
                    {7, 7},
                    {8, 7},
                    {8, 6},
                    {6, 5},
                    {7, 5},
                    {1, 8},
                    {8, 8}, //16
                    {3, 5},
                    {1, 6},
                    {1, 7},
                    {2, 7},
                    {3, 7},
                    {4, 6},
                    {4, 8},
                    {5, 7}, //24
                    {4, 5},
                    {2, 6},
                    {3, 6},
                    {2, 8},
                    {3, 8},
                    {4, 7},
                    {5, 8},
                    {5, 6},
                    {4, 2},
                    {3, 1},
                    {2, 1},
                    {3, 3},
                    {2, 3},
                    {4, 4},
                    {1, 4},
                    {2, 5},
                    {4, 3},
                    {3, 2},
                    {2, 2},
                    {1, 2},
                    {1, 3},
                    {3, 4},
                    {2, 4},
                    {1, 5},
                    {7, 4},
                    {6, 4},
                    {8, 3},
                    {8, 2},
                    {7, 2},
                    {6, 2},
                    {5, 3},
                    {5, 1},
                    {8, 4},
                    {5, 4},
                    {7, 3},
                    {6, 3},
                    {7, 1},
                    {6, 1},
                    {5, 2},
                    {4, 1} };
        //Mapping for stimulating (Chad's setup)
        /* internal static short[] ch2stimChannel = new short[] { -1, 36, 35, 37, 46, 47, 43, -1,
                                                            29, 11, 39, 33, 48, 45, 52, 55,
                                                            26, 31, 40, 34, 44, 41, 51, 54,
                                                            32, 30, 28, 38, 42, 50, 53, 49,
                                                            -1, 21, 18, 10, 6,  60, 62, 64,
                                                            22, 19, 9,  12, 2,  8,  63, 58,
                                                            23, 20, 13, 16, 1,  7,  59, 61,
                                                            -1, 11, 15, 14, 5,  3,  4,  -1}; */

        // WAFA_CHANGE2:  With the new setup, some of the values for the above map are SWITCHED.  They have been modified for the below map.       
        //SC 61 goes to electrodes 1, 57, 61 and SC 53 goes to electrodes 8, 53, 64  
        internal static short[] ch2stimChannel = new short[] { -1, 36, 35, 37, 46, 47, 43, -1,
                                                               13, 11, 39, 33, 48, 45, 52, 55,
                                                               10, 15, 40, 34, 44, 41, 51, 54,
                                                               16, 14, 12, 38, 42, 50, 53, 49,
                                                               -1,  5,  2, 26, 22, 60, 62, 64,
                                                                6,  3, 25, 28, 18, 24, 63, 58,
                                                                7,  4, 29, 32, 17, 23, 59, 61,
                                                               -1, 27, 31, 30, 21, 19, 20, -1};
        //For reference only, to make it easy to see SC mapping.
        /*internal static short[] ch2stimChannel = new short[] { 1, 2, 3, 4, 5, 6, 7, 8,
                                                                 9, 10, 11, 12, 13, 14, 15, 16,
                                                                 17, 18, 19, 20, 21, 22, 23, 24, 
                                                                 25, 26, 27, 28, 29, 30, 31, 32,
                                                                 33, 34, 35, 36, 37, 38, 39, 40,
                                                                 41, 42, 43, 44, 45, 46, 47, 48,
                                                                 49, 50, 51, 52, 53, 54, 55, 56,
                                                                 57, 58, 59, 60, 61, 62, 63, 64}; */

        internal struct Coords
        {
            public double x;
            public double y;
            public Coords(double x, double y) { this.x = x; this.y = y; }
        }

        internal Dictionary<int, Coords> CoordFromChannel;
        internal MEAChannelMappings()
        {
            CoordFromChannel = new Dictionary<int, Coords>(usedChannels.Length);
            for (int i = 0; i < usedChannels.Length; ++i)
                CoordFromChannel.Add(usedChannels[i], channel2Coord(usedChannels[i]));
        }

        internal static Coords channel2Coord(int channel)
        {
            //Channel is 0-based.  Coords are row/col (y/x)
            switch (channel)
            {
                case 0:
                    return new Coords(6, 8);
                case 1:
                    return new Coords(7, 8);
                case 2:
                    return new Coords(6, 6);
                case 3:
                    return new Coords(7, 6);
                case 4:
                    return new Coords(5, 5);
                case 5:
                    return new Coords(8, 5);
                case 6:
                    return new Coords(1, 1);
                case 7:
                    return new Coords(8, 1);
                case 8:
                    return new Coords(6, 7);
                case 9:
                    return new Coords(7, 7);
                case 10:
                    return new Coords(8, 7);
                case 11:
                    return new Coords(8, 6);
                case 12:
                    return new Coords(6, 5);
                case 13:
                    return new Coords(7, 5);
                case 14:
                    return new Coords(1, 8);
                case 15:
                    return new Coords(8, 8);
                case 16:
                    return new Coords(3, 5);
                case 17:
                    return new Coords(1, 6);
                case 18:
                    return new Coords(1, 7);
                case 19:
                    return new Coords(2, 7);
                case 20:
                    return new Coords(3, 7);
                case 21:
                    return new Coords(4, 6);
                case 22:
                    return new Coords(4, 8);
                case 23:
                    return new Coords(5, 7);
                case 24:
                    return new Coords(4, 5);
                case 25:
                    return new Coords(2, 6);
                case 26:
                    return new Coords(3, 6);
                case 27:
                    return new Coords(2, 8);
                case 28:
                    return new Coords(3, 8);
                case 29:
                    return new Coords(4, 7);
                case 30:
                    return new Coords(5, 8);
                case 31:
                    return new Coords(5, 6);
                case 32:
                    return new Coords(4, 2);
                case 33:
                    return new Coords(3, 1);
                case 34:
                    return new Coords(2, 1);
                case 35:
                    return new Coords(3, 3);
                case 36:
                    return new Coords(2, 3);
                case 37:
                    return new Coords(4, 4);
                case 38:
                    return new Coords(1, 4);
                case 39:
                    return new Coords(2, 5);
                case 40:
                    return new Coords(4, 3);
                case 41:
                    return new Coords(3, 2);
                case 42:
                    return new Coords(2, 2);
                case 43:
                    return new Coords(1, 2);
                case 44:
                    return new Coords(1, 3);
                case 45:
                    return new Coords(3, 4);
                case 46:
                    return new Coords(2, 4);
                case 47:
                    return new Coords(1, 5);
                case 48:
                    return new Coords(7, 4);
                case 49:
                    return new Coords(6, 4);
                case 50:
                    return new Coords(8, 3);
                case 51:
                    return new Coords(8, 2);
                case 52:
                    return new Coords(7, 2);
                case 53:
                    return new Coords(6, 2);
                case 54:
                    return new Coords(5, 3);
                case 55:
                    return new Coords(5, 1);
                case 56:
                    return new Coords(8, 4);
                case 57:
                    return new Coords(5, 4);
                case 58:
                    return new Coords(7, 3);
                case 59:
                    return new Coords(6, 3);
                case 60:
                    return new Coords(7, 1);
                case 61:
                    return new Coords(6, 1);
                case 62:
                    return new Coords(5, 2);
                case 63:
                    return new Coords(4, 1);
                default:
                    return new Coords(double.NaN, double.NaN); //ERROR!
            }
        }

        /// <summary>
        /// Takes the hardware channel and outputs the display channel number ((row-1)*8+col)
        /// </summary>
        /// <param name="index">0-based channel number to map</param>
        /// <returns>0-based mapped channel number</returns>
        internal static short channel2LinearCR(int index)
        {
            // 0 indexed
            return (short) (channel2LinearCRplus1((short) index) - 1);
        }

        internal static short channel2LinearCRplus1(short index)
        {
            // 1 indexed
            switch (index)
            {
                case 0: return 48;
                case 1: return 56;
                case 2: return 46;
                case 3: return 54;
                case 4: return 37;
                case 5: return 61;
                case 6: return 1;
                case 7: return 57;
                case 8: return 47;
                case 9: return 55;
                case 10: return 63;
                case 11: return 62;
                case 12: return 45;
                case 13: return 53;
                case 14: return 8;
                case 15: return 64;
                case 16: return 21;
                case 17: return 6;
                case 18: return 7;
                case 19: return 15;
                case 20: return 23;
                case 21: return 30;
                case 22: return 32;
                case 23: return 39;
                case 24: return 29;
                case 25: return 14;
                case 26: return 22;
                case 27: return 16;
                case 28: return 24;
                case 29: return 31;
                case 30: return 40;
                case 31: return 38;
                case 32: return 26;
                case 33: return 17;
                case 34: return 9;
                case 35: return 19;
                case 36: return 11;
                case 37: return 28;
                case 38: return 4;
                case 39: return 13;
                case 40: return 27;
                case 41: return 18;
                case 42: return 10;
                case 43: return 2;
                case 44: return 3;
                case 45: return 20;
                case 46: return 12;
                case 47: return 5;
                case 48: return 52;
                case 49: return 44;
                case 50: return 59;
                case 51: return 58;
                case 52: return 50;
                case 53: return 42;
                case 54: return 35;
                case 55: return 33;
                case 56: return 60;
                case 57: return 36;
                case 58: return 51;
                case 59: return 43;
                case 60: return 49;
                case 61: return 41;
                case 62: return 34;
                case 63: return 25;
                default: return -1;
            }
        }

    }
}
