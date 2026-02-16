using System.ComponentModel.DataAnnotations;

namespace SistemaReserva.Models
{

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña Actual")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nueva Contraseña")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "La pregunta de seguridad es obligatoria")]
        [Display(Name = "Pregunta de Seguridad")]
        public string PreguntaSeguridad { get; set; }

        [Required(ErrorMessage = "La respuesta de seguridad es obligatoria")]
        [Display(Name = "Respuesta de Seguridad")]
        public string RespuestaSeguridad { get; set; }
    }
}