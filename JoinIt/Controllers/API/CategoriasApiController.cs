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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> GetCategorias()
        {
            var categorias = await _context.Categorias
                .AsNoTracking()
                .OrderBy(c => c.Nome)
                .Select(c => new CategoriaDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    NumeroEventosPublicos = c.Eventos.Count(e => !e.IsPrivado)
                })
                .ToListAsync();

            return Ok(categorias);
        }

        // GET: /api/categorias/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoriaDto>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CategoriaDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    NumeroEventosPublicos = c.Eventos.Count(e => !e.IsPrivado)
                })
                .FirstOrDefaultAsync();

            if (categoria == null)
            {
                return NotFound();
            }

            return Ok(categoria);
        }
    }
}