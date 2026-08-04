using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaReserva.Models
{
    public class Cobro
    {
        [Key]
        public int IdCobro { get; set; }

        [Required]
        public int IdReserva { get; set; }
        [ForeignKey("IdReserva")]
        public virtual Reserva Reserva { get; set; }

        [Required]
        public decimal MontoTotal { get; set; }

        [Required]
        public DateTime FechaCobro { get; set; }

        [Required]
        public string MetodoPago { get; set; } 

        [Required]
        public string Estado { get; set; } 
        
        // Si pagó con tarjeta, guardamos solo esto
        public string? Ultimos4Digitos { get; set; }
        public string? MarcaTarjeta { get; set; }

        // Si paga por transferencia, guardardamos el número de comprobante
        public string? NumeroComprobante { get; set; }

        // Propiedades para el bimonetarismo
        public string MonedaPago { get; set; } = "ARS";
        public decimal? MontoEnDolares { get; set; }
        public decimal? CotizacionAplicada { get; set; }

    }
}