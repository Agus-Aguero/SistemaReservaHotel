using System.ComponentModel.DataAnnotations;

namespace SistemaReserva.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }

        public string Pregunta { get; set; }

        public string Respuesta { get; set; }

        [DataType(DataType.Password)]
        public string NuevaPassword { get; set; }

        public int Paso { get; set; } = 1; 
    }
}