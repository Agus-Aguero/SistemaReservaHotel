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

    // ESTE ES EL NUEVO MÉTODO
    public async Task<IActionResult> Habitaciones()
    {
        // Traemos todos los tipos de habitación para mostrarlos
        var tipos = await _context.TipoHabitacion.ToListAsync();
        return View(tipos);
    }
}
