using System.ComponentModel.DataAnnotations;

namespace JoinIt.DTOs
{
    //Classe DTO para representar os detalhes de um evento
    public class EventoDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(100,ErrorMessage = "O título não pode ultrapassar 100 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(1000,ErrorMessage = "A descrição não pode ultrapassar 1000 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data do evento é obrigatória.")]
        public DateTime DataHora { get; set; }

        [Range(-90,90,ErrorMessage = "A latitude deve estar entre -90 e 90.")]
        public double Latitude { get; set; }

        [Range(-180,180,ErrorMessage = "A longitude deve estar entre -180 e 180.")]
        public double Longitude { get; set; }

        [Range(1,int.MaxValue,ErrorMessage = "Seleciona uma categoria válida.")]
        public string Categoria { get; set; } = string.Empty;

        [Range(1,int.MaxValue,ErrorMessage = "O evento deve permitir pelo menos um participante.")]
        public int NumeroParticipantes { get; set; }

        public int NumeroMaximoParticipantes { get; set; }
    }
}