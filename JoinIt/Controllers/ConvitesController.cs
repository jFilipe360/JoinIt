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

        public ConvitesController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Apresenta todos os convites recebidos pelo utilizador atual
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var convites = await _context.ConvitesEventos
                .AsNoTracking()
                // Carrega os dados necessários para apresentar a informação completa de cada evento.
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

        // Aceita um convite pendente e adiciona o utilizador à lista de participantes do evento
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
                // Os participantes têm de ser carregados para verificar se o utilizador já participa e se ainda existem vagas
                .Include(c => c.Evento)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.UtilizadorId == userId &&
                    c.Estado == EstadoPedido.Pendente);

            if (convite == null)
            {
                return NotFound();
            }

            var evento = convite.Evento;

            // Eventos cancelados ou terminados já não aceitam inscrições
            if (evento.Estado == EstadoEvento.Cancelado || evento.Estado == EstadoEvento.Terminado)
            {
                TempData["Erro"] = "Este evento já não aceita participantes.";

                return RedirectToAction(nameof(Index));
            }

            var jaParticipa = evento.Participantes
                .Any(p => p.UserId == userId);

            // Só é necessário verificar o número de participantes quando o utilizador ainda não pertence ao evento
            if (!jaParticipa && evento.Participantes.Count >= evento.NumMaxParticipantes)
            {
                TempData["Erro"] = "O evento já atingiu o número máximo de participantes.";

                return RedirectToAction(nameof(Index));
            }

            // Evita criar uma participação duplicada
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

            // Notifica o organizador de que o convite foi aceite
            _context.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = convite.Evento.CriadorId,
                Mensagem = $"{nomeUtilizador} aceitou o convite para o evento \"{convite.Evento.Titulo}\".",
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

        // Rejeita um convite recebido que ainda esteja pendente
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