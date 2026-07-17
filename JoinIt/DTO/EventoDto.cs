namespace JoinIt.DTOs
{
    public class EventoDto
    {
        public int Id { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public DateTime DataHora { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public int NumeroParticipantes { get; set; }

        public int NumeroMaximoParticipantes { get; set; }
    }
}