using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinIt.Enums
{
    //Lista de estados possíveis para um evento
    public enum EstadoEvento
    {
        [Display(Name = "Para Breve")]
        ParaBreve,

        [Display(Name = "A Decorrer")]
        ADecorrer,

        Terminado,

        Cancelado
    }
}
