using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaReserva.Models
{
    public class AuditoriaSesion
    {
        [Key]
        public int IdRegistro { get; set; }
        
        [Required]
        public string EmailUsuario { get; set; }
        
        public DateTime FechaHora { get; set; } = DateTime.Now;
        
        [Required]
        public string TipoEvento { get; set; } // "LOGIN EXITOSO", "LOGIN FALLIDO", "LOGOUT"
        
        public string Detalles { get; set; } // Ej: "Contraseña incorrecta", "Cierre manual", etc.
    }
}