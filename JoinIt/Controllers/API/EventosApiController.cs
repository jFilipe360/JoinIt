using JoinIt.Data;
using JoinIt.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Controllers.Api
{
    [ApiController]
    [Route("api/eventos")]
    [AllowAnonymous]
    public class EventosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/eventos
        [HttpGet]
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
    }
}