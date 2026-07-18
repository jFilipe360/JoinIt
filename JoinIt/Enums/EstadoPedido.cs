using System.ComponentModel.DataAnnotations;

namespace JoinIt.Enums
{
    // Lista de estados possíveis para um pedido de participação em um evento ou pedido de amizade
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