using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaReserva.Models
{
    public class HistorialReserva
    {
        [Key]
        public int IdHistorial { get; set; }
        
        [Required]
        public int IdReserva { get; set; }
        
        [Required]
        public DateTime FechaHora { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Usuario { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Accion { get; set; }
        
        [MaxLength(255)]
        public string Detalle { get; set; }

        // Relación con la tabla Reserva
        [ForeignKey("IdReserva")]
        public virtual Reserva Reserva { get; set; }
    }
}