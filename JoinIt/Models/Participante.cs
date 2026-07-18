using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinIt.Models
{
    public class Participante
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key to ApplicationUser
        [Required]
        [Display(Name = "Utilizador")]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // Foreign key to Evento
        [Required]
        [Display(Name = "Evento")]
        public int EventoId { get; set; }

        [ForeignKey(nameof(EventoId))]
        public Evento Evento { get; set; } = null!;

        // Data de entrada do participante no evento
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data de Entrada")]
        public DateTime DataEntrada { get; set; } = DateTime.Now;

        // Organizador do evento?
        [Display(Name = "Organizador")]
        public bool IsOrganizador { get; set; }

    }
}
