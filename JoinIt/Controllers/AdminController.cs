using JoinIt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUtilizadores =
                await _context.Users.CountAsync();

            ViewBag.TotalEventos =
                await _context.Eventos.CountAsync();

            ViewBag.EventosPublicos =
                await _context.Eventos
                    .CountAsync(e => !e.IsPrivado);

            ViewBag.EventosPrivados =
                await _context.Eventos
                    .CountAsync(e => e.IsPrivado);

            ViewBag.TotalCategorias =
                await _context.Categorias.CountAsync();

            ViewBag.TotalParticipantes =
                await _context.Participantes.CountAsync();

            ViewBag.TotalAmizades =
                await _context.Amizades.CountAsync();

            ViewBag.TotalConvites =
                await _context.ConvitesEventos.CountAsync();

            ViewBag.TotalMensagens =
                await _context.Mensagens.CountAsync();

            return View();
        }
    }
}