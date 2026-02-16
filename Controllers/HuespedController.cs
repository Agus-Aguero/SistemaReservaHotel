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

        public async Task<IActionResult> Details(string moneda = "ARS")
        {
            var emailLogueado = SesionUsuario.Instancia.Email;
            
            var huesped = await _context.Persona.OfType<Huesped>()
                .Include(h => h.Reservas)           
                    .ThenInclude(r => r.TipoHabitacion) 
                .Include(h => h.Reservas)
                    .ThenInclude(r => r.Habitacion)     
                .FirstOrDefaultAsync(h => h.Email == emailLogueado);

            if (huesped == null) return RedirectToAction("Create");

            // 1. CÁLCULO DE ESTADÍSTICAS (Noches Totales)
            // Sumamos las noches de todas las reservas que no estén canceladas
            ViewBag.TotalNoches = huesped.Reservas
                .Where(r => r.Estado != "Cancelada")
                .Sum(r => {
                    int n = (r.FechaFin - r.FechaInicio).Days;
                    return n <= 0 ? 1 : n; // Si es el mismo día, cuenta como 1 noche
                });

            // 2. LÓGICA DEL DECORATOR (ARS / USD)
            IPrecioDisplay display = new PrecioPesosDisplay();
            
            if (moneda == "USD")
            {
                var service = new DolarService();
                // Usamos el decorador para convertir el precio
                decimal cotizacion = await service.ObtenerCotizaciónBlue();
                display = new PrecioDolarDecorator(display, cotizacion);
                ViewBag.Cotizacion = cotizacion;
            }

            ViewBag.Display = display;
            ViewBag.Moneda = moneda;

            return View("Details", huesped); 
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
        public async Task<IActionResult> Create(Huesped model)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") && 
                !SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
            {
                return Forbid(); 
            }
            ModelState.Remove("Usuario");
            ModelState.Remove("Perfil");

            if (ModelState.IsValid)
            {
                var perfilHuesped = await _context.Componente
                    .FirstOrDefaultAsync(c => c.Nombre == "Huesped");

                if (perfilHuesped == null)
                {
                    ModelState.AddModelError("", "Error: El perfil 'Huesped' no existe en la base de datos.");
                    return View(model);
                }

                var nuevoUsuario = new Usuario
                {
                    Email = model.Email,
                    Password = Encriptador.GenerarHash("1234"),
                    Perfil = perfilHuesped,
                    PerfilId = perfilHuesped.IdComponente,
                    PreguntaSeguridad = "Configurada por Admin",
                    RespuestaSeguridad = "1234"
                };

                _context.Usuario.Add(nuevoUsuario);
                _context.Huesped.Add(model);
                
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Huésped registrado. Puede ingresar con su email y clave 1234.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
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

            // VALIDACIÓN DE IDENTIDAD: El huésped solo puede editarse a sí mismo
            bool esStaff = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
            if (!esStaff && huesped.Email != SesionUsuario.Instancia.Email)
            {
                return Forbid(); // O redireccionar con error
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (!esStaff)
                    {
                        // Protegemos el email para que no lo cambie manualmente
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