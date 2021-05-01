using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Services
{
    public class SingAlService
    {
        public string CurrentVideoFilePath { get; set; }

        internal Task QueueSong(Guid songId)
        {
            throw new NotImplementedException();
        }
    }
}
