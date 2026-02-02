using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaReserva.Models
{
    public class Habitacion
    {
        [Key]
        public int IdHabitacion { get; set; }

        [Required]
        [Display(Name = "Número de Habitación")]
        public string Numero { get; set; }

        public bool Disponible { get; set; } = true;

        [Required(ErrorMessage = "Debe seleccionar un tipo de habitación")]
        [Display(Name = "Tipo de Habitación")]
        public int IdTipoHabitacion { get; set; } // Esta es la Clave Foránea (FK)

        [ForeignKey("IdTipoHabitacion")]
        public virtual TipoHabitacion? Tipo { get; set; } // Propiedad de navegación
    }
}