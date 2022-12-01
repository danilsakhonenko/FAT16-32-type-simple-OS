using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chaos.Structures
{
    internal static class Superblock
    {
        public static string Title { get; private set; } = "ChaOS";
        public static short ClusterSize { get; private set; } = 4096;
        public static short FATSize { get; private set; } = 1024;
        public static short FATCount { get; private set; } = 2;
        public static short UsersAreaSize { get; private set; } = 176;
        public static int DataAreaSize { get; private set; } = 2099392;
        public static int Size { get; private set; } = 16;
        
        public static int TablesStart => Size;
        public static int UsersAreaStart => TablesStart + (FATSize * FATCount);
        public static int DataAreaStart => UsersAreaStart + UsersAreaSize;
        public static int FSSize => 2099392;
    }
}
