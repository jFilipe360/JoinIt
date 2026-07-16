
using JoinIt.Data;
using JoinIt.Enums;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JoinIt.Controllers
{
    [Authorize]
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: EVENTOS
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var eventos = await _context.Eventos
                .AsNoTracking()
                .Include(e => e.Categoria)
                .Include(e => e.Criador)
                .Include(e => e.Participantes)
                .Where(e =>
                    !e.IsPrivado ||
                    e.CriadorId == userId ||
                    e.Participantes.Any(p => p.UserId == userId))
                .OrderBy(e => e.DataHora)
                .ToListAsync();

            return View(eventos);
        }

        // GET: EVENTOS/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .AsNoTracking()
                .Include(e => e.Categoria)
                .Include(e => e.Criador)
                .Include(e => e.Participantes)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            if (evento.IsPrivado)
            {
                var temAcesso =
                    evento.CriadorId == userId ||
                    evento.Participantes.Any(p => p.UserId == userId);

                if (!temAcesso)
                    return Forbid();
            }

            return View(evento);
        }

        // GET: EVENTOS/Create
        public IActionResult Create()
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nome");
            return View();
        }

        // POST: EVENTOS/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titulo,Descricao,DataHora,Latitude,Longitude,IsPrivado,NumMaxParticipantes,CategoriaId")] Evento evento)
        {
            ModelState.Remove(nameof(Evento.CriadorId));

            if (evento.Latitude == 0 && evento.Longitude == 0)
            {
                ModelState.AddModelError(
                    nameof(Evento.Latitude),
                    "Seleciona uma localização no mapa."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nome", evento.CategoriaId);
                return View(evento);
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            evento.CriadorId = userId;
            evento.Estado = EstadoEvento.ParaBreve;

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();

            _context.Participantes.Add(new Participante
            {
                EventoId = evento.Id,
                UserId = userId,
                DataEntrada = DateTime.Now,
                IsOrganizador = true
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: EVENTOS/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            if (evento.CriadorId != userId)
                return Forbid();


            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "Id", "Nome", evento.CategoriaId);

            return View(evento);
        }

        // POST: EVENTOS/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id,[Bind("Id,Titulo,Descricao,DataHora,Latitude,Longitude,IsPrivado,NumMaxParticipantes,CategoriaId")]Evento evento)
        {
            if (id == null || id != evento.Id)
            {
                return NotFound();
            }

            var eventoOriginal = await _context.Eventos
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventoOriginal == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            if (eventoOriginal.CriadorId != userId)
            {
                return Forbid();
            }

            ModelState.Remove(nameof(Evento.CriadorId));

            if (evento.Latitude == 0 && evento.Longitude == 0)
            {
                ModelState.AddModelError(
                    nameof(Evento.Latitude),
                    "Seleciona uma localização no mapa."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewData["CategoriaId"] = new SelectList(
                    await _context.Categorias
                        .OrderBy(c => c.Nome)
                        .ToListAsync(),
                    "Id",
                    "Nome",
                    evento.CategoriaId
                );

                return View(evento);
            }

            eventoOriginal.Titulo = evento.Titulo;
            eventoOriginal.Descricao = evento.Descricao;
            eventoOriginal.DataHora = evento.DataHora;
            eventoOriginal.Latitude = evento.Latitude;
            eventoOriginal.Longitude = evento.Longitude;
            eventoOriginal.IsPrivado = evento.IsPrivado;
            eventoOriginal.NumMaxParticipantes = evento.NumMaxParticipantes;
            eventoOriginal.CategoriaId = evento.CategoriaId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventoExists(evento.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(
                nameof(Details),
                new { id = evento.Id }
            );
        }

        // GET: EVENTOS/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evento = await _context.Eventos
                .Include(e => e.Categoria)
                .Include(e => e.Participantes)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            if (evento.CriadorId != userId)
                return Forbid();

            return View(evento);
        }

        // POST: EVENTOS/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            if (evento.CriadorId != userId)
                return Forbid();

            _context.Eventos.Remove(evento);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventoExists(int? id)
        {
            return _context.Eventos.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Participar(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var evento = await _context.Eventos.FindAsync(id);

            if (evento == null)
                return NotFound();

            // O criador não pode inscrever-se
            if (evento.CriadorId == userId)
                return RedirectToAction(nameof(Details), new { id });

            // Verifica se já participa
            bool jaParticipa = await _context.Participantes
                .AnyAsync(p => p.EventoId == id && p.UserId == userId);

            if (jaParticipa)
                return RedirectToAction(nameof(Details), new { id });

            // Conta participantes
            int totalParticipantes = await _context.Participantes
                .CountAsync(p => p.EventoId == id);

            if (totalParticipantes >= evento.NumMaxParticipantes)
                return RedirectToAction(nameof(Details), new { id });

            _context.Participantes.Add(new Participante
            {
                EventoId = id,
                UserId = userId,
                DataEntrada = DateTime.Now,
                IsOrganizador = false
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sair(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var participante = await _context.Participantes
                .FirstOrDefaultAsync(p => p.EventoId == id && p.UserId == userId);

            if (participante == null)
                return RedirectToAction(nameof(Details), new { id });

            // O organizador não pode sair do próprio evento
            if (participante.IsOrganizador)
                return RedirectToAction(nameof(Details), new { id });

            _context.Participantes.Remove(participante);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Mapa()
        {
            var eventos = await _context.Eventos
                .AsNoTracking()
                .Include(e => e.Categoria)
                .Where(e => !e.IsPrivado)
                .Where(e =>
                    e.Latitude >= -90 &&
                    e.Latitude <= 90 &&
                    e.Longitude >= -180 &&
                    e.Longitude <= 180)
                .Where(e => e.Latitude != 0 || e.Longitude != 0)
                .OrderBy(e => e.DataHora)
                .ToListAsync();

            return View(eventos);
        }
    }
}
