using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SingAl.Models;
using SingAl.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Controllers
{
    public class UserController : Controller
    {
        SingAlService _service;
        SongRepository _repo;
        IHubContext<WebPlayerHub> _hubcontext;

        public record JoinModel(string Nickname);
        public record QueueSongModel(Guid SingerId, Guid SongId);

        public UserController(SingAlService service, SongRepository repo, IHubContext<WebPlayerHub> hubcontext)
        {
            _service = service;
            _repo = repo;
            _hubcontext = hubcontext;
        }

        [HttpPost]
        [Route("/addsong")]
        public async Task<IActionResult> QueueSong([FromBody] QueueSongModel parameters)
        {
            var (singer, song) = await _service.QueueSong(parameters.SingerId, parameters.SongId);
            if(singer == null)
            {
                return BadRequest("No such singer exists");
            }
            if (song == null)
            {
                return BadRequest("No such song exists");
            }

            await _hubcontext.Clients.All.SendAsync("SongAdded", singer, song);
            Console.WriteLine($"{singer.Nickname} has enqueued {song.Title}");
            return Ok();
        }

        [HttpPost]
        [Route("/join")]
        public IActionResult AddSinger([FromBody] JoinModel join)
        {
            var result = _service.TryAddSinger(join.Nickname);
            if (!result.Ok)
                return BadRequest(result.Error);
            else
            {
                Console.WriteLine($"User {join.Nickname} has logged in");
                return Json(new { singerId = result.Id });
            }
        }

        [HttpGet]
        [Route("/search")]
        public async Task<IEnumerable<Song>> SearchSong(string query)
        {
            return await _repo.GetMatchingSongs(query);
        }

        [HttpPost]
        [Route("/pause")]
        public IActionResult Pause()
        {
            _hubcontext.Clients.All.SendAsync("Pause");
            return Ok();
        }

        [HttpPost]
        [Route("/play")]
        public IActionResult Play()
        {
            _hubcontext.Clients.All.SendAsync("Play");
            return Ok();
        }

        [HttpPost]
        [Route("/skip")]
        public IActionResult Skip()
        {
            _hubcontext.Clients.All.SendAsync("SkipCurrent");
            return Ok();
        }
    }
}
