using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Patters;

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

        // GET: Huesped/Create
        public IActionResult Create()
        {
            // Solo permitimos si es el usuario logueado quien crea su perfil
            var nuevoHuesped = new Huesped
            {
                Email = SesionUsuario.Instancia.Email
            };

            return View(nuevoHuesped);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Apellido,Genero,Provincia,Pais,FechaNacimiento,Email,Telefono,Ciudad,Nacionalidad")] Huesped huesped)
        {
            // Forzamos el email de la sesión por seguridad
            huesped.Email = SesionUsuario.Instancia.Email;

            if (ModelState.IsValid)
            {
                _context.Add(huesped);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Reserva"); // Lo mandamos a reservar
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