
using JoinIt.Data;
using JoinIt.Enums;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? pesquisa, int? categoriaId, string? tipo, bool apenasFuturos = false, bool participo = false, string ordem = "data")
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null;

            var query = _context.Eventos
                .AsNoTracking()
                .Include(e => e.Categoria)
                .Include(e => e.Criador)
                .Include(e => e.Participantes)
                .Include(e => e.Convites)
                .AsQueryable();

            //Controlar a visibilidade de eventos privados com base no utilizador autenticado
            if (userId == null)
            {
                query = query.Where(e => !e.IsPrivado);
            }
            else
            {
                query = query.Where(e =>
                    !e.IsPrivado ||
                    e.CriadorId == userId ||
                    e.Participantes.Any(p => p.UserId == userId) ||
                    e.Convites.Any(c =>
                        c.UtilizadorId == userId &&
                        c.Estado != EstadoPedido.Rejeitado));
            }

            // Esconder eventos passados dos outros utilizadores
            // O criador continua a ver os próprios eventos passados
            // O Admin continua a ver todos
            var agora = DateTime.Now;

            if (!User.IsInRole("Admin"))
            {
                query = query.Where(e =>
                    e.DataHora >= agora ||
                    e.Estado == EstadoEvento.ADecorrer ||
                    (userId != null && e.CriadorId == userId));
            }


            //Pesquisa pelos filtros fornecidos
            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                pesquisa = pesquisa.Trim();

                query = query.Where(e =>
                    e.Titulo.Contains(pesquisa));
            }

            // Filtrar por categoria
            if (categoriaId.HasValue)
            {
                query = query.Where(e =>
                    e.CategoriaId == categoriaId.Value);
            }

            // Filtrar por tipo de evento (público ou privado)
            if (tipo == "publico")
            {
                query = query.Where(e => !e.IsPrivado);
            }
            else if (tipo == "privado")
            {
                query = query.Where(e => e.IsPrivado);
            }


            //Eventos em que o utilizador participa
            if (participo)
            {
                if (userId == null)
                {
                    return Challenge();
                }

                query = query.Where(e =>
                    e.Participantes.Any(p => p.UserId == userId));
            }

            //Ordenação dos eventos
            query = ordem switch
            {
                "data_desc" => query
                    .OrderByDescending(e => e.DataHora),

                "titulo" => query
                    .OrderBy(e => e.Titulo),

                "titulo_desc" => query
                    .OrderByDescending(e => e.Titulo),

                _ => query.OrderBy(e => e.DataHora)
            };

            var eventos = await query.ToListAsync();

            ViewBag.Categorias = new SelectList(
                await _context.Categorias
                    .AsNoTracking()
                    .OrderBy(c => c.Nome)
                    .ToListAsync(),
                "Id",
                "Nome",
                categoriaId
            );

            ViewData["Pesquisa"] = pesquisa;
            ViewData["CategoriaId"] = categoriaId;
            ViewData["Tipo"] = tipo;
            ViewData["ApenasFuturos"] = apenasFuturos;
            ViewData["Participo"] = participo;
            ViewData["Ordem"] = ordem;

            return View(eventos);
        }

        // GET: EVENTOS/Details/5
        [AllowAnonymous]
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
                .Include(e => e.Convites)
                    .ThenInclude(c => c.Utilizador)
                .Include(e => e.Mensagens)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            var userId = User.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null;

            if (userId == null)
            {
                return Challenge();
            }

            if (evento.IsPrivado)
            {
                if (userId == null)
                {
                    return Challenge();
                }

                var temAcesso =
                    evento.CriadorId == userId ||
                    evento.Participantes.Any(p => p.UserId == userId) ||
                    evento.Convites.Any(c =>
                        c.UtilizadorId == userId &&
                        c.Estado != EstadoPedido.Rejeitado);

                if (!temAcesso)
                {
                    return Forbid();
                }
            }

            if (evento.IsPrivado && evento.CriadorId == userId)
            {
                var amizadesAceites = await _context.Amizades
                    .AsNoTracking()
                    .Where(a =>
                        a.Estado == EstadoPedido.Aceite &&
                        (a.PedidoPorId == userId ||
                         a.PedidoAId == userId))
                    .ToListAsync();

                var idsAmigos = amizadesAceites
                    .Select(a =>
                        a.PedidoPorId == userId
                            ? a.PedidoAId
                            : a.PedidoPorId)
                    .ToList();

                var idsParticipantes = evento.Participantes
                    .Select(p => p.UserId)
                    .ToList();

                var idsComConviteAtivo = evento.Convites
                    .Where(c => c.Estado != EstadoPedido.Rejeitado)
                    .Select(c => c.UtilizadorId)
                    .ToList();

                var amigosDisponiveis = await _context.Users
                    .AsNoTracking()
                    .Where(u =>
                        idsAmigos.Contains(u.Id) &&
                        !idsParticipantes.Contains(u.Id) &&
                        !idsComConviteAtivo.Contains(u.Id))
                    .OrderBy(u => u.Nome)
                    .ToListAsync();

                ViewBag.AmigosDisponiveis = amigosDisponiveis;
            }

            ViewBag.PodeUsarChat =
                userId != null &&
                (
                    evento.CriadorId == userId ||
                    evento.Participantes.Any(p => p.UserId == userId)
                );

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

            if (evento.DataHora <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(Evento.DataHora),
                    "A data do evento deve ser futura."
                );
            }

            if (evento.NumMaxParticipantes < 1)
            {
                ModelState.AddModelError(
                    nameof(Evento.NumMaxParticipantes),
                    "O evento deve permitir pelo menos um participante."
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

            TempData["Sucesso"] = "Evento criado com sucesso.";

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

            var numeroParticipantes = await _context.Participantes.CountAsync(p => p.EventoId == evento.Id);

            if (evento.NumMaxParticipantes < numeroParticipantes)
            {
                ModelState.AddModelError(
                    nameof(Evento.NumMaxParticipantes),
                    $"O limite não pode ser inferior aos {numeroParticipantes} participantes atuais."
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

            TempData["Sucesso"] = "Evento atualizado com sucesso.";

            return RedirectToAction(nameof(Details), new { id = evento.Id });
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

            TempData["Sucesso"] = "Evento eliminado com sucesso.";

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

            if (evento.Estado == EstadoEvento.Cancelado || evento.Estado == EstadoEvento.Terminado)
            {
                TempData["Erro"] =
                    "Já não é possível participar neste evento.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            if (evento == null)
                return NotFound();

            if (evento.IsPrivado)
            {
                var temConviteAceite = await _context.ConvitesEventos
                    .AnyAsync(c =>
                        c.EventoId == evento.Id &&
                        c.UtilizadorId == userId &&
                        c.Estado == EstadoPedido.Aceite);

                if (!temConviteAceite)
                {
                    return Forbid();
                }
            }

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

            TempData["Sucesso"] = "Entraste no evento com sucesso.";

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

            TempData["Sucesso"] = "Saíste do evento.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convidar(int id, string utilizadorId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(utilizadorId))
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            var evento = await _context.Eventos
                .Include(e => e.Participantes)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento.Estado == EstadoEvento.Cancelado || evento.Estado == EstadoEvento.Terminado)
            {
                TempData["Erro"] =
                    "Não é possível enviar convites para este evento.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            if (evento == null)
            {
                return NotFound();
            }

            if (evento.CriadorId != userId)
            {
                return Forbid();
            }

            if (!evento.IsPrivado)
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            if (utilizadorId == userId)
            {
                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            var saoAmigos = await _context.Amizades
                .AnyAsync(a =>
                    a.Estado == EstadoPedido.Aceite &&
                    (
                        (a.PedidoPorId == userId &&
                            a.PedidoAId == utilizadorId)
                        ||
                        (a.PedidoPorId == utilizadorId &&
                            a.PedidoAId == userId)
                    ));

            if (!saoAmigos)
            {
                return Forbid();
            }

            var jaParticipa = evento.Participantes
                .Any(p => p.UserId == utilizadorId);

            if (jaParticipa)
            {
                TempData["Aviso"] = "Já participas neste evento.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            var conviteExistente = await _context.ConvitesEventos
                .FirstOrDefaultAsync(c =>
                    c.EventoId == id &&
                    c.UtilizadorId == utilizadorId);

            var conviteEnviado = false;

            if (conviteExistente == null)
            {
                var convite = new ConviteEvento
                {
                    EventoId = id,
                    UtilizadorId = utilizadorId,
                    Estado = EstadoPedido.Pendente,
                    DataConvite = DateTime.Now
                };

                _context.ConvitesEventos.Add(convite);

                conviteEnviado = true;
            }
            else if (conviteExistente.Estado == EstadoPedido.Rejeitado)
            {
                conviteExistente.Estado = EstadoPedido.Pendente;
                conviteExistente.DataConvite = DateTime.Now;

                conviteEnviado = true;
            }

            if (conviteEnviado)
            {
                _context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = utilizadorId,
                    Mensagem =
                        $"Recebeste um convite para o evento \"{evento.Titulo}\".",
                    Link = Url.Action("Index", "Convites"),
                    Lida = false,
                    CriadaEm = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["Sucesso"] = "Convite enviado com sucesso.";
            }
            else
            {
                TempData["Aviso"] =
                    "Este utilizador já possui um convite pendente ou aceite.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id }
            );
        }

        [AllowAnonymous]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarEstado(int id, EstadoEvento novoEstado)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var evento = await _context.Eventos
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
            {
                return NotFound();
            }

            var podeGerir =
                evento.CriadorId == userId ||
                User.IsInRole("Admin");

            if (!podeGerir)
            {
                return Forbid();
            }

            var transicaoValida = novoEstado switch
            {
                EstadoEvento.ADecorrer =>
                    evento.Estado == EstadoEvento.ParaBreve,

                EstadoEvento.Terminado =>
                    evento.Estado == EstadoEvento.ADecorrer,

                EstadoEvento.Cancelado =>
                    evento.Estado == EstadoEvento.ParaBreve ||
                    evento.Estado == EstadoEvento.ADecorrer,

                _ => false
            };

            if (!transicaoValida)
            {
                TempData["Erro"] =
                    "Não é possível alterar o evento para esse estado.";

                return RedirectToAction(
                    nameof(Details),
                    new { id }
                );
            }

            evento.Estado = novoEstado;

            var mensagemNotificacao = novoEstado switch
            {
                EstadoEvento.ADecorrer =>
                    $"O evento \"{evento.Titulo}\" começou.",

                EstadoEvento.Terminado =>
                    $"O evento \"{evento.Titulo}\" terminou.",

                EstadoEvento.Cancelado =>
                    $"O evento \"{evento.Titulo}\" foi cancelado.",

                _ => $"O estado do evento \"{evento.Titulo}\" foi atualizado."
            };

            var destinatarios = await _context.Participantes
                .Where(p =>
                    p.EventoId == evento.Id &&
                    p.UserId != userId)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            var linkEvento = Url.Action(
                "Details",
                "Eventos",
                new { id = evento.Id }
            );

            var notificacoes = destinatarios.Select(utilizadorId =>
                new Notificacao
                {
                    UtilizadorId = utilizadorId,
                    Mensagem = mensagemNotificacao,
                    Link = linkEvento,
                    Lida = false,
                    CriadaEm = DateTime.Now
                });

            _context.Notificacoes.AddRange(notificacoes);

            await _context.SaveChangesAsync();

            TempData["Sucesso"] = novoEstado switch
            {
                EstadoEvento.ADecorrer =>
                    "O evento foi iniciado.",

                EstadoEvento.Terminado =>
                    "O evento foi terminado.",

                EstadoEvento.Cancelado =>
                    "O evento foi cancelado.",

                _ => "Estado atualizado."
            };

            return RedirectToAction(
                nameof(Details),
                new { id }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarMensagem(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var mensagem = await _context.Mensagens
                .Include(m => m.Evento)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mensagem == null)
            {
                return NotFound();
            }

            var podeEliminar =
                mensagem.Evento.CriadorId == userId ||
                User.IsInRole("Admin");

            if (!podeEliminar)
            {
                return Forbid();
            }

            var eventoId = mensagem.EventoId;

            _context.Mensagens.Remove(mensagem);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Mensagem eliminada com sucesso.";

            return RedirectToAction(
                nameof(Details),
                new { id = eventoId }
            );
        }
    }
}
