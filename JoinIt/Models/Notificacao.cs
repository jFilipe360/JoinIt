using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace JoinIt.Models
{
    public class Notificacao
    {
        public int Id { get; set; }

        [Required]
        public string UtilizadorId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser Utilizador { get; set; } = null!;

        [Required]
        [StringLength(250)]
        public string Mensagem { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Link { get; set; }

        public bool Lida { get; set; } = false;

        public DateTime CriadaEm { get; set; } = DateTime.Now;
    }
}