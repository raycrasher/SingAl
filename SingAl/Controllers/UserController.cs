using Microsoft.AspNetCore.Mvc;
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

        public UserController(SingAlService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> QueueSong(Guid songId)
        {
            await _service.QueueSong(songId);
            return Ok();
        }
    }
}
