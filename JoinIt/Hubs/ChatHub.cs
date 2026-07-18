using JoinIt.Data;
using JoinIt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JoinIt.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task EntrarNoEvento(int eventoId)
        {
            var userId = ObterUserId();

            var podeAceder = await PodeAcederAoChat(
                eventoId,
                userId
            );

            if (!podeAceder)
            {
                throw new HubException(
                    "Não tens autorização para aceder ao chat deste evento."
                );
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                ObterNomeGrupo(eventoId)
            );
        }

        public async Task EnviarMensagem(
            int eventoId,
            string texto)
        {
            var userId = ObterUserId();

            var podeAceder = await PodeAcederAoChat(eventoId,userId);

            if (!podeAceder)
            {
                throw new HubException("Não tens autorização para enviar mensagens neste evento." );
            }

            texto = texto?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
            {
                throw new HubException("A mensagem não pode estar vazia.");
            }

            if (texto.Length > 500)
            {
                throw new HubException("A mensagem não pode ter mais de 500 caracteres.");
            }

            var utilizador = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Nome,
                    u.UserName
                })
                .FirstOrDefaultAsync();

            if (utilizador == null)
            {
                throw new HubException(
                    "Não foi possível identificar o utilizador."
                );
            }

            var mensagem = new Mensagem
            {
                EventoId = eventoId,
                UserId = userId,
                Texto = texto,
                DataEnvio = DateTime.Now
            };

            _context.Mensagens.Add(mensagem);
            await _context.SaveChangesAsync();

            var nomeUtilizador =
                !string.IsNullOrWhiteSpace(utilizador.Nome)
                    ? utilizador.Nome
                    : utilizador.UserName ?? "Utilizador";

            await Clients
                .Group(ObterNomeGrupo(eventoId))
                .SendAsync(
                    "ReceberMensagem",
                    new
                    {
                        id = mensagem.Id,
                        texto = mensagem.Texto,
                        utilizador = nomeUtilizador,
                        dataEnvio = mensagem.DataEnvio
                            .ToString("HH:mm")
                    }
                );
        }

        private string ObterUserId()
        {
            var userId = _userManager.GetUserId(Context.User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException(
                    "É necessário iniciar sessão."
                );
            }

            return userId;
        }

        private async Task<bool> PodeAcederAoChat(
            int eventoId,
            string userId)
        {
            return await _context.Eventos
                .AsNoTracking()
                .AnyAsync(e =>
                    e.Id == eventoId &&
                    (
                        e.CriadorId == userId ||
                        e.Participantes.Any(p =>
                            p.UserId == userId
                        )
                    )
                );
        }

        private static string ObterNomeGrupo(int eventoId)
        {
            return $"evento-{eventoId}";
        }
    }
}