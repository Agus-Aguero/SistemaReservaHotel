using System.ComponentModel.DataAnnotations;

public class ForzarCambioClaveViewModel
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$", 
        ErrorMessage = "Debe incluir mayúscula, número y carácter especial.")]
    [Display(Name = "Nueva Contraseña")]
    public string NuevaPassword { get; set; }

    [Required(ErrorMessage = "Debes confirmar la contraseña")]
    [Compare("NuevaPassword", ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar Contraseña")]
    public string ConfirmarPassword { get; set; }
}