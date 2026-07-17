using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JoinIt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult StatusCode(int code)
        {
            Response.StatusCode = code;

            ViewBag.Codigo = code;

            return View();
        }

        [AllowAnonymous]
        public IActionResult AcessoNegado()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;

            return View();
        }
    }
}
