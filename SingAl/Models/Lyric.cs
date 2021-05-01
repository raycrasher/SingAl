using System;

namespace SingAl.Models
{
    public class Lyric
    {
        public TimeSpan Timestamp { get; set; }
        public TimeSpan Length { get; set; }
        public string Text { get; set; }
        public int SingerIndex { get; set; } 
    }
}
