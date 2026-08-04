using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaReserva.Models
{
    public class AuditoriaReserva
    {
        [Key]
        public int IdAuditoria { get; set; }
        
        public int IdReserva { get; set; }
        
        [Required]
        public string EmailUsuario { get; set; } // Quién hizo el cambio
        
        public DateTime FechaHora { get; set; } = DateTime.Now;
        
        [Required]
        public string TipoOperacion { get; set; } // "CREACIÓN", "MODIFICACIÓN", "ELIMINACIÓN"
        
        // Guardamos un JSON o un string descriptivo con el estado anterior
        public string ValoresOriginales { get; set; } 
        
        // Guardamos cómo quedó después de la acción
        public string ValoresNuevos { get; set; } 
    }
}