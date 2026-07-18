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

        public NotificacoesController(
            ApplicationDbContext context,
            UserManager<Models.ApplicationUser> userManager)
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

            var notificacoes = await _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId)
                .OrderByDescending(n => n.CriadaEm)
                .ToListAsync();

            return View(notificacoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoLida(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

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

            if (!string.IsNullOrWhiteSpace(notificacao.Link) &&
                Url.IsLocalUrl(notificacao.Link))
            {
                return LocalRedirect(notificacao.Link);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasComoLidas()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

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

            TempData["Sucesso"] =
                "Todas as notificações foram marcadas como lidas.";

            return RedirectToAction(nameof(Index));
        }

        //Testar notificações
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CriarTeste()
        //{
        //    var userId = _userManager.GetUserId(User);

        //    if (userId == null)
        //    {
        //        return Challenge();
        //    }

        //    var notificacao = new Notificacao
        //    {
        //        UtilizadorId = userId,
        //        Mensagem = "Esta é uma notificação de teste.",
        //        Link = Url.Action("Index", "Eventos"),
        //        Lida = false,
        //        CriadaEm = DateTime.Now
        //    };

        //    _context.Notificacoes.Add(notificacao);
        //    await _context.SaveChangesAsync();

        //    TempData["Sucesso"] = "Notificação de teste criada.";

        //    return RedirectToAction(nameof(Index));
        //}
    }
}