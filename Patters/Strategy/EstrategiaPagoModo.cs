using SistemaReserva.Models;
using System;
using System.Collections.Generic;

namespace SistemaReserva.Patters.Strategy
{
    public class EstrategiaPagoModo : IEstrategiaPago
    {
        public ResultadoPago ProcesarPago(Reserva reserva, decimal monto, Dictionary<string, string> datosPago)
        {

            var cobro = new Cobro
            {
                IdReserva = reserva.IdReserva,
                MontoTotal = monto,
                FechaCobro = DateTime.Now,
                MetodoPago = "Billetera Virtual - MODO",
                Estado = "Aprobado",
                NumeroComprobante = $"Transacción QR #{new Random().Next(100000, 999999)}"
            };

            return new ResultadoPago
            {
                Exito = true,
                Mensaje = "El pago mediante código QR de MODO se procesó correctamente.",
                CobroGenerado = cobro
            };
        }
    }
}