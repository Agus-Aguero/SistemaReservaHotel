using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaReserva.Models.Seguridad;

namespace SistemaReserva.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int? PerfilId { get; set; }
        [ForeignKey("PerfilId")]
        public virtual Componente? Perfil { get; set; }
        public string PreguntaSeguridad { get; set; }
        public string RespuestaSeguridad { get; set; } 
    }
}