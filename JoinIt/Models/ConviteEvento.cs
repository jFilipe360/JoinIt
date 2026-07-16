using JoinIt.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace JoinIt.Models
{
    public class ConviteEvento
    {
        public int Id { get; set; }

        [Required]
        public int EventoId { get; set; }

        [ValidateNever]
        public Evento Evento { get; set; } = null!;

        [Required]
        public string UtilizadorId { get; set; } = null!;

        [ValidateNever]
        public ApplicationUser Utilizador { get; set; } = null!;

        public EstadoConvite Estado { get; set; } = EstadoConvite.Pendente;

        public DateTime DataConvite { get; set; } = DateTime.Now;
    }
}
