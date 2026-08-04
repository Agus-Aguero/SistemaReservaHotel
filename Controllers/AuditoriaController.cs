using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Patters; 

namespace SistemaReserva.Controllers
{
    public class AuditoriaController : Controller
    {
        private readonly SistemaReservaContext _context;

        public AuditoriaController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Auditoria/Sesiones
        public async Task<IActionResult> Sesiones(int pagina = 1)
        {
            // Seguridad: Solo perfiles gerenciales o admins
            if (!SesionUsuario.Instancia.TienePermiso("Ver Auditoria"))
            {
                TempData["Error"] = "No tienes permisos para ver las auditorías.";
                return RedirectToAction("Index", "Home");
            }

            int registrosPorPagina = 15; // Podés cambiar este número según prefieras

            // 1. Armamos la consulta base ordenada (sin ejecutarla todavía)
            var query = _context.AuditoriaSesion.OrderByDescending(s => s.FechaHora);

            // 2. Calculamos el total de páginas
            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

            // 3. Aplicamos la paginación y traemos solo los de la página actual
            var historialSesiones = await query
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            // 4. Mandamos la info a la vista para dibujar los botones
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            return View(historialSesiones);
        }

        [HttpGet]
        public async Task<IActionResult> Reservas(int pagina = 1)
        {
            int registrosPorPagina = 20;

            // 1. Armamos la consulta base
            var query = _context.AuditoriaReserva.OrderByDescending(a => a.FechaHora);

            // 2. Calculamos el total de páginas
            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)registrosPorPagina);

            // 3. Aplicamos la paginación con Skip y Take
            var auditoriasPaginadas = await query
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            // 4. Mandamos la info a la vista
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = totalPaginas;

            return View(auditoriasPaginadas);
        }
    }
}