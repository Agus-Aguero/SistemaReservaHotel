using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;

namespace SistemaReserva.Controllers
{
    public class HuespedController : Controller
    {
        private readonly SistemaReservaContext _context;

        public HuespedController(SistemaReservaContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var huespedes = await _context.Persona
                .OfType<Huesped>()
                .ToListAsync();
            return View(huespedes);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huesped = await _context.Persona
                .OfType<Huesped>()
                .Include(p => p.Reservas) // Cargamos la colección de reservas
                    .ThenInclude(r => r.TipoHabitacion) // Cargamos el objeto Tipo dentro de Reserva
                .Include(p => p.Reservas)
                    .ThenInclude(r => r.Habitacion) // Cargamos el objeto Habitacion dentro de Reserva
                .FirstOrDefaultAsync(p => p.IdPersona == id);

            if (huesped == null)
            {
                return NotFound();
            }

            return View(huesped);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Apellido,Genero,Provincia,Pais,FechaNacimiento,Email,Telefono,Ciudad,Nacionalidad")] Huesped huesped)
        {
            if (ModelState.IsValid)
            {
                // IMPORTANTE: Aquí NO debe haber ninguna línea que mencione "huesped.Rol"
                _context.Add(huesped); 
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(huesped);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huesped = await _context.Persona
                .OfType<Huesped>()
                .FirstOrDefaultAsync(p => p.IdPersona == id);

            if (huesped == null)
            {
                return NotFound();
            }
            return View(huesped);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("IdPersona,Nombre,Apellido,Genero,Provincia,Pais,FechaNacimiento,Email,Telefono,Ciudad,Nacionalidad")] Huesped huesped)
        {
            if (id != huesped.IdPersona)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(huesped);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HuespedExists(huesped.IdPersona))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(huesped);
        }

        private bool HuespedExists(int idPersona)
        {
            throw new NotImplementedException();
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huesped = await _context.Persona
                .OfType<Huesped>()
                .FirstOrDefaultAsync(p => p.IdPersona == id);

            if (huesped == null)
            {
                return NotFound();
            }

            return View(huesped);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var huesped = await _context.Persona
                .OfType<Huesped>()
                .FirstOrDefaultAsync(p => p.IdPersona == id);

            if (huesped == null)
            {
                return NotFound();
            }

            _context.Persona.Remove(huesped);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}