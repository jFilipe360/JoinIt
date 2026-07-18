using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinIt.Models
{
    public class Mensagem
    {
        // Primary key
        public int Id { get; set; }

        // Texto da mensagem
        [Required(ErrorMessage = "A mensagem não pode estar vazia.")]
        [StringLength(500)]
        [Display(Name = "Mensagem")]
        public string Texto { get; set; } = string.Empty;

        // Data de envio da mensagem
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Enviada em")]
        public DateTime DataEnvio { get; set; } = DateTime.Now;

        // Foreign key para o user que enviou a mensagem
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        // Foreign key para o evento ao qual a mensagem pertence
        [Required]
        public int EventoId { get; set; }

        [ForeignKey(nameof(EventoId))]
        public Evento Evento { get; set; } = null!;
    }
}
