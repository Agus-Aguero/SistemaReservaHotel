using SistemaReserva.Models;
using System;
using System.Collections.Generic;

namespace SistemaReserva.Patters.Strategy
{
    public class EstrategiaPagoTransferencia : IEstrategiaPago
    {
        public ResultadoPago ProcesarPago(Reserva reserva, decimal monto, Dictionary<string, string> datosPago)
        {
            var cobro = new Cobro
            {
                IdReserva = reserva.IdReserva,
                MontoTotal = monto,
                FechaCobro = DateTime.Now,
                MetodoPago = "Transferencia Bancaria",
                Estado = "Pendiente de Comprobante"
            };

            string mensajeAlias = "Por favor, transfiera el total al Alias: HOTEL.ARGENTOWER.PESOS o CBU: 0000123456789012345678 y envíe el comprobante por WhatsApp.";

            return new ResultadoPago { 
                Exito = true, 
                Mensaje = mensajeAlias, 
                CobroGenerado = cobro 
            };
        }
    }
}