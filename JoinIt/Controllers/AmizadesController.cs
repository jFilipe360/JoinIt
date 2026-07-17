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
    public class AmizadesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AmizadesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

            var amizades = await _context.Amizades
                .AsNoTracking()
                .Include(a => a.PedidoPor)
                .Include(a => a.PedidoA)
                .Where(a =>
                    a.PedidoPorId == userId ||
                    a.PedidoAId == userId)
                .OrderByDescending(a => a.DataPedido)
                .ToListAsync();

            ViewBag.UserId = userId;

            return View(amizades);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(string id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (id == userId)
            {
                return RedirectToAction(
                    "Details",
                    "Perfis",
                    new { id }
                );
            }

            var destinatarioExiste = await _context.Users
                .AnyAsync(u => u.Id == id);

            if (!destinatarioExiste)
            {
                return NotFound();
            }

            var amizadeExistente = await _context.Amizades
                .FirstOrDefaultAsync(a =>
                    (a.PedidoPorId == userId && a.PedidoAId == id) ||
                    (a.PedidoPorId == id && a.PedidoAId == userId)
                );

            if (amizadeExistente != null)
            {
                if (amizadeExistente.Estado ==
                    JoinIt.Enums.EstadoAmizade.Rejeitado)
                {
                    amizadeExistente.PedidoPorId = userId;
                    amizadeExistente.PedidoAId = id;
                    amizadeExistente.Estado =
                        JoinIt.Enums.EstadoAmizade.Pendente;
                    amizadeExistente.DataPedido = DateTime.Now;

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(
                    "Details",
                    "Perfis",
                    new { id }
                );
            }

            var amizade = new Amizade
            {
                PedidoPorId = userId,
                PedidoAId = id,
                Estado = JoinIt.Enums.EstadoAmizade.Pendente,
                DataPedido = DateTime.Now
            };

            _context.Amizades.Add(amizade);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Pedido de amizade enviado.";

            return RedirectToAction(
                "Details",
                "Perfis",
                new { id }
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

            var amizade = await _context.Amizades
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.PedidoAId == userId &&
                    a.Estado == EstadoAmizade.Pendente);

            if (amizade == null)
            {
                return NotFound();
            }

            amizade.Estado = EstadoAmizade.Rejeitado;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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

            var amizade = await _context.Amizades
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.PedidoAId == userId &&
                    a.Estado == EstadoAmizade.Pendente);

            if (amizade == null)
            {
                return NotFound();
            }

            amizade.Estado = EstadoAmizade.Aceite;

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Pedido de amizade aceite.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var amizade = await _context.Amizades
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.PedidoPorId == userId &&
                    a.Estado == EstadoAmizade.Pendente
                );

            if (amizade == null)
            {
                return NotFound();
            }

            _context.Amizades.Remove(amizade);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverAmigo(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var amizade = await _context.Amizades
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Estado == EstadoAmizade.Aceite &&
                    (
                        a.PedidoPorId == userId ||
                        a.PedidoAId == userId
                    )
                );

            if (amizade == null)
            {
                return NotFound();
            }

            _context.Amizades.Remove(amizade);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Amigo removido.";

            return RedirectToAction(nameof(Index));
        }

    }
}
