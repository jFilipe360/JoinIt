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
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UtilizadorId == userId &&
                    c.Estado == EstadoPedido.Pendente);

            if (convite == null)
            {
                return NotFound();
            }

            if (convite.Estado != EstadoPedido.Pendente)
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

            convite.Estado = EstadoPedido.Aceite;

            var utilizadorAtual = await _userManager.GetUserAsync(User);

            var nomeUtilizador =
                utilizadorAtual?.Nome ??
                utilizadorAtual?.UserName ??
                "Um utilizador";

            _context.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = convite.Evento.CriadorId,
                Mensagem =
                    $"{nomeUtilizador} aceitou o convite para o evento \"{convite.Evento.Titulo}\".",
                Link = Url.Action(
                    "Details",
                    "Eventos",
                    new { id = convite.EventoId }
                ),
                Lida = false,
                CriadaEm = DateTime.Now
            });

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
                    c.Estado == EstadoPedido.Pendente);

            if (convite == null)
            {
                return NotFound();
            }

            convite.Estado = EstadoPedido.Rejeitado;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Convite rejeitado.";

            return RedirectToAction(nameof(Index));
        }
    }
}