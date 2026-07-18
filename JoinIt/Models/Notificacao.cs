using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JoinIt.Models
{
    public class Notificacao
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key do utilizador
        [Required]
        public string UtilizadorId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser Utilizador { get; set; } = null!;

        // Mensagem da notificação
        [Required]
        [StringLength(250)]
        public string Mensagem { get; set; } = string.Empty;

        // Link para onde a notificação deve redirecionar o utilizador
        [StringLength(300)]
        public string? Link { get; set; }

        // Indica se a notificação já foi lida pelo utilizador
        public bool Lida { get; set; } = false;

        // Data e hora em que a notificação foi criada
        public DateTime CriadaEm { get; set; } = DateTime.Now;
    }
}