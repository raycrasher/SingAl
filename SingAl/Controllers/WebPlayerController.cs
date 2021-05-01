using Microsoft.AspNetCore.Mvc;
using SingAl.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Controllers
{
    public class WebPlayerController : Controller
    {
        SingAlService _service;

        public WebPlayerController(SingAlService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetVideoFile()
        {
            return PhysicalFile(_service.CurrentVideoFilePath, "application/octet-stream");
        }
    }
}
