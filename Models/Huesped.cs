using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaReserva.Models
{
    public class Huesped : Persona
    {
        public Huesped()
        {
            Reservas = new List<Reserva>();
        }

        public string? Provincia { get; set; }
        public string? Pais { get; set; }
        public virtual ICollection<Reserva> Reservas { get; set; }
    }

}