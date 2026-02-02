using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SistemaReserva.Models
{
    public class TipoHabitacion
    {
        [Key]
        public int IdTipoHabitacion { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Tipo")]
        public string Nombre { get; set; } // Ejemplo: Suite, Estándar

        [Required]
        [Range(1, 10, ErrorMessage = "La capacidad debe ser entre 1 y 10")]
        public int Capacidad { get; set; }

        [Display(Name = "Descripción de Camas")]
        public string DescripcionCamas { get; set; } // Ejemplo: 1 Cama Matrimonial

        [Required]
        [Display(Name = "Precio por Noche")]
        public decimal PrecioBase { get; set; }
    }
}