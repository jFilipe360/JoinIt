using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoinIt.Enums
{
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
