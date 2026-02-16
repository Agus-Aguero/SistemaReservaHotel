using System.ComponentModel.DataAnnotations;

namespace SistemaReserva.Models
{
    public class Recepcionista : Persona
    {
        [Required]
        public string Legajo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow; 

        public bool Activo { get; set; } = true;
    }

}