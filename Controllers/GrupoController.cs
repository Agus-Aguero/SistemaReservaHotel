using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad;
using SistemaReserva.Patters;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaReserva.Controllers
{
    public class GrupoController : Controller
    {
        private readonly SistemaReservaContext _context;

        public GrupoController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Grupo/Index
        public async Task<IActionResult> Index()
        {
            // SEGURIDAD: Solo el Admin puede gestionar los grupos
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) 
                return RedirectToAction("Index", "Home");

            // Traemos solo los componentes que son de tipo "Familia" (Grupos)
            var grupos = await _context.Componente.OfType<Familia>().ToListAsync();
            return View(grupos);
        }

        // GET: Grupo/Create
        public async Task<IActionResult> Create()
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction("Index", "Home");

            // Pasamos a la vista todas las Patentes (Permisos individuales) para armar los Checkboxes
            ViewBag.PermisosDisponibles = await _context.Componente.OfType<Patente>().ToListAsync();
            return View();
        }

        // POST: Grupo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nombre, int[] permisosSeleccionados)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del grupo es obligatorio.");
                ViewBag.PermisosDisponibles = await _context.Componente.OfType<Patente>().ToListAsync();
                return View();
            }

            var nuevoGrupo = new Familia { Nombre = nombre };

            // PATRÓN COMPOSITE: Agregamos las patentes seleccionadas como hijos del grupo
            if (permisosSeleccionados != null && permisosSeleccionados.Length > 0)
            {
                var patentes = await _context.Componente.OfType<Patente>()
                    .Where(p => permisosSeleccionados.Contains(p.IdComponente))
                    .ToListAsync();

                foreach (var patente in patentes)
                {
                    nuevoGrupo.Agregar(patente);
                }
            }

            _context.Componente.Add(nuevoGrupo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Grupo creado exitosamente con sus permisos.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Grupo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction("Index", "Home");
            if (id == null) return NotFound();

            // Buscamos el grupo incluyendo a sus hijos (permisos que ya tiene asignados)
            var grupo = await _context.Componente.OfType<Familia>()
                .Include(f => f.Hijos)
                .FirstOrDefaultAsync(f => f.IdComponente == id);

            if (grupo == null) return NotFound();

            ViewBag.PermisosDisponibles = await _context.Componente.OfType<Patente>().ToListAsync();
            return View(grupo);
        }

        // POST: Grupo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string nombre, int[] permisosSeleccionados)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction("Index", "Home");

            var grupo = await _context.Componente.OfType<Familia>()
                .Include(f => f.Hijos)
                .FirstOrDefaultAsync(f => f.IdComponente == id);

            if (grupo == null) return NotFound();

            grupo.Nombre = nombre;

            // Limpiamos los permisos viejos del Composite
            grupo.Hijos.Clear();

            // PATRÓN COMPOSITE: Agregamos los nuevos permisos seleccionados
            if (permisosSeleccionados != null && permisosSeleccionados.Length > 0)
            {
                var patentes = await _context.Componente.OfType<Patente>()
                    .Where(p => permisosSeleccionados.Contains(p.IdComponente))
                    .ToListAsync();

                foreach (var patente in patentes)
                {
                    grupo.Agregar(patente);
                }
            }

            _context.Update(grupo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Grupo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Grupo/Delete/5 (Lo hacemos directo por POST para más seguridad)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios")) return RedirectToAction("Index", "Home");

            var grupo = await _context.Componente.OfType<Familia>()
                .Include(f => f.Usuarios) // Verificamos si hay usuarios en este grupo
                .FirstOrDefaultAsync(f => f.IdComponente == id);

            if (grupo != null)
            {
                if (grupo.Usuarios.Any())
                {
                    TempData["Error"] = "No se puede eliminar el grupo porque tiene usuarios asignados. Quita los usuarios primero.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Componente.Remove(grupo);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Grupo eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}