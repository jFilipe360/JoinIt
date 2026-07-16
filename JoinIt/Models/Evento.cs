using JoinIt.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace JoinIt.Models
{
    public class Evento
    {
        // Primary key
        public int Id { get; set; }

        // Titulo do evento
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        // Descrição do evento
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(1000)]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        // Data e hora do evento
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Data do Evento")]
        public DateTime DataHora { get; set; }

        //Coordenadas geográfica da latitude e longitude do evento
        [Required]
        [Range(-90, 90, ErrorMessage = "A latitude deve estar entre -90 e 90.")]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "A longitude deve estar entre -180 e 180.")]
        public double Longitude { get; set; }

        // Indica se o evento é privado ou público
        [Display(Name = "Evento Privado?")]
        public bool IsPrivado { get; set; }

        // Número máximo de participantes do evento
        [Required]
        [Range(1, 100000, ErrorMessage = "O número máximo de participantes deve ser superior a 0.")]
        [Display(Name = "Máximo de Participantes")]
        public int NumMaxParticipantes { get; set; }

        // Estado do evento (Para breve, A decorrer, Terminado, Cancelado)
        [Required]
        public EstadoEvento Estado { get; set; }

        // Foreign key para o criador do evento
        [Required]
        [Display(Name = "Criador do Evento")]
        public string CriadorId { get; set; } = string.Empty;
        [ValidateNever]
        public ApplicationUser Criador { get; set; } = null!;

        // Foreign key para a categoria do evento
        [Required]
        [Display(Name = "Categoria")]
        public int CategoriaId { get; set; }
        [ValidateNever]
        public Categoria Categoria { get; set; } = null!;

        [ValidateNever]
        public ICollection<Participante> Participantes { get; set; } = new List<Participante>();

        [ValidateNever]
        public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    }
}
