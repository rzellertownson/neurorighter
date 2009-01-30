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

        internal static short[,] ch2rc = new short[,] { 
                    {7, 4}, //1
                    {8, 4}, //2
                    {6, 4}, //3
                    {5, 4}, //4
                    {8, 3},
                    {7, 3},
                    {8, 2},
                    {6, 3}, //8
                    {7, 2},
                    {7, 1},
                    {6, 2},
                    {6, 1},
                    {5, 3},
                    {5, 2},
                    {5, 1},
                    {4, 1}, //16
                    {4, 2},
                    {4, 3},
                    {3, 1},
                    {3, 2},
                    {2, 1},
                    {2, 2},
                    {3, 3},
                    {1, 2}, //24
                    {2, 3},
                    {1, 3},
                    {4, 4},
                    {3, 4},
                    {1, 4},
                    {2, 4},
                    {2, 5},
                    {1, 5},
                    {6, 8},
                    {6, 7},
                    {7, 8},
                    {7, 7},
                    {6, 6},
                    {8, 7},
                    {7, 6},
                    {8, 6},
                    {5, 5},
                    {6, 5},
                    {8, 5},
                    {7, 5},
                    {1, 1},
                    {1, 8},
                    {8, 1},
                    {8, 8},
                    {3, 5},
                    {4, 5},
                    {1, 6},
                    {2, 6},
                    {1, 7},
                    {3, 6},
                    {2, 7},
                    {2, 8},
                    {3, 7},
                    {3, 8},
                    {4, 6},
                    {4, 7},
                    {4, 8},
                    {5, 8},
                    {5, 7},
                    {5, 6} };

        internal static short[] ch2stimChannel = new short[] { -1, 36, 35, 37, 46, 47, 43, -1,
                                                            29, 27, 39, 33, 48, 45, 52, 55,
                                                            26, 31, 40, 34, 44, 41, 51, 54,
                                                            32, 30, 28, 38, 42, 50, 53, 49,
                                                            -1, 21, 18, 10, 6,  60, 62, 64,
                                                            22, 19, 9,  12, 2,  8,  63, 58,
                                                            23, 20, 13, 16, 1,  7,  59, 61,
                                                            -1, 11, 15, 14, 5,  3,  4,  -1};

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
                    return new Coords(7, 4);
                case 1:
                    return new Coords(8, 4);
                case 2:
                    return new Coords(6, 4);
                case 3:
                    return new Coords(5, 4);
                case 4:
                    return new Coords(8, 3);
                case 5:
                    return new Coords(7, 3);
                case 6:
                    return new Coords(8, 2);
                case 7:
                    return new Coords(6, 3);
                case 8:
                    return new Coords(7, 2);
                case 9:
                    return new Coords(7, 1);
                case 10:
                    return new Coords(6, 2);
                case 11:
                    return new Coords(6, 1);
                case 12:
                    return new Coords(5, 3);
                case 13:
                    return new Coords(5, 2);
                case 14:
                    return new Coords(5, 1);
                case 15:
                    return new Coords(4, 1);
                case 16:
                    return new Coords(4, 2);
                case 17:
                    return new Coords(4, 3);
                case 18:
                    return new Coords(3, 1);
                case 19:
                    return new Coords(3, 2);
                case 20:
                    return new Coords(2, 1);
                case 21:
                    return new Coords(2, 2);
                case 22:
                    return new Coords(3, 3);
                case 23:
                    return new Coords(1, 1);
                case 24:
                    return new Coords(2, 3);
                case 25:
                    return new Coords(1, 3);
                case 26:
                    return new Coords(4, 4);
                case 27:
                    return new Coords(3, 4);
                case 28:
                    return new Coords(1, 4);
                case 29:
                    return new Coords(2, 4);
                case 30:
                    return new Coords(2, 5);
                case 31:
                    return new Coords(1, 5);
                case 32:
                    return new Coords(6, 8);
                case 33:
                    return new Coords(6, 7);
                case 34:
                    return new Coords(7, 8);
                case 35:
                    return new Coords(7, 7);
                case 36:
                    return new Coords(6, 6);
                case 37:
                    return new Coords(8, 7);
                case 38:
                    return new Coords(7, 6);
                case 39:
                    return new Coords(8, 6);
                case 40:
                    return new Coords(5, 5);
                case 41:
                    return new Coords(6, 5);
                case 42:
                    return new Coords(8, 5);
                case 43:
                    return new Coords(7, 5);
                case 44:
                    return new Coords(7, 5);
                case 45:
                    return new Coords(7, 5);
                case 46:
                    return new Coords(7, 5);
                case 47:
                    return new Coords(7, 5);
                case 48:
                    return new Coords(3, 5);
                case 49:
                    return new Coords(4, 5);
                case 50:
                    return new Coords(1, 6);
                case 51:
                    return new Coords(2, 6);
                case 52:
                    return new Coords(1, 7);
                case 53:
                    return new Coords(3, 6);
                case 54:
                    return new Coords(2, 7);
                case 55:
                    return new Coords(2, 8);
                case 56:
                    return new Coords(3, 7);
                case 57:
                    return new Coords(3, 8);
                case 58:
                    return new Coords(4, 6);
                case 59:
                    return new Coords(4, 7);
                case 60:
                    return new Coords(4, 8);
                case 61:
                    return new Coords(5, 8);
                case 62:
                    return new Coords(5, 7);
                case 63:
                    return new Coords(5, 6);
                default:
                    return new Coords(double.NaN, double.NaN); //ERROR!
            }
        }

        internal static int channel2CR(int index)
        {
            switch (index)
            {
                case 0:
                    return 44;
                case 1:
                    return 23; //21
                case 2:
                    return 25; //31
                case 3:
                    return 28; //41
                case 4:
                    return 31; //51
                case 5:
                    return 50; //61
                case 6:
                    return 52; //71
                case 7:
                    return 45;
                case 8:
                    return 20; //12
                case 9:
                    return 21; //22
                case 10:
                    return 24; //32
                case 11:
                    return 29; //42
                case 12:
                    return 30; //52
                case 13:
                    return 51; //62
                case 14:
                    return 54; //72
                case 15:
                    return 55; //82
                case 16:
                    return 18; //13
                case 17:
                    return 19; //23
                case 18:
                    return 22; //33
                case 19:
                    return 27; //43
                case 20:
                    return 48; //53
                case 21:
                    return 53; //63
                case 22:
                    return 56; //73
                case 23:
                    return 57; //83
                case 24:
                    return 15; //14
                case 25:
                    return 16; //24
                case 26:
                    return 17; //34
                case 27:
                    return 26; //44
                case 28:
                    return 49; //54
                case 29:
                    return 58; //64
                case 30:
                    return 59; //74
                case 31:
                    return 60; //84
                case 32:
                    return 14; //15
                case 33:
                    return 13; //25
                case 34:
                    return 12; //35
                case 35:
                    return 3; //45
                case 36:
                    return 40; //55
                case 37:
                    return 63; //65
                case 38:
                    return 62; //75
                case 39:
                    return 61; //85
                case 40:
                    return 11; //16
                case 41:
                    return 10; //26
                case 42:
                    return 7; //36
                case 43:
                    return 2; //46
                case 44:
                    return 41; //56
                case 45:
                    return 36; //66
                case 46:
                    return 33; //76
                case 47:
                    return 32; //86
                case 48:
                    return 9; //17
                case 49:
                    return 8; //27
                case 50:
                    return 5; //37
                case 51:
                    return 0; //47
                case 52:
                    return 43; //57
                case 53:
                    return 38; //67
                case 54:
                    return 35; //77
                case 55:
                    return 34; //87
                case 56:
                    return 46;
                case 57:
                    return 6; //28
                case 58:
                    return 4; //38
                case 59:
                    return 1; //48
                case 60:
                    return 42; //58
                case 61:
                    return 39; //68
                case 62:
                    return 37; //78
                case 63:
                    return 47;
                default:
                    return -1;
            }
        }
    }
}
