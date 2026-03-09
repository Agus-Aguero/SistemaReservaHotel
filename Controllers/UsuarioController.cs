using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad;
using SistemaReserva.Patters;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaReserva.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly SistemaReservaContext _context;

        public UsuarioController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Usuario/Index
        public async Task<IActionResult> Index()
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
                return RedirectToAction("Index", "Home");

            // Traemos todos los usuarios incluyendo los grupos que tienen asignados actualmente
            var usuarios = await _context.Usuario
                .Include(u => u.Grupos)
                .ToListAsync();

            return View(usuarios);
        }

        // GET: Usuario/Edit/5 (Acá asignamos los grupos)
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
                return RedirectToAction("Index", "Home");

            if (id == null) return NotFound();

            var usuario = await _context.Usuario
                .Include(u => u.Grupos)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null) return NotFound();

            // Mandamos a la vista todos los Grupos (Familias) disponibles para armar los checkboxes
            ViewBag.GruposDisponibles = await _context.Componente.OfType<Familia>().ToListAsync();
            
            return View(usuario);
        }

        // POST: Usuario/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int[] gruposSeleccionados)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
                return RedirectToAction("Index", "Home");

            var usuario = await _context.Usuario
                .Include(u => u.Grupos)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null) return NotFound();

            // 1. Limpiamos los grupos que tenía asignados antes
            usuario.Grupos.Clear();

            // 2. Buscamos los nuevos grupos tildados y se los agregamos
            if (gruposSeleccionados != null && gruposSeleccionados.Length > 0)
            {
                var familias = await _context.Componente.OfType<Familia>()
                    .Where(f => gruposSeleccionados.Contains(f.IdComponente))
                    .ToListAsync();

                foreach (var familia in familias)
                {
                    usuario.Grupos.Add(familia);
                }
            }

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Accesos del usuario actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Usuario/ResetearClave/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetearClave(int id)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
                return RedirectToAction("Index", "Home");

            var usuario = await _context.Usuario.FindAsync(id);
            if (usuario == null) return NotFound();

            // Forzamos la contraseña a "1234" hasheada
            usuario.Password = Encriptador.GenerarHash("1234");
            
            // Opcional: Podés resetear también la pregunta de seguridad para forzar al usuario a configurarla
            usuario.PreguntaSeguridad = "Configurada por Admin";
            usuario.RespuestaSeguridad = "1234";

            _context.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"La clave del usuario {usuario.Email} ha sido reseteada a '1234'.";
            return RedirectToAction(nameof(Index));
        }
    }
}