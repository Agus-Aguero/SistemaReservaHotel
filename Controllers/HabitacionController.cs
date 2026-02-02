using Microsoft.AspNetCore.Mvc;
using SistemaReserva.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaReserva.Controllers
{
    public class HabitacionController : Controller
    {
        private readonly SistemaReservaContext _context;

        public HabitacionController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Habitacion/Create (Muestra el formulario vacío)
       public IActionResult Create()
        {
            // Esta línea busca los tipos en la base de datos y los prepara para el HTML
            ViewBag.IdTipoHabitacion = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre");
            return View();
        }

        // POST: Habitacion/Create (Recibe los datos del HTML y guarda en SQL)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdHabitacion,Numero,Disponible,IdTipoHabitacion")] Habitacion habitacion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(habitacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(habitacion);
        }

        public async Task<IActionResult> Index()
        {
            // El .Include(h => h.Tipo) es el que hace la unión en SQL
            var habitaciones = await _context.Habitacion
                .Include(h => h.Tipo) 
                .ToListAsync();
                
            return View(habitaciones);
        }

     // GET: Habitacion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var habitacion = await _context.Habitacion.FindAsync(id);
            if (habitacion == null) return NotFound();

            // Cargamos los tipos y seleccionamos el que ya tiene la habitación
            ViewBag.IdTipoHabitacion = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", habitacion.IdTipoHabitacion);
            
            return View(habitacion);
        }

        // POST: Habitacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdHabitacion,Numero,Disponible,IdTipoHabitacion")] Habitacion habitacion)
        {
            if (id != habitacion.IdHabitacion) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(habitacion); // Marca el objeto como modificado
                    await _context.SaveChangesAsync(); // C# guarda los cambios en SQL
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Habitacion.Any(e => e.IdHabitacion == habitacion.IdHabitacion))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(habitacion);
        }

        // GET: Habitacion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
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
            var habitacion = await _context.Habitacion.FindAsync(id);
            if (habitacion != null)
            {
                _context.Habitacion.Remove(habitacion);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}