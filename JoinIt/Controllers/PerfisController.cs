using JoinIt.Data;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Controllers
{
    [Authorize]
    public class PerfisController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PerfisController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? pesquisa)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var utilizadoresQuery = _context.Users
                .AsNoTracking()
                .Where(u => u.Id != userId);

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                pesquisa = pesquisa.Trim();

                utilizadoresQuery = utilizadoresQuery.Where(u =>
                    u.Nome.Contains(pesquisa) ||
                    (u.UserName != null && u.UserName.Contains(pesquisa))
                );
            }

            var utilizadores = await utilizadoresQuery
                .OrderBy(u => u.Nome)
                .ToListAsync();

            ViewData["Pesquisa"] = pesquisa;

            return View(utilizadores);
        }

        public async Task<IActionResult> Details(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var utilizador = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utilizador == null)
            {
                return NotFound();
            }

            var utilizadorAtualId = _userManager.GetUserId(User);

            if (utilizadorAtualId == null)
            {
                return Challenge();
            }

            var eventosVisiveis = _context.Eventos
                .AsNoTracking()
                .Include(e => e.Categoria)
                .Include(e => e.Participantes)
                .Where(e =>
                    !e.IsPrivado ||
                    e.CriadorId == utilizadorAtualId ||
                    e.Participantes.Any(p =>
                        p.UserId == utilizadorAtualId
                    )
                );

            var eventosCriados = await eventosVisiveis
                .Where(e => e.CriadorId == utilizador.Id)
                .OrderBy(e => e.DataHora)
                .ToListAsync();

            var eventosParticipa = await eventosVisiveis
                .Where(e =>
                    e.CriadorId != utilizador.Id &&
                    e.Participantes.Any(p =>
                        p.UserId == utilizador.Id
                    )
                )
                .OrderBy(e => e.DataHora)
                .ToListAsync();

            var amizade = await _context.Amizades
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    (a.PedidoPorId == utilizadorAtualId &&
                     a.PedidoAId == utilizador.Id)
                    ||
                    (a.PedidoPorId == utilizador.Id &&
                     a.PedidoAId == utilizadorAtualId)
            );


            ViewBag.EventosCriados = eventosCriados;
            ViewBag.EventosParticipa = eventosParticipa;
            ViewBag.SouDono = utilizadorAtualId == utilizador.Id;
            ViewBag.Amizade = amizade;

            return View(utilizador);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var utilizador = await _userManager.GetUserAsync(User);

            if (utilizador == null)
            {
                return Challenge();
            }

            return View(utilizador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string nome,
            string? fotoPerfil)
        {
            var utilizador = await _userManager.GetUserAsync(User);

            if (utilizador == null)
            {
                return Challenge();
            }

            nome = nome?.Trim() ?? string.Empty;

            fotoPerfil = string.IsNullOrWhiteSpace(fotoPerfil)
                ? null
                : fotoPerfil.Trim();

            if (string.IsNullOrWhiteSpace(nome))
            {
                ModelState.AddModelError(
                    nameof(ApplicationUser.Nome),
                    "O nome é obrigatório."
                );
            }
            else if (nome.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(ApplicationUser.Nome),
                    "O nome não pode ter mais de 100 caracteres."
                );
            }

            if (fotoPerfil != null)
            {
                var urlValida =
                    Uri.TryCreate(
                        fotoPerfil,
                        UriKind.Absolute,
                        out var uri
                    ) &&
                    (uri.Scheme == Uri.UriSchemeHttp ||
                     uri.Scheme == Uri.UriSchemeHttps);

                if (!urlValida)
                {
                    ModelState.AddModelError(
                        nameof(ApplicationUser.FotoPerfil),
                        "Introduz um endereço de imagem válido."
                    );
                }
            }

            utilizador.Nome = nome;
            utilizador.FotoPerfil = fotoPerfil;

            if (!ModelState.IsValid)
            {
                return View(utilizador);
            }

            var resultado = await _userManager.UpdateAsync(utilizador);

            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        erro.Description
                    );
                }

                return View(utilizador);
            }

            return RedirectToAction(
                nameof(Details),
                new { id = utilizador.Id }
            );
        }



        public IActionResult MeuPerfil()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            return RedirectToAction(
                nameof(Details),
                new { id = userId }
            );
        }
    }
}