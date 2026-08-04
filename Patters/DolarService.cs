using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public class DolarService
{
    // 1. Variables estáticas: viven en la memoria RAM del servidor web
    private static decimal _cotizacionCache = 0;
    private static DateTime _ultimaConsulta = DateTime.MinValue;
    public async Task<decimal> ObtenerCotizaciónBlue()
    {
        // 2. Si ya tenemos el precio y pasaron menos de 30 minutos, respondemos al instante
        if (_cotizacionCache > 0 && (DateTime.Now - _ultimaConsulta).TotalMinutes < 30)
        {
            return _cotizacionCache; 
        }
        // 3. Si no hay caché o pasaron los 30 min, vamos a internet
        try 
        {
            using var client = new HttpClient();
            
            client.Timeout = TimeSpan.FromSeconds(3); 

            var response = await client.GetFromJsonAsync<JsonElement>("https://dolarapi.com/v1/dolares/blue");
            decimal cotizacionObtenida = response.GetProperty("venta").GetDecimal();

            // Guardamos en memoria para la próxima vez
            _cotizacionCache = cotizacionObtenida;
            _ultimaConsulta = DateTime.Now;

            return cotizacionObtenida;
        } 
        catch 
        {
            // Devolvemos lo que teníamos en caché (si hay algo), sino el valor de respaldo.
            return _cotizacionCache > 0 ? _cotizacionCache : 1420;
        }
    }
}