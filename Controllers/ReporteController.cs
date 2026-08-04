using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Patters;

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
            // Validamos permisos
            if (!SesionUsuario.Instancia.TienePermiso("Ver Reportes"))
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

        [HttpGet]
        // 1. Agregamos el parámetro 'moneda' por defecto en "ARS"
        public IActionResult Recaudacion(DateTime? fechaDesde, DateTime? fechaHasta, string moneda = "ARS")
        {
            // Configuramos las fechas por defecto (El mes actual) si vienen vacías
            DateTime desde = fechaDesde ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime hasta = fechaHasta ?? DateTime.Now.Date;

            // Guardamos las variables en el ViewBag para el HTML
            ViewBag.FechaDesde = desde.ToString("yyyy-MM-dd");
            ViewBag.FechaHasta = hasta.ToString("yyyy-MM-dd");
            ViewBag.MonedaActual = moneda;

            // Armamos la consulta base filtrando solo los aprobados
            var query = _context.Cobro.Where(c => c.Estado == "Aprobado").AsQueryable();

            // Aplicamos el filtro de fechas
            query = query.Where(c => c.FechaCobro >= desde && c.FechaCobro < hasta.AddDays(1));

            if (moneda == "ARS")
            {
                // Usamos IsNullOrEmpty para atrapar los NULL y también los strings vacíos ("") de la migración
                query = query.Where(c => c.MonedaPago == "ARS" || string.IsNullOrEmpty(c.MonedaPago));
            }
            else
            {
                // Si buscamos dólares, traemos estrictamente los "USD"
                query = query.Where(c => c.MonedaPago == moneda);
            }

            // Obtenemos el detalle completo para la tabla (antes de agrupar)
            ViewBag.DetalleCobros = query
                .Include(c => c.Reserva)          
                    .ThenInclude(r => r.Huesped)
                .OrderByDescending(c => c.FechaCobro)
                .ToList();

            // 3. NUEVO: Agrupamos y sumamos dependiendo de la moneda
            List<RecaudacionViewModel> datosRecaudacion;

            if (moneda == "USD")
            {
                datosRecaudacion = query
                    .GroupBy(c => c.MetodoPago)
                    .Select(grupo => new RecaudacionViewModel
                    {
                        MetodoPago = grupo.Key,
                        // Si es USD, sumamos la columna de dólares (usamos ?? 0 por si hay nulos viejos)
                        TotalRecaudado = grupo.Sum(c => c.MontoEnDolares ?? 0), 
                        CantidadOperaciones = grupo.Count()
                    })
                    .OrderByDescending(r => r.TotalRecaudado)
                    .ToList();
            }
            else
            {
                datosRecaudacion = query
                    .GroupBy(c => c.MetodoPago)
                    .Select(grupo => new RecaudacionViewModel
                    {
                        MetodoPago = grupo.Key,
                        // Si es ARS, sumamos el MontoTotal tradicional
                        TotalRecaudado = grupo.Sum(c => c.MontoTotal),
                        CantidadOperaciones = grupo.Count()
                    })
                    .OrderByDescending(r => r.TotalRecaudado)
                    .ToList();
            }

            // Calculamos el total general para el pie de página
            ViewBag.TotalGeneral = datosRecaudacion.Sum(r => r.TotalRecaudado);

            return View(datosRecaudacion);
        }
    }
}