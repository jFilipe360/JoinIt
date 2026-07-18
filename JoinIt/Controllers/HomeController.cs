using JoinIt.Data;
using JoinIt.Enums;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JoinIt.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger,ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: /Home/Index
        // Pagina inicial do site, que mostra os proximos eventos publicos que estao para acontecer
        public async Task<IActionResult> Index()
        {
            var agora = DateTime.Now;

            var proximosEventos = await _context.Eventos
                .AsNoTracking()
                .Include(e => e.Categoria)
                .Include(e => e.Participantes)
                .Where(e =>
                    !e.IsPrivado &&
                    e.Estado != EstadoEvento.Cancelado &&
                    e.DataHora >= agora)
                .OrderBy(e => e.DataHora)
                .Take(3)
                .ToListAsync();

            return View(proximosEventos);
        }

        // Pagina "Sobre" do site, que mostra informacoes sobre o projeto
        public IActionResult Privacy()
        {
            return View();
        }

        // Impede que a página de erro seja guardada em cache
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Apresenta uma página personalizada para erros HTTP
        [AllowAnonymous]
        public IActionResult ErroHttp(int code)
        {
            Response.StatusCode = code;
            ViewBag.Codigo = code;

            return View("StatusCode");
        }

        // Apresenta a página personalizada de acesso negado
        [AllowAnonymous]
        public IActionResult AcessoNegado()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;

            return View();
        }
    }
}
