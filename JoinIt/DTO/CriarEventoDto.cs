using System.ComponentModel.DataAnnotations;

namespace JoinIt.DTOs
{
    public class CriarEventoDto
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(
            100,
            MinimumLength = 3,
            ErrorMessage = "O título deve ter entre 3 e 100 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(
            1000,
            MinimumLength = 10,
            ErrorMessage = "A descrição deve ter entre 10 e 2000 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data e hora são obrigatórias.")]
        public DateTime DataHora { get; set; }

        [Range(-90, 90, ErrorMessage = "A latitude deve estar entre -90 e 90.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "A longitude deve estar entre -180 e 180.")]
        public double Longitude { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Seleciona uma categoria válida.")]
        public int CategoriaId { get; set; }

        [Range(
            2,
            1000,
            ErrorMessage = "O número máximo de participantes deve estar entre 2 e 1000.")]
        public int NumeroMaximoParticipantes { get; set; }

        public bool IsPrivado { get; set; }
    }
}