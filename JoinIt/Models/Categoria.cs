using System.ComponentModel.DataAnnotations;

namespace JoinIt.Models
{
    public class Categoria
    {
        // Primary key
        public int Id { get; set; }

        // Nome da categoria
        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [StringLength(50)]
        [Display(Name = "Categoria")]
        public string Nome { get; set; } = string.Empty;

        public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    }
}
