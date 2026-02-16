using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;

namespace SistemaReserva.Controllers
{
    public class TipoHabitacionController : Controller
    {
        private readonly SistemaReservaContext _context;

        public TipoHabitacionController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: TipoHabitacion
        public async Task<IActionResult> Index()
        {
            // Traemos todos los tipos de habitación de SQL Server
            var tipos = await _context.TipoHabitacion.ToListAsync();
            return View(tipos);
        }

        // GET: TipoHabitacion/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TipoHabitacion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoHabitacion tipoHabitacion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tipoHabitacion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tipoHabitacion);
        }

        // GET: TipoHabitacion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tipoHabitacion = await _context.TipoHabitacion.FindAsync(id);
            if (tipoHabitacion == null) return NotFound();
            
            return View(tipoHabitacion);
        }

        // POST: TipoHabitacion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TipoHabitacion tipoHabitacion)
        {
            if (id != tipoHabitacion.IdTipoHabitacion) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tipoHabitacion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TipoHabitacion.Any(e => e.IdTipoHabitacion == id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tipoHabitacion);
        }

        // GET: TipoHabitacion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tipoHabitacion = await _context.TipoHabitacion
                .FirstOrDefaultAsync(m => m.IdTipoHabitacion == id);
                
            if (tipoHabitacion == null) return NotFound();

            return View(tipoHabitacion);
        }

        // POST: TipoHabitacion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipoHabitacion = await _context.TipoHabitacion.FindAsync(id);
            if (tipoHabitacion != null)
            {
                _context.TipoHabitacion.Remove(tipoHabitacion);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}