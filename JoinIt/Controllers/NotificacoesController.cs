using JoinIt.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JoinIt.Models;

namespace JoinIt.Controllers
{
    [Authorize]
    public class NotificacoesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificacoesController(ApplicationDbContext context,UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Lista todas as notificações do utilizador atual, ordenadas pela data de criação (mais recentes primeiro).
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var notificacoes = await _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId)
                .OrderByDescending(n => n.CriadaEm)
                .ToListAsync();

            return View(notificacoes);
        }

        // Marca uma notificação específica como lida e redireciona para o link associado, se houver.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoLida(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            // Procura a notificação apenas entre as notificações pertencentes ao utilizador autenticado
            var notificacao = await _context.Notificacoes
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UtilizadorId == userId);

            if (notificacao == null)
            {
                return NotFound();
            }

            notificacao.Lida = true;

            await _context.SaveChangesAsync();

            // Só permite redirecionamentos para endereços internos da aplicação
            if (!string.IsNullOrWhiteSpace(notificacao.Link) && Url.IsLocalUrl(notificacao.Link))
            {
                return LocalRedirect(notificacao.Link);
            }

            return RedirectToAction(nameof(Index));
        }

        // Marca todas as notificações do utilizador atual como lidas.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasComoLidas()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            // Obtém apenas as notificações ainda não lidas do utilizador atual
            var notificacoes = await _context.Notificacoes
                .Where(n =>
                    n.UtilizadorId == userId &&
                    !n.Lida)
                .ToListAsync();

            foreach (var notificacao in notificacoes)
            {
                notificacao.Lida = true;
            }

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Todas as notificações foram marcadas como lidas.";

            return RedirectToAction(nameof(Index));
        }
    }
}