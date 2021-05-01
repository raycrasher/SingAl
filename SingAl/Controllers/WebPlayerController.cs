using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SingAl.Models;
using SingAl.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Controllers
{
    public class WebPlayerController : Controller
    {
        private SingAlService _service;
        private SongRepository _repo;
        private IWebHostEnvironment _env;

        public WebPlayerController(SingAlService service, SongRepository repo, IWebHostEnvironment env)
        {
            _service = service;
            _repo = repo;
            _env = env;
        }

        [HttpGet]
        [Route("/video")]
        public IActionResult GetBackgroundVideo(int index)
        {
            return PhysicalFile(Path.Combine(_env.ContentRootPath, _service.BackgroundVideos[index % _service.BackgroundVideos.Length]), "application/octet-stream");
        }

        [HttpGet]
        [Route("/queue")]
        public IEnumerable<QueuedSong> GetSongQueue(Guid? singer)
        {
            if (singer != null)
            {
                return _service.SongQueue.Where(s => s.Singer.Id == singer.Value).ToArray();                
            }
            else
            {
                return _service.SongQueue.ToArray();
            }
        }

        [HttpGet]
        [Route("/lyrics")]
        public Task<IEnumerable<Lyric>> GetSongLyrics(Guid songId)
        {
            return _repo.GetSongLyrics(songId);
        }
    }
}
