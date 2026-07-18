using Humanizer;
using JoinIt.Data;
using JoinIt.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Controllers.Api
{
    [ApiController]
    [Route("api/categorias")]
    [AllowAnonymous]
    public class CategoriasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriasApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/categorias
        // Devolve todas as categorias ordenadas alfabeticamente
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> GetCategorias()
        {
            var categorias = await _context.Categorias

                // Como os dados são apenas para leitura,
                // não é necessário o Entity Framework acompanhar alterações.
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                // Converte cada categoria para um DTO,
                // evitando devolver diretamente a entidade da base de dados.
                .Select(c => new CategoriaDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    // Conta apenas os eventos públicos associados à categoria.
                    NumeroEventosPublicos = c.Eventos.Count(e => !e.IsPrivado)
                })
                .ToListAsync();

            return Ok(categorias);
        }

        // GET: /api/categorias/5
        // Devolve uma categoria específica através do seu id
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoriaDto>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                // Filtra a categoria pelo ID recebido no endereço.
                .Where(c => c.Id == id)
                .Select(c => new CategoriaDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    NumeroEventosPublicos = c.Eventos.Count(e => !e.IsPrivado)
                })
                .FirstOrDefaultAsync();

            // Caso não exista nenhuma categoria com esse ID, é devolvido o código HTTP 404.
            if (categoria == null)
            {
                return NotFound();
            }

            return Ok(categoria);
        }
    }
}