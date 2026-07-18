using JoinIt.Data;
using JoinIt.DTOs;
using JoinIt.Enums;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Controllers.Api
{
    [ApiController]
    [Route("api/eventos")]
    [Authorize]
    public class EventosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventosApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /api/eventos
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<EventoDto>>> GetEventos()
        {
            var eventos = await _context.Eventos
                .AsNoTracking()
                .Where(e => !e.IsPrivado)
                .OrderBy(e => e.DataHora)
                .Select(e => new EventoDto
                {
                    Id = e.Id,
                    Titulo = e.Titulo,
                    Descricao = e.Descricao,
                    DataHora = e.DataHora,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    Categoria = e.Categoria.Nome,
                    NumeroParticipantes = e.Participantes.Count(),
                    NumeroMaximoParticipantes = e.NumMaxParticipantes
                })
                .ToListAsync();

            return Ok(eventos);
        }

        // GET: /api/eventos/5
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<EventoDto>> GetEvento(int id)
        {
            var evento = await _context.Eventos
                .AsNoTracking()
                .Where(e =>
                    e.Id == id &&
                    !e.IsPrivado)
                .Select(e => new EventoDto
                {
                    Id = e.Id,
                    Titulo = e.Titulo,
                    Descricao = e.Descricao,
                    DataHora = e.DataHora,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    Categoria = e.Categoria.Nome,
                    NumeroParticipantes = e.Participantes.Count(),
                    NumeroMaximoParticipantes = e.NumMaxParticipantes
                })
                .FirstOrDefaultAsync();

            if (evento == null)
            {
                return NotFound();
            }

            return Ok(evento);
        }

        // POST: /api/eventos
        [HttpPost]
        public async Task<ActionResult<EventoDto>> CriarEvento(
            CriarEventoDto dto)
        {
            var utilizador = await _userManager.GetUserAsync(User);

            if (utilizador == null)
            {
                return Unauthorized();
            }

            if (dto.DataHora <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(dto.DataHora),
                    "A data do evento deve ser futura.");

                return ValidationProblem(ModelState);
            }

            var categoria = await _context.Categorias
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CategoriaId);

            if (categoria == null)
            {
                ModelState.AddModelError(
                    nameof(dto.CategoriaId),
                    "A categoria selecionada não existe.");

                return ValidationProblem(ModelState);
            }

            var evento = new Evento
            {
                Titulo = dto.Titulo.Trim(),
                Descricao = dto.Descricao.Trim(),
                DataHora = dto.DataHora,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CategoriaId = dto.CategoriaId,
                NumMaxParticipantes = dto.NumeroMaximoParticipantes,
                IsPrivado = dto.IsPrivado,
                CriadorId = utilizador.Id,
                Estado = EstadoEvento.ParaBreve
            };

            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();

            var resultado = new EventoDto
            {
                Id = evento.Id,
                Titulo = evento.Titulo,
                Descricao = evento.Descricao,
                DataHora = evento.DataHora,
                Latitude = evento.Latitude,
                Longitude = evento.Longitude,
                Categoria = categoria.Nome,
                NumeroParticipantes = 0,
                NumeroMaximoParticipantes = evento.NumMaxParticipantes
            };

            return CreatedAtAction(
                nameof(GetEvento),
                new { id = evento.Id },
                resultado);
        }

        // PUT: /api/eventos/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarEvento(
            int id,
            CriarEventoDto dto)
        {
            var utilizadorId = _userManager.GetUserId(User);

            if (utilizadorId == null)
            {
                return Unauthorized();
            }

            var evento = await _context.Eventos
                .Include(e => e.Participantes)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            var podeEditar =
                evento.CriadorId == utilizadorId ||
                User.IsInRole("Admin");

            if (!podeEditar)
            {
                return Forbid();
            }

            if (dto.DataHora <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(dto.DataHora),
                    "A data do evento deve ser futura.");

                return ValidationProblem(ModelState);
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == dto.CategoriaId);

            if (!categoriaExiste)
            {
                ModelState.AddModelError(
                    nameof(dto.CategoriaId),
                    "A categoria selecionada não existe.");

                return ValidationProblem(ModelState);
            }

            var numeroParticipantes = evento.Participantes.Count;

            if (dto.NumeroMaximoParticipantes < numeroParticipantes)
            {
                ModelState.AddModelError(
                    nameof(dto.NumeroMaximoParticipantes),
                    $"O evento já tem {numeroParticipantes} participantes.");

                return ValidationProblem(ModelState);
            }

            evento.Titulo = dto.Titulo.Trim();
            evento.Descricao = dto.Descricao.Trim();
            evento.DataHora = dto.DataHora;
            evento.Latitude = dto.Latitude;
            evento.Longitude = dto.Longitude;
            evento.CategoriaId = dto.CategoriaId;
            evento.NumMaxParticipantes = dto.NumeroMaximoParticipantes;
            evento.IsPrivado = dto.IsPrivado;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: /api/eventos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> CancelarEvento(int id)
        {
            var utilizadorId = _userManager.GetUserId(User);

            if (utilizadorId == null)
            {
                return Unauthorized();
            }

            var evento = await _context.Eventos
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            var podeEliminar =
                evento.CriadorId == utilizadorId ||
                User.IsInRole("Admin");

            if (!podeEliminar)
            {
                return Forbid();
            }

            if (evento.Estado == EstadoEvento.Cancelado)
            {
                return NoContent();
            }

            evento.Estado = EstadoEvento.Cancelado;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}