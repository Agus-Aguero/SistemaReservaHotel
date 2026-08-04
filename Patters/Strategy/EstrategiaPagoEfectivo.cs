using System;
using System.Collections.Generic;
using SistemaReserva.Models; 

namespace SistemaReserva.Patters.Strategy
{
    public class EstrategiaPagoEfectivo : IEstrategiaPago 
    {
        // Agregamos el parámetro "decimal monto" que pedía la interfaz
        public ResultadoPago ProcesarPago(Reserva reserva, decimal monto, Dictionary<string, string> datosPago)
        {
            var cobro = new Cobro
            {
                IdReserva = reserva.IdReserva,
                MontoTotal = monto, // Asignamos el monto que viene por parámetro
                FechaCobro = DateTime.Now,
                MetodoPago = "Efectivo",
                Estado = "Aprobado", 
                NumeroComprobante = $"Ticket Efectivo #{new Random().Next(100000, 999999)}" // Un ticket aleatorio como tenés en MODO
            };

            return new ResultadoPago
            {
                Exito = true,
                Mensaje = "Pago en efectivo registrado correctamente en caja.",
                CobroGenerado = cobro
            };
        }
    }
}