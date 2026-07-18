using System.ComponentModel.DataAnnotations;

namespace JoinIt.Enums
{
    public enum EstadoPedido
    {
        [Display(Name = "Pendente")]
        Pendente = 0,

        [Display(Name = "Aceite")]
        Aceite = 1,

        [Display(Name = "Rejeitado")]
        Rejeitado = 2
    }
}