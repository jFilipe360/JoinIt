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
        private readonly IWebHostEnvironment _environment;

        public PerfisController(ApplicationDbContext context,UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
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
        public async Task<IActionResult> Edit(string nome, string? fotoPerfil, IFormFile? fotografia)
        {
            var utilizador = await _userManager.GetUserAsync(User);

            if (utilizador == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                ModelState.AddModelError(
                    "Nome",
                    "O nome é obrigatório."
                );
            }

            if (fotografia != null && fotografia.Length > 0)
            {
                const long tamanhoMaximo = 2 * 1024 * 1024;

                if (fotografia.Length > tamanhoMaximo)
                {
                    ModelState.AddModelError(
                        "fotografia",
                        "A fotografia não pode ultrapassar 2 MB."
                    );
                }

                var extensao = Path
                    .GetExtension(fotografia.FileName)
                    .ToLowerInvariant();

                var extensoesPermitidas = new[]
                {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

                var tiposPermitidos = new[]
                {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

                if (!extensoesPermitidas.Contains(extensao) ||
                    !tiposPermitidos.Contains(fotografia.ContentType))
                {
                    ModelState.AddModelError(
                        "fotografia",
                        "Seleciona uma imagem JPG, PNG ou WebP."
                    );
                }
            }

            if (!ModelState.IsValid)
            {
                return View(utilizador);
            }

            utilizador.Nome = nome.Trim();

            if (fotografia != null && fotografia.Length > 0)
            {
                var pastaFotografias = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "perfis"
                );

                Directory.CreateDirectory(pastaFotografias);

                var extensao = Path
                    .GetExtension(fotografia.FileName)
                    .ToLowerInvariant();

                var nomeFicheiro = $"{Guid.NewGuid()}{extensao}";

                var caminhoFisico = Path.Combine(
                    pastaFotografias,
                    nomeFicheiro
                );

                await using (var stream = new FileStream(
                    caminhoFisico,
                    FileMode.Create))
                {
                    await fotografia.CopyToAsync(stream);
                }

                // Apagar a fotografia local anterior, quando existir.
                if (!string.IsNullOrWhiteSpace(utilizador.FotoPerfil) &&
                    utilizador.FotoPerfil.StartsWith(
                        "/uploads/perfis/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    var nomeFicheiroAntigo = Path.GetFileName(
                        utilizador.FotoPerfil
                    );

                    var caminhoAntigo = Path.Combine(
                        pastaFotografias,
                        nomeFicheiroAntigo
                    );

                    if (System.IO.File.Exists(caminhoAntigo))
                    {
                        System.IO.File.Delete(caminhoAntigo);
                    }
                }

                utilizador.FotoPerfil =
                    $"/uploads/perfis/{nomeFicheiro}";
            }
            else if (!string.IsNullOrWhiteSpace(fotoPerfil))
            {
                // Mantém também a possibilidade de usar um URL.
                utilizador.FotoPerfil = fotoPerfil.Trim();
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

            TempData["Sucesso"] = "Perfil atualizado com sucesso.";

            return RedirectToAction(nameof(MeuPerfil));
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