using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaReserva.Models
{
    public class Empleado : Persona
{
    [Required]
    public string Legajo { get; set; } = string.Empty; // Asegura que siempre tenga un valor

    [Required]
    public string Direccion { get; set; } = string.Empty;

    [Required]
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow; // Inicialización opcional
}

}