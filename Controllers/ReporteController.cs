using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models; // Ajusta el namespace según tu proyecto
using SistemaReserva.Patters; // Para SesionUsuario

namespace SistemaReserva.Controllers
{
    public class ReportesController : Controller
    {
        private readonly SistemaReservaContext _context;

        public ReportesController(SistemaReservaContext context)
        {
            _context = context;
        }

        // 1. La Vista Principal con los filtros
        public IActionResult Index()
        {
            // Validamos permisos (Solo Admin o Gerencia)
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // 2. La API que devuelve los datos al gráfico (llamada por AJAX)
        [HttpGet]
        public async Task<IActionResult> DatosReservas(DateTime? inicio, DateTime? fin)
        {
            // Si no eligen fechas, ponemos el último mes por defecto
            if (!inicio.HasValue) inicio = DateTime.Today.AddDays(-30);
            if (!fin.HasValue) fin = DateTime.Today;

            var datos = await _context.Reserva
                .Include(r => r.TipoHabitacion) // Importante incluir el Tipo
                .Where(r => r.FechaInicio >= inicio && r.FechaInicio <= fin) // Filtro de fechas
                .GroupBy(r => r.TipoHabitacion.Nombre) // Agrupamos por nombre (ej: "Twin", "Suite")
                .Select(g => new { 
                    Etiqueta = g.Key, 
                    Valor = g.Count() 
                })
                .ToListAsync();

            return Json(datos);
        }
    }
}