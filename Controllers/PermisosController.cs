using Microsoft.AspNetCore.Mvc;
using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace SistemaReserva.Controllers
{
    public class PermisosController : Controller
    {
        private readonly SistemaReservaContext _context;

        // El constructor es vital para que _context no esté en rojo
        public PermisosController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Permisos/Gestionar/5
        public IActionResult Gestionar(int idUsuario)
        {
            // Buscamos el usuario e incluimos su Perfil (Composite)
            var usuario = _context.Usuario
                .Include(u => u.Perfil)
                    .ThenInclude(p => p.Hijos)
                .FirstOrDefault(u => u.IdUsuario == idUsuario);

            var listaParaVista = new List<string>();
            
            if (usuario?.Perfil != null)
            {
                // Llamada a la función recursiva
                GenerarListaTreeView(usuario.Perfil, listaParaVista, 0);
            }

            return View(listaParaVista);
        }

        // ESTA ES LA FUNCIÓN RECURSIVA QUE PIDE EL T04
        private void GenerarListaTreeView(Componente componente, List<string> resultado, int nivel)
        {
            // Creamos la indentación visual con guiones
            string guiones = new string('-', nivel * 2);
            
            // Verificamos el tipo para la etiqueta (Composite)
            string tipo = (componente is Familia) ? "Familia" : "Patente";
            
            resultado.Add($"{guiones} {componente.Nombre} ({tipo})");

            // RECURSIVIDAD: Si tiene hijos, los recorremos
            if (componente.Hijos != null)
            {
                foreach (var hijo in componente.Hijos)
                {
                    GenerarListaTreeView(hijo, resultado, nivel + 1);
                }
            }
        }
    }
}