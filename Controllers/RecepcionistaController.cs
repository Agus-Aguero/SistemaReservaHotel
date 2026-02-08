using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Patters;


namespace SistemaReserva.Controllers
{
    public class RecepcionistaController : Controller
    {
        private readonly SistemaReservaContext _context;

        public RecepcionistaController(SistemaReservaContext context)
        {
            _context = context;
        }

        // LISTADO: Solo para Admin
        public async Task<IActionResult> Index()
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
                return RedirectToAction("Index", "Home");

            return View(await _context.Recepcionista.ToListAsync());
        }

        // CREATE: Solo para Admin
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Recepcionista model, string password)
        {
            // Limpiamos objetos que no vienen del formulario para que pase el IsValid
            ModelState.Remove("Usuario");
            ModelState.Remove("Perfil");

            if (ModelState.IsValid)
            {
                // 1. Buscamos el perfil para asignarle los permisos de Recepcionista
                var perfilRecepcionista = await _context.Componente
                    .FirstOrDefaultAsync(c => c.Nombre == "Recepcionista");

                // 2. Creamos el Usuario
                var nuevoUsuario = new Usuario
                {
                    Email = model.Email,
                    Password = Encriptador.GenerarHash(password),
                    Perfil = perfilRecepcionista,
                    PreguntaSeguridad = "Configurada por Admin",
                    RespuestaSeguridad = "1234"
                };

                _context.Usuario.Add(nuevoUsuario);
                _context.Recepcionista.Add(model);
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // EDIT: El Admin edita a cualquiera, el Recepcionista a sí mismo
        public async Task<IActionResult> Edit(int? id)
        {
            var emailLogueado = SesionUsuario.Instancia.Email;
            var esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            var recepcionista = await _context.Recepcionista.FindAsync(id);
            if (recepcionista == null) return NotFound();

            // Seguridad: Si no es Admin y el email no coincide, no puede editar
            if (!esAdmin && recepcionista.Email != emailLogueado) return Forbid();

            return View(recepcionista);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Recepcionista model)
        {
            if (id != model.IdPersona) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Recuperamos el registro original para proteger campos sensibles
                    var original = await _context.Recepcionista.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.IdPersona == id);

                    if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
                    {
                        // Si NO es admin, forzamos que el Legajo y Email no cambien
                        model.Legajo = original.Legajo;
                        model.Email = original.Email;
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), "Home");
                }
                catch (DbUpdateConcurrencyException) { /* ... */ }
            }
            return View(model);
        }

    }
}