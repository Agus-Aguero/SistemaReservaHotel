using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;

namespace SistemaReserva.Controllers;

public class HomeController : Controller
{
    private readonly SistemaReservaContext _context;

        public HomeController(SistemaReservaContext context)
        {
            _context = context;
        }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> Habitaciones(string moneda = "ARS")
    {
        var tiposHabitacion = await _context.TipoHabitacion.ToListAsync();

        SistemaReserva.Patters.IPrecioDisplay display = new SistemaReserva.Patters.PrecioPesosDisplay();

        if (moneda == "USD")
        {
            var service = new DolarService();
            decimal cotizacion = await service.ObtenerCotizaciónBlue();
            display = new SistemaReserva.Patters.PrecioDolarDecorator(display, cotizacion);
            ViewBag.Cotizacion = cotizacion; // Opcional, por si querés mostrar el valor del dólar
        }

        ViewBag.Display = display;
        ViewBag.Moneda = moneda;

        return View(tiposHabitacion);
    }
}
