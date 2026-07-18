using JoinIt.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace JoinIt.Models
{
    public class ConviteEvento
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key do Evento
        [Required]
        public int EventoId { get; set; }

        [ValidateNever]
        public Evento Evento { get; set; } = null!;

        // Foreign key do Utilizador
        [Required]
        public string UtilizadorId { get; set; } = null!;

        [ValidateNever]
        public ApplicationUser Utilizador { get; set; } = null!;

        // Estado do convite
        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendente;

        // Data do convite
        public DateTime DataConvite { get; set; } = DateTime.Now;
    }
}
