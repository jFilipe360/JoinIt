using JoinIt.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoinIt.Models
{
    public class Amizade
    {
        // Primary key
        public int Id { get; set; }

        //User que enviou o pedido de amizade
        [Required]
        public string PedidoPorId { get; set; } = string.Empty;

        [ForeignKey(nameof(PedidoPorId))]
        public ApplicationUser PedidoPor { get; set; } = null!;


        //User que recebeu o pedido de amizade
        [Required]
        public string PedidoAId { get; set; } = string.Empty;

        [ForeignKey(nameof(PedidoAId))]
        public ApplicationUser PedidoA { get; set; } = null!;


        // Estado do pedido de amizade (Pendente, Aceite, Recusado)
        [Required]
        public EstadoPedido Estado { get; set; }

        // Data do pedido de amizade
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DataPedido { get; set; } = DateTime.Now;
    }
}
