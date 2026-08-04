using System;

namespace SistemaReserva.Utils
{
    // Definimos un Enum para saber exactamente qué tipo de tarjeta es
    public enum TipoTarjeta
    {
        Credito,
        Debito,
        Desconocida
    }

    public static class DetectorTarjeta
    {
        /// Evalúa los primeros 4 dígitos (BIN) para determinar si es Crédito o Débito.
        public static TipoTarjeta IdentificarTipo(string numeroTarjeta)
        {
            // Limpiamos espacios en blanco por si el usuario tipeó "4111 2222..."
            string numeroLimpio = numeroTarjeta?.Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(numeroLimpio) || numeroLimpio.Length < 4)
                return TipoTarjeta.Desconocida;

            // Extraemos los primeros 4 dígitos para nuestra validación
            string bin = numeroLimpio.Substring(0, 4);

            return bin switch
            {
                // ---- VISA ----
                "4111" => TipoTarjeta.Credito,
                "4555" => TipoTarjeta.Debito,
                
                // ---- MASTERCARD ----
                "5100" => TipoTarjeta.Credito,
                "5500" => TipoTarjeta.Debito,
                
                // ---- AMEX  ----
                "3400" => TipoTarjeta.Credito,
                "3700" => TipoTarjeta.Credito,
                
                // Si no coincide con ninguno de nuestros BINs hardcodeados
                _ => TipoTarjeta.Desconocida
            };
        }

        /// <summary>
        /// (Opcional) Un método extra que te puede servir para guardar la marca 
        /// en la base de datos o mostrar un mensaje más personalizado.
        /// </summary>
        public static string IdentificarMarca(string numeroTarjeta)
        {
            string numeroLimpio = numeroTarjeta?.Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(numeroLimpio)) 
                return "Desconocida";
            
            if (numeroLimpio.StartsWith("4")) return "Visa";
            if (numeroLimpio.StartsWith("5")) return "Mastercard";
            if (numeroLimpio.StartsWith("34") || numeroLimpio.StartsWith("37")) return "Amex";
            
            return "Desconocida";
        }
    }
}