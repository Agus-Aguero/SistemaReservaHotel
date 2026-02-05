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

        public async Task<IActionResult> Details()
        {
            var emailLogueado = SesionUsuario.Instancia.Email;
            
            var huesped = await _context.Persona.OfType<Huesped>()
                .Include(h => h.Reservas)           // IMPORTANTE: Para ver el historial
                    .ThenInclude(r => r.TipoHabitacion) // Para ver el nombre de la categoría
                .Include(h => h.Reservas)
                    .ThenInclude(r => r.Habitacion)     // Para ver el número de habitación
                .FirstOrDefaultAsync(h => h.Email == emailLogueado);

            if (huesped == null) return RedirectToAction("Create");

            return View("Details", huesped); // Le decimos que use la vista Details.cshtml
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
                return RedirectToAction("Index", "Home");
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
        public async Task<IActionResult> Edit(int id, Huesped huesped)
        {
            if (id != huesped.IdPersona) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Si NO es admin, nos aseguramos de que no haya hackeado el HTML para cambiar el email
                    if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
                    {
                        huesped.Email = SesionUsuario.Instancia.Email;
                    }

                    _context.Update(huesped);
                    await _context.SaveChangesAsync();
                    
                    // REDIRECCIÓN INTELIGENTE
                    if (SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["Success"] = "Perfil actualizado con éxito.";
                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Manejo de errores...
                }
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