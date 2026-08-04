using SistemaReserva.Models;
using System.Collections.Generic;

namespace SistemaReserva.Patters.Strategy
{
    // Una clase auxiliar para devolver la respuesta del pago
    public class ResultadoPago
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public Cobro CobroGenerado { get; set; }
    }

    public interface IEstrategiaPago
    {
        ResultadoPago ProcesarPago(Reserva reserva, decimal monto, Dictionary<string, string> datosPago);
    }
}