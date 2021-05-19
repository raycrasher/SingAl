using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl
{
    public class AppSettings
    {
        public string SongDirectory { get; set; }
        public string SongCacheDir { get; set; }
        public string LamePath { get; set; }
        public string TimidityPath { get; internal set; }
        public string ConvertedSongFormatExtension { get; set; }
    }
}
