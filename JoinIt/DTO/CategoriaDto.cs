namespace JoinIt.DTOs
{
    // Classe DTO (Data Transfer Object) para representar uma categoria de eventos.
    public class CategoriaDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public int NumeroEventosPublicos { get; set; }
    }
}