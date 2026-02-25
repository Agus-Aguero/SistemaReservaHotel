using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad;
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
        public async Task<IActionResult> Create(Recepcionista model) 
        {
            ModelState.Remove("Usuario");
            ModelState.Remove("Perfil");

            if (ModelState.IsValid)
            {
                var perfilRecepcionista = await _context.Componente
                    .FirstOrDefaultAsync(c => c.Nombre == "Recepcion"); 

                if (perfilRecepcionista == null)
                {
                    ModelState.AddModelError("", "Error: El perfil 'Recepcionista' no existe en la base de datos.");
                    return View(model);
                }

                var nuevoUsuario = new Usuario
                {
                    Email = model.Email.Trim(), // FIX: Limpiar email
                    Password = Encriptador.GenerarHash("1234"), // Generamos el hash limpio
                    Grupos = new List<Familia> { (Familia)perfilRecepcionista },
                    PreguntaSeguridad = "Configurada por Admin",
                    RespuestaSeguridad = "1234"
                };

                _context.Usuario.Add(nuevoUsuario);
                _context.Recepcionista.Add(model); 
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Recepcionista registrado. Puede ingresar con su email y clave 1234.";
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

        [HttpPost]
        public async Task<IActionResult> ToggleEstado(int id)
        {
            var staff = await _context.Recepcionista.FindAsync(id);
            if (staff != null)
            {
                staff.Activo = !staff.Activo; // Cambia de true a false o viceversa
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

    }
}