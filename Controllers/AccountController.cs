using Microsoft.AspNetCore.Mvc;
using SistemaReserva.Models;
using SistemaReserva.Patters;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models.Seguridad;

namespace SistemaReserva.Controllers
{
    public class AccountController : Controller
    {
        private readonly SistemaReservaContext _context;

        public AccountController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            // Si ya hay una sesión activa, redirigir al Home
            if (SesionUsuario.Instancia.Email != null) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            string hashIngresado = Encriptador.GenerarHash(password);

            var usuario = await _context.Usuario
                .Include(u => u.Perfil) 
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == hashIngresado);

            if (usuario != null)
            {
                // LLAMADA CLAVE: Cargamos recursivamente toda la estructura del Composite
                if (usuario.Perfil != null)
                {
                    await CargarHijosRecursivo(usuario.Perfil);
                }

                SesionUsuario.Instancia.Login(usuario.IdUsuario, usuario.Email, usuario.Perfil);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Credenciales incorrectas.";
            return View();
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // Si el usuario ya está logueado, lo mandamos al Home
            if (SesionUsuario.Instancia.Email != null) return RedirectToAction("Index", "Home");
            
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Verificamos si el usuario ya existe para evitar duplicados
                var usuarioExistente = await _context.Usuario.AnyAsync(u => u.Email == model.Email);
                if (usuarioExistente)
                {
                    ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                    return View(model);
                }

                // 2. Buscamos el perfil "Huesped" en la base de datos (definido en el Seeder)
                var perfilHuesped = await _context.Componente
                    .FirstOrDefaultAsync(c => c.Nombre == "Huesped");

                if (perfilHuesped == null)
                {
                    ModelState.AddModelError("", "Error crítico: El perfil por defecto no existe. Contacte al administrador.");
                    return View(model);
                }

                // 3. Creamos la instancia del nuevo Usuario
                var nuevoUsuario = new Usuario
                {
                    Email = model.Email,
                    Password = Encriptador.GenerarHash(model.Password), // Encriptamos la clave
                    Perfil = perfilHuesped // Asignamos el objeto Familia "Huesped"
                };

                // 4. Guardamos en la base de datos
                _context.Usuario.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "¡Cuenta creada con éxito! Ahora podés iniciar sesión.";
                return RedirectToAction("Login");
            }

            // Si llegamos acá, algo falló en las validaciones (ej: contraseñas no coinciden)
            return View(model);
        }

        // Método auxiliar para romper la limitación de EF Core
        private async Task CargarHijosRecursivo(Componente componente)
        {
            // Cargamos los hijos del componente actual
            await _context.Entry(componente)
                .Collection(c => c.Hijos)
                .LoadAsync();

            // Si tiene hijos, entramos en cada uno para cargar sus propios hijos (recursividad)
            foreach (var hijo in componente.Hijos)
            {
                await CargarHijosRecursivo(hijo);
            }
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            // LOG OUT: Limpiar el Singleton
            SesionUsuario.Instancia.Logout();
            
            return RedirectToAction("Login");
        }
    }
}