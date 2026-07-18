using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JoinIt.Models
{
    public class ApplicationUser : IdentityUser
    {
        //Nome do utilizador
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        //Foto de perfil do utilizador
        [Display(Name = "Foto de Perfil")]
        public string? FotoPerfil { get; set; }

        //Data em que o utilizador foi criado
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data de Criação")]
        public DateTime CriadoEm { get; set; } = DateTime.Now;

        public ICollection<Evento> EventosCriados { get; set; } = new List<Evento>();

        public ICollection<Participante> EventosParticipados { get; set; } = new List<Participante>();

        public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();

        public ICollection<Amizade> PedidosEnviados { get; set; } = new List<Amizade>();

        public ICollection<Amizade> PedidosRecebidos { get; set; } = new List<Amizade>();

        public ICollection<ConviteEvento> ConvitesRecebidos { get; set; } = new List<ConviteEvento>();

        public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();
    }
}
