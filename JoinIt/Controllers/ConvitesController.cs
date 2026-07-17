using JoinIt.Data;
using JoinIt.Enums;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Controllers
{
    [Authorize]
    public class ConvitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConvitesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var convites = await _context.ConvitesEventos
                .AsNoTracking()
                .Include(c => c.Evento)
                    .ThenInclude(e => e.Criador)
                .Include(c => c.Evento)
                    .ThenInclude(e => e.Categoria)
                .Include(c => c.Evento)
                    .ThenInclude(e => e.Participantes)
                .Where(c => c.UtilizadorId == userId)
                .OrderByDescending(c => c.DataConvite)
                .ToListAsync();

            return View(convites);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aceitar(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var convite = await _context.ConvitesEventos
                .Include(c => c.Evento)
                    .ThenInclude(e => e.Participantes)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UtilizadorId == userId);

            if (convite == null)
            {
                return NotFound();
            }

            if (convite.Estado != EstadoConvite.Pendente)
            {
                return RedirectToAction(nameof(Index));
            }

            var evento = convite.Evento;

            if (evento.Estado == EstadoEvento.Cancelado || evento.Estado == EstadoEvento.Terminado)
            {
                TempData["Erro"] =
                    "Este evento já não aceita participantes.";

                return RedirectToAction(nameof(Index));
            }

            var jaParticipa = evento.Participantes
                .Any(p => p.UserId == userId);

            if (!jaParticipa &&
                evento.Participantes.Count >= evento.NumMaxParticipantes)
            {
                TempData["Erro"] =
                    "O evento já atingiu o número máximo de participantes.";

                return RedirectToAction(nameof(Index));
            }

            if (!jaParticipa)
            {
                var participante = new Participante
                {
                    EventoId = evento.Id,
                    UserId = userId,
                    DataEntrada = DateTime.Now,
                    IsOrganizador = false
                };

                _context.Participantes.Add(participante);
            }

            convite.Estado = EstadoConvite.Aceite;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Convite aceite com sucesso.";

            return RedirectToAction(
                "Details",
                "Eventos",
                new { id = evento.Id }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rejeitar(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var convite = await _context.ConvitesEventos
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UtilizadorId == userId &&
                    c.Estado == EstadoConvite.Pendente);

            if (convite == null)
            {
                return NotFound();
            }

            convite.Estado = EstadoConvite.Rejeitado;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Convite rejeitado.";

            return RedirectToAction(nameof(Index));
        }
    }
}