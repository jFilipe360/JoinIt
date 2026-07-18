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
        // Devolve todos os eventos públicos
        public async Task<ActionResult<IEnumerable<EventoDto>>> GetEventos()
        {
            var eventos = await _context.Eventos
                .AsNoTracking()
                .Where(e => !e.IsPrivado)
                .OrderBy(e => e.DataHora)
                // Converte as entidades em DTOs para controlar
                // os dados enviados na resposta.
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
        // Devolve um evento público específico pelo seu ID
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

            // O evento pode não existir ou ser privado.
            if (evento == null)
            {
                return NotFound();
            }

            return Ok(evento);
        }

        // POST: /api/eventos
        // Cria um novo evento associado ao utilizador autenticado
        [HttpPost]
        public async Task<ActionResult<EventoDto>> CriarEvento(
            CriarEventoDto dto)
        {
            var utilizador = await _userManager.GetUserAsync(User);

            if (utilizador == null)
            {
                return Unauthorized();
            }

            // Um evento não pode ser criado com uma data passada
            if (dto.DataHora <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(dto.DataHora),
                    "A data do evento deve ser futura.");

                return ValidationProblem(ModelState);
            }

            // Confirma que a categoria recebida no DTO existe
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

            // Converte os dados recebidos no DTO numa entidade Evento
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

            // Prepara o DTO devolvido ao cliente após a criação
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

            // Devolve HTTP 201 e o endereço do novo recurso
            return CreatedAtAction(
                nameof(GetEvento),
                new { id = evento.Id },
                resultado);
        }

        // PUT: /api/eventos/5
        // Atualiza um evento existente
        [HttpPut("{id:int}")]
        public async Task<IActionResult> AtualizarEvento(int id,CriarEventoDto dto)
        {
            var utilizadorId = _userManager.GetUserId(User);

            if (utilizadorId == null)
            {
                return Unauthorized();
            }

            // Os participantes são carregados para validar o novo limite máximo do evento
            var evento = await _context.Eventos
                .Include(e => e.Participantes)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            // Apenas o criador do evento ou um administrador pode alterar os seus dados
            var podeEditar =
                evento.CriadorId == utilizadorId ||
                User.IsInRole("Admin");

            if (!podeEditar)
            {
                return Forbid();
            }

            if (dto.DataHora <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(dto.DataHora),"A data do evento deve ser futura.");

                return ValidationProblem(ModelState);
            }

            var categoriaExiste = await _context.Categorias
                .AnyAsync(c => c.Id == dto.CategoriaId);

            if (!categoriaExiste)
            {
                ModelState.AddModelError(nameof(dto.CategoriaId),"A categoria selecionada não existe.");

                return ValidationProblem(ModelState);
            }

            var numeroParticipantes = evento.Participantes.Count;

            // O limite não pode ficar abaixo do número de participantes que já estão inscritos
            if (dto.NumeroMaximoParticipantes < numeroParticipantes)
            {
                ModelState.AddModelError(nameof(dto.NumeroMaximoParticipantes),$"O evento já tem {numeroParticipantes} participantes.");

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

            // HTTP 204 indica que a atualização foi realizada e que não existe conteúdo para devolver
            return NoContent();
        }

        // DELETE: /api/eventos/5
        // Cancela um evento existente
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

            // Apenas o criador ou um administrador pode cancelar o evento
            var podeEliminar =
                evento.CriadorId == utilizadorId ||
                User.IsInRole("Admin");

            if (!podeEliminar)
            {
                return Forbid();
            }

            // Se já estiver cancelado, a operação continua a ser considerada bem-sucedida.
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