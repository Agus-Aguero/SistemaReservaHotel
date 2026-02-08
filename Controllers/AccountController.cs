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
                var tienePerfil = await _context.Persona.AnyAsync(p => p.Email == usuario.Email);
    
                if (!tienePerfil && !SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
                {
                    return RedirectToAction("CompleteData"); // Lo mandamos directo a completar
                }
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
                    Perfil = perfilHuesped, // Asignamos el objeto Familia "Huesped"
                    PreguntaSeguridad = model.PreguntaSeguridad,
                    RespuestaSeguridad = model.RespuestaSeguridad
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


        // GET: Account/ChangePassword
        public IActionResult ChangePassword()
        {
            if (SesionUsuario.Instancia.Email == null) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var emailLogueado = SesionUsuario.Instancia.Email;
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Email == emailLogueado);

            if (usuario == null) return NotFound();

            // 1. Verificar que la contraseña actual sea correcta
            // Asumimos que tu Encriptador tiene un método para comparar o generar el hash
            var hashActual = Encriptador.GenerarHash(model.CurrentPassword);
            
            if (usuario.Password != hashActual)
            {
                ModelState.AddModelError("CurrentPassword", "La contraseña actual no es correcta.");
                return View(model);
            }

            // 2. Actualizar con la nueva contraseña
            usuario.Password = Encriptador.GenerarHash(model.NewPassword);
            _context.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Contraseña cambiada con éxito.";
            
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (usuario == null)
            {
                ModelState.AddModelError("Email", "El email no está registrado en ArgenTower.");
                return View(model);
            }

            // PASO 1: Validamos Email y recuperamos la pregunta
            if (model.Paso == 1)
            {
                model.Pregunta = usuario.PreguntaSeguridad;
                model.Paso = 2;
                ModelState.Clear(); // Limpiamos validaciones del email para el siguiente paso
                return View(model);
            }

            // PASO 2: Validamos la respuesta
            if (model.Paso == 2)
            {
                if (usuario.RespuestaSeguridad.ToLower() == model.Respuesta?.ToLower())
                {
                    model.Paso = 3;
                    model.Pregunta = usuario.PreguntaSeguridad; // La mantenemos para la vista
                    return View(model);
                }
                ModelState.AddModelError("Respuesta", "La respuesta es incorrecta.");
                model.Pregunta = usuario.PreguntaSeguridad; // No perder la pregunta al recargar
                return View(model);
            }

            // PASO 3: Cambio de contraseña final
            if (model.Paso == 3)
            {
                if (string.IsNullOrEmpty(model.NuevaPassword))
                {
                    ModelState.AddModelError("NuevaPassword", "Debes ingresar una nueva contraseña.");
                    model.Pregunta = usuario.PreguntaSeguridad;
                    return View(model);
                }

                usuario.Password = Encriptador.GenerarHash(model.NuevaPassword);
                _context.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Contraseña restablecida con éxito. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: Account/CompleteData
        [HttpGet]
        public async Task<IActionResult> CompleteData()
        {
            var emailLogueado = SesionUsuario.Instancia.Email;
            
            // Verificamos si ya existe para no pedir datos de más
            var existe = await _context.Persona.AnyAsync(p => p.Email == emailLogueado);
            if (existe)
            {
                return RedirectToAction("Create", "Reserva");
            }

            // Si no existe, le mostramos el formulario vacío (o con el email ya cargado)
            var nuevoHuesped = new Huesped { Email = emailLogueado };
            return View(nuevoHuesped);
        }

        // POST: Account/CompleteData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteData(Huesped huesped)
        {
            var emailLogueado = SesionUsuario.Instancia.Email;

            var existe = await _context.Persona.AnyAsync(p => p.Email == emailLogueado);
            if (existe) return RedirectToAction("Index", "Home"); // Cambiado a Home

            // Limpiamos errores de validación de objetos relacionados que no cargamos en el form
            ModelState.Remove("Reservas"); 

            if (ModelState.IsValid)
            {
                try
                {
                    huesped.Email = emailLogueado;
                    
                    _context.Huesped.Add(huesped); 
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "¡Perfil completado con éxito!";
                    return RedirectToAction("Index", "Home"); // Volvemos al inicio
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
            }
            return View(huesped);
        }

        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            var emailLogueado = SesionUsuario.Instancia.Email;
            
            // Buscamos en la tabla base Persona para ver si existe el email
            var persona = await _context.Persona
                .FirstOrDefaultAsync(p => p.Email == emailLogueado);

            if (persona == null)
            {
                return RedirectToAction("CompleteData");
            }

            // Si es Recepcionista, lo mandamos al Edit de RecepcionistaController
            if (SesionUsuario.Instancia.TienePermiso("Recepcionista")) // O el permiso que definas
            {
                return RedirectToAction("Edit", "Recepcionista", new { id = persona.IdPersona });
            }

            // Si existe pero es un Huesped, vamos a sus detalles
            return RedirectToAction("Details", "Huesped", new { id = persona.IdPersona });
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