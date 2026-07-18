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

        // Apresenta os pedidos e amizades do utilizador autenticado. 
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var amizades = await _context.Amizades
                .AsNoTracking()
                // Carrega os dados dos dois utilizadores envolvidos 
                .Include(a => a.PedidoPor)
                .Include(a => a.PedidoA)
                // Obtém pedidos enviados e recebidos pelo utilizador atual
                .Where(a =>
                    a.PedidoPorId == userId ||
                    a.PedidoAId == userId)
                .OrderByDescending(a => a.DataPedido)
                .ToListAsync();

            // Permite à view identificar qual dos utilizadores é o atual
            ViewBag.UserId = userId;

            return View(amizades);
        }

        //Enviar um pedido de amizade para outro utilizador
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

            // Impede que um utilizador envie um pedido a si próprio
            if (id == userId)
            {
                return RedirectToAction("Details","Perfis",new { id });
            }

            var destinatarioExiste = await _context.Users
                .AnyAsync(u => u.Id == id);

            if (!destinatarioExiste)
            {
                return NotFound();
            }

            var utilizadorAtual = await _userManager.GetUserAsync(User);

            var nomeUtilizador =
                utilizadorAtual?.Nome ??
                utilizadorAtual?.UserName ??
                "Um utilizador";

            // Procura uma relação existente em qualquer uma das direções
            var amizadeExistente = await _context.Amizades
                .FirstOrDefaultAsync(a =>
                    (a.PedidoPorId == userId && a.PedidoAId == id) ||
                    (a.PedidoPorId == id && a.PedidoAId == userId)
                );

            if (amizadeExistente != null)
            {
                // Um pedido anteriormente rejeitado pode ser enviado novamente
                if (amizadeExistente.Estado == EstadoPedido.Rejeitado)
                {
                    amizadeExistente.PedidoPorId = userId;
                    amizadeExistente.PedidoAId = id;
                    amizadeExistente.Estado = EstadoPedido.Pendente;
                    amizadeExistente.DataPedido = DateTime.Now;

                    _context.Notificacoes.Add(new Notificacao
                    {
                        // O destinatário recebe a notificação
                        UtilizadorId = id,
                        Mensagem = $"{nomeUtilizador} enviou-te um pedido de amizade.",
                        Link = Url.Action("Index", "Amizades"),
                        Lida = false,
                        CriadaEm = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    TempData["Sucesso"] =
                        "Pedido de amizade enviado novamente.";
                }
                else
                {
                    TempData["Aviso"] =
                        "Já existe um pedido ou amizade entre estes utilizadores.";
                }

                return RedirectToAction("Details","Perfis",new { id });
            }

            // Cria um novo pedido de amizade pendente
            var amizade = new Amizade
            {
                PedidoPorId = userId,
                PedidoAId = id,
                Estado = EstadoPedido.Pendente,
                DataPedido = DateTime.Now
            };

            _context.Amizades.Add(amizade);

            // Notifica o destinatário do novo pedido
            _context.Notificacoes.Add(new Notificacao
            {
                // Tem de ser 'id', não 'userId'
                UtilizadorId = id,
                Mensagem = $"{nomeUtilizador} enviou-te um pedido de amizade.",
                Link = Url.Action("Index", "Amizades"),
                Lida = false,
                CriadaEm = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Pedido de amizade enviado.";

            return RedirectToAction("Details","Perfis",new { id });
        }

        // Rejeitar um pedido de amizade recebido
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
                    a.Estado == EstadoPedido.Pendente);

            if (amizade == null)
            {
                return NotFound();
            }

            amizade.Estado = EstadoPedido.Rejeitado;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Aceitar um pedido de amizade recebido
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
                    a.Estado == EstadoPedido.Pendente);

            if (amizade == null)
            {
                return NotFound();
            }

            amizade.Estado = EstadoPedido.Aceite;

            var utilizadorAtual = await _userManager.GetUserAsync(User);

            var nomeUtilizador =
                utilizadorAtual?.Nome ??
                utilizadorAtual?.UserName ??
                "Um utilizador";

            // Informa o remetente de que o pedido foi aceite
            _context.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = amizade.PedidoPorId,
                Mensagem = $"{nomeUtilizador} aceitou o teu pedido de amizade.",
                Link = Url.Action("Index", "Amizades"),
                Lida = false,
                CriadaEm = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Pedido de amizade aceite.";

            return RedirectToAction(nameof(Index));
        }

        // Permite ao remetente cancelar um pedido ainda pendente
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
                    a.Estado == EstadoPedido.Pendente
                );

            if (amizade == null)
            {
                return NotFound();
            }

            _context.Amizades.Remove(amizade);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Remove uma amizade aceite, independentemente de quem enviou originalmente o pedido
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
                    a.Estado == EstadoPedido.Aceite &&
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
