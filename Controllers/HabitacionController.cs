using Microsoft.AspNetCore.Mvc;
using SistemaReserva.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaReserva.Patters; // IMPORTANTE: Agregamos el using para usar la sesión

namespace SistemaReserva.Controllers
{
    public class HabitacionController : Controller
    {
        private readonly SistemaReservaContext _context;

        public HabitacionController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Habitacion/Index
        public async Task<IActionResult> Index()
        {
            bool esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
            bool esRecepcion = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes");
            bool manejaInfraestructura = SesionUsuario.Instancia.TienePermiso("Gestionar Habitaciones");

            if (!esAdmin && !esRecepcion && !manejaInfraestructura)
            {
                TempData["Error"] = "No tienes permisos para acceder a este módulo.";
                return RedirectToAction("Index", "Home");
            }

            var habitaciones = await _context.Habitacion
                .Include(h => h.Tipo) 
                .ToListAsync()
                .ContinueWith(t => t.Result.OrderBy(h => h.Numero));
                
            return View(habitaciones);
        }

        // GET: Habitacion/Create
        public IActionResult Create()
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
            {
                TempData["Error"] = "Acceso denegado. Solo los administradores pueden registrar habitaciones.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IdTipoHabitacion = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre");
            return View();
        }

        // POST: Habitacion/Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdHabitacion,Numero,Disponible,IdTipoHabitacion")] Habitacion habitacion)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction(nameof(Index));

            if (ModelState.IsValid)
            {
                _context.Add(habitacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(habitacion);
        }

        // GET: Habitacion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            bool esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
            bool esRecepcion = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes");
            if (!esAdmin && !esRecepcion) return RedirectToAction("Index", "Home");

            if (id == null) return NotFound();

            var habitacion = await _context.Habitacion.FindAsync(id);
            if (habitacion == null) return NotFound();

            ViewBag.IdTipoHabitacion = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", habitacion.IdTipoHabitacion);
            
            return View(habitacion);
        }

        // POST: Habitacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdHabitacion,Numero,Disponible,IdTipoHabitacion")] Habitacion habitacion)
        {
            bool esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
            bool esRecepcion = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes");
            if (!esAdmin && !esRecepcion) return RedirectToAction("Index", "Home");

            if (id != habitacion.IdHabitacion) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(habitacion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Habitacion.Any(e => e.IdHabitacion == habitacion.IdHabitacion))
                        return NotFound();
                    else
                        throw;
                }
                TempData["Success"] = "Habitación actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(habitacion);
        }

        // GET: Habitacion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
            {
                TempData["Error"] = "Acceso denegado. Solo los administradores pueden eliminar habitaciones.";
                return RedirectToAction(nameof(Index));
            }

            if (id == null) return NotFound();

            var habitacion = await _context.Habitacion
                .Include(h => h.Tipo)
                .FirstOrDefaultAsync(m => m.IdHabitacion == id);

            if (habitacion == null) return NotFound();

            return View(habitacion);
        }

        // POST: Habitacion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction(nameof(Index));

            var habitacion = await _context.Habitacion.FindAsync(id);
            if (habitacion != null)
            {
                _context.Habitacion.Remove(habitacion);
                await _context.SaveChangesAsync();
            }
            TempData["Success"] = "Habitación eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}