using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaReserva.Models
{
    public class Reserva
    {
        public virtual Cobro Cobro { get; set; }
        
        [Key]
        public int IdReserva { get; set; }

        // El cliente elige qué TIPO quiere
        [Required]
        [Display(Name = "Tipo de Habitación")]
        public int IdTipoHabitacion { get; set; }
        
        [ForeignKey("IdTipoHabitacion")]
        public virtual TipoHabitacion? TipoHabitacion { get; set; }

        // El número de habitación queda vacío al principio (int? permite nulos)
        [Display(Name = "Habitación Asignada")]
        public int? IdHabitacion { get; set; }
        
        [ForeignKey("IdHabitacion")]
        public virtual Habitacion? Habitacion { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }
        [Required]
        public DateTime FechaFin { get; set; }

        // Unión con Huésped (Persona)
        public int IdPersona { get; set; }
        [ForeignKey("IdPersona")]
        public virtual Huesped? Huesped { get; set; }

        public int IdUsuario { get; set; } // La columna física

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        [Required]
        [Display(Name = "Estado de la Reserva")]
        public string Estado { get; set; } = "Pendiente"; 
        // Valores posibles: "Pendiente", "Activa", "Finalizada", "Cancelada"

        [Display(Name = "Precio Total")]
        [DataType(DataType.Currency)]
        public decimal PrecioTotal { get; set; } = 0;

    }
}