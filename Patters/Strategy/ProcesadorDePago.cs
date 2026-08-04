using SistemaReserva.Models;
using System;
using System.Collections.Generic;

namespace SistemaReserva.Patters.Strategy
{
    public class ProcesadorDePagos
    {
        private IEstrategiaPago _estrategia;

        public void EstablecerEstrategia(IEstrategiaPago estrategia)
        {
            _estrategia = estrategia;
        }

        public ResultadoPago EjecutarCobro(Reserva reserva, decimal monto, Dictionary<string, string> datosPago)
        {
            if (_estrategia == null)
                throw new InvalidOperationException("No se ha seleccionado un método de pago.");

            return _estrategia.ProcesarPago(reserva, monto, datosPago);
        }
    }
}