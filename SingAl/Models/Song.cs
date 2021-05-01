using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Models
{
    public class Song
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string[] Tags { get; set; }
    }
}
