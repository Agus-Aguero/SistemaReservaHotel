using System.Text.Json;

public class DolarService
{
    public async Task<decimal> ObtenerCotizaciónBlue()
    {
        try {
            using var client = new HttpClient();
            var response = await client.GetFromJsonAsync<JsonElement>("https://dolarapi.com/v1/dolares/blue");
            return response.GetProperty("venta").GetDecimal();
        } catch {
            return 1430; // Valor de respaldo 
        }
    }
}