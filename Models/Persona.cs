using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaReserva.Models
{
   public abstract class Persona
    {
        [Key]
        public int IdPersona { get; set; }
        
        [Required]
        public string Nombre { get; set; }
        
        [Required]
        public string Apellido { get; set; }
        
        [Required]
        public DateOnly FechaNacimiento { get; set; }
        
        [Required, EmailAddress]
        public string Email { get; set; }
        
        public string? Telefono { get; set; }

        public string? Genero { get; set; }
        
        public string? Ciudad { get; set; }
        
        public string? Nacionalidad { get; set; }

    }

}