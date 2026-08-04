using SistemaReserva.Models;
using System;
using System.Collections.Generic;

namespace SistemaReserva.Patters.Strategy
{
public class EstrategiaPagoDebito : IEstrategiaPago
{
    public ResultadoPago ProcesarPago(Reserva reserva, decimal monto, Dictionary<string, string> datosPago)
    {
        string numeroTarjeta = datosPago.GetValueOrDefault("NumeroTarjeta", "");
            string cvv = datosPago.GetValueOrDefault("CVV", "");

            // 1. Validaciones básicas simuladas
            if (numeroTarjeta.Length < 15 || numeroTarjeta.Length > 16)
                return new ResultadoPago { Exito = false, Mensaje = "El número de tarjeta es inválido (debe tener 15 o 16 dígitos)." };

            if (cvv.Length < 3 || cvv.Length > 4)
                return new ResultadoPago { Exito = false, Mensaje = "El código de seguridad (CVV) es inválido." };

            // 2. Simulamos la aprobación de la pasarela de pago
            string ultimos4 = numeroTarjeta.Substring(numeroTarjeta.Length - 4);
            string marca = numeroTarjeta.StartsWith("4") ? "Visa" : (numeroTarjeta.StartsWith("5") ? "Mastercard" : "Amex");

        var cobro = new Cobro
        {
            IdReserva = reserva.IdReserva,
            MontoTotal = monto,
            FechaCobro = DateTime.Now,
            MetodoPago = "Tarjeta de Débito",
            Estado = "Aprobado", 
            Ultimos4Digitos = ultimos4,
            MarcaTarjeta = marca
        };

        return new ResultadoPago { Exito = true, Mensaje = "Pago total procesado correctamente.", CobroGenerado = cobro };
    }
}

}