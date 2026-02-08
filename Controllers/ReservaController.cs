using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Patters;

namespace SistemaReserva.Controllers
{
    public class ReservaController : Controller
    {
        private readonly SistemaReservaContext _context;

        public ReservaController(SistemaReservaContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index(string moneda = "ARS")
        {
            // 1. VALIDACIÓN DE SEGURIDAD 
            if (!SesionUsuario.Instancia.TienePermiso("Ver Reservas"))
            {
                TempData["Error"] = "No tienes permisos para acceder a la gestión de reservas.";
                return RedirectToAction("Index", "Home");
            }

            // 2. PREPARAMOS LA CONSULTA (IQueryable permite agregar filtros antes de ir a la DB)
            var query = _context.Reserva
                .Include(r => r.Huesped)
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Habitacion)
                .AsQueryable();

            // 3. FILTRO DE PRIVACIDAD:
            // Si no tiene permiso de gestión (es un Huésped), solo traemos sus reservas.
            // Si es Admin/Recepcionista, la consulta queda igual y ve todo.
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
            {
                var emailLogueado = SesionUsuario.Instancia.Email;
                query = query.Where(r => r.Huesped.Email == emailLogueado);
            }

            // 4. EJECUTAMOS LA CONSULTA FILTRADA
            var reservas = await query.ToListAsync();

            // 5. LÓGICA DE MONEDA
            ViewBag.Moneda = moneda;
            if (moneda == "USD") 
            {
                var service = new DolarService();
                ViewBag.Cotizacion = await service.ObtenerCotizaciónBlue();
            }

            return View(reservas);
        }

        // GET: Reserva/Create

       public async Task<IActionResult> Create()
        {
            var emailLogueado = SesionUsuario.Instancia.Email?.Trim().ToLower();
            bool esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            // 1. Verificamos si el usuario actual tiene su perfil completo (solo relevante para Huéspedes)
            var personaLogueada = await _context.Persona
                .FirstOrDefaultAsync(p => p.Email.ToLower() == emailLogueado);

            // El Admin siempre puede entrar; el Huésped necesita tener perfil
            ViewBag.TienePerfil = esAdmin || (personaLogueada != null);

            // 2. Definimos la consulta de Huéspedes
            // Usamos _context.Huesped para traer solo a los clientes registrados
            IQueryable<Huesped> consulta = _context.Huesped;

            if (!esAdmin)
            {
                // El Huésped común solo se ve a sí mismo para autocompletar
                consulta = consulta.Where(h => h.Email.ToLower() == emailLogueado);
            }
            else 
            {
                // El Admin ve a todos los Huéspedes ordenados por apellido para facilitar la búsqueda
                consulta = consulta.OrderBy(h => h.Apellido).ThenBy(h => h.Nombre);
            }

            var listaHuespedes = await consulta.ToListAsync();

            // 3. Cargamos los ViewData para los Selects
            // Para el Admin, el texto mostrado será "Apellido, Nombre"
            ViewData["IdPersona"] = new SelectList(listaHuespedes, "IdPersona", "Apellido");
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            // 1. PRE-VALIDACIÓN: Asignamos el ID de usuario del Singleton antes de validar el modelo
            reserva.IdUsuario = SesionUsuario.Instancia.IdUsuario;

            // 2. LIMPIEZA: Quitamos las propiedades de navegación de la validación
            // Esto evita errores si EF intenta validar objetos que no vienen del formulario
            ModelState.Remove("Usuario");
            ModelState.Remove("Huesped");
            ModelState.Remove("TipoHabitacion");

            if (ModelState.IsValid)
            {
                // --- Lógica de disponibilidad (Lo que ya tenías) ---
                var totalHabitaciones = await _context.Habitacion
                    .CountAsync(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion);

                var reservasOcupadas = await _context.Reserva
                    .CountAsync(r => r.IdTipoHabitacion == reserva.IdTipoHabitacion &&
                                    r.Estado != "Cancelada" &&
                                    reserva.FechaInicio < r.FechaFin && 
                                    reserva.FechaFin > r.FechaInicio);

                if (reservasOcupadas >= totalHabitaciones)
                {
                    var tipo = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    ModelState.AddModelError("", $"Lo sentimos, no hay habitaciones de tipo '{tipo?.Nombre}' disponibles.");
                    
                    // Recarga de combos en caso de error de disponibilidad
                    await RecargarDatosVista(reserva);
                    return View(reserva);
                }

                // --- Guardado ---
                var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
                reserva.Estado = "Pendiente";

                _context.Add(reserva);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // 3. SI EL MODELO NO ES VÁLIDO: Recargamos todo para no perder el nombre
            await RecargarDatosVista(reserva);
            return View(reserva);
        }

        // Método auxiliar para no repetir código de recarga
        private async Task RecargarDatosVista(Reserva reserva)
        {
            var emailLogueado = SesionUsuario.Instancia.Email?.Trim().ToLower();
            ViewBag.TienePerfil = true;
            
            // Volvemos a filtrar para que el Huésped vea su apellido y no "Usuario"
            IQueryable<Huesped> consulta = _context.Huesped;
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios"))
            {
                consulta = consulta.Where(h => h.Email.ToLower() == emailLogueado);
            }

            ViewData["IdPersona"] = new SelectList(await consulta.ToListAsync(), "IdPersona", "Apellido", reserva.IdPersona);
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", reserva.IdTipoHabitacion);
        }


        // GET: Reserva/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Traemos la reserva incluyendo el Tipo para saber qué filtrar
            var reserva = await _context.Reserva
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Huesped)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            // FILTRADO: Buscamos habitaciones que coincidan con el tipo Y estén disponibles
            // También incluimos la habitación que ya tenga asignada (por si solo estamos editando otra cosa)
            var habitacionesDisponibles = await _context.Habitacion
                .Where(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion && (h.Disponible || h.IdHabitacion == reserva.IdHabitacion))
                .ToListAsync();

            ViewData["IdHabitacion"] = new SelectList(habitacionesDisponibles, "IdHabitacion", "Numero", reserva.IdHabitacion);
            ViewData["IdPersona"] = new SelectList(_context.Persona, "IdPersona", "Apellido", reserva.IdPersona);
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", reserva.IdTipoHabitacion);

            return View(reserva);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reserva reserva)
        {
            if (id != reserva.IdReserva) return NotFound();

            // 1. LIMPIEZA DE VALIDACIONES
            // Esto evita que el formulario rebote por objetos que no están en la vista
            ModelState.Remove("Huesped");
            ModelState.Remove("Usuario");
            ModelState.Remove("TipoHabitacion");
            ModelState.Remove("Habitacion");

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. RECUPERAR DATOS ORIGINALES
                    // Buscamos la reserva actual sin rastrearla (AsNoTracking) para obtener el IdUsuario original
                    var reservaOriginal = await _context.Reserva.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.IdReserva == id);
                    
                    if (reservaOriginal != null)
                    {
                        reserva.IdUsuario = reservaOriginal.IdUsuario; // Mantenemos el creador original
                    }

                    // 3. VALIDACIÓN DE DISPONIBILIDAD (Tu lógica actual)
                    var totalHabitaciones = await _context.Habitacion
                        .CountAsync(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion);

                    var reservasOcupadas = await _context.Reserva
                        .CountAsync(r => r.IdTipoHabitacion == reserva.IdTipoHabitacion &&
                                        r.IdReserva != id &&
                                        r.Estado != "Cancelada" &&
                                        reserva.FechaInicio < r.FechaFin && 
                                        reserva.FechaFin > r.FechaInicio);

                    if (reservasOcupadas >= totalHabitaciones)
                    {
                        var tipo = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                        ModelState.AddModelError("", $"No hay cupo para el tipo '{tipo?.Nombre}' en esas fechas.");
                        CargarCombosEdit(reserva); 
                        return View(reserva);
                    }

                    // 4. RECALCULAR PRECIO Y ESTADO
                    var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    if (tipoHab != null) {
                        reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
                    }

                    // Si se asignó habitación, el estado pasa a ser el que definas (ej: "Pendiente" pero con pieza)
                    // El Check-In se encargará de pasarla a "Hospedado"
                    
                    _context.Update(reserva);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservaExists(reserva.IdReserva)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            CargarCombosEdit(reserva); 
            return View(reserva);
        }

        // Método auxiliar para no repetir código de los SelectList
        private void CargarCombosEdit(Reserva reserva) {
            ViewData["IdHabitacion"] = new SelectList(_context.Habitacion.Where(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion), "IdHabitacion", "Numero", reserva.IdHabitacion);
            ViewData["IdPersona"] = new SelectList(_context.Persona, "IdPersona", "Apellido", reserva.IdPersona);
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", reserva.IdTipoHabitacion);
        }

        // GET: Reserva/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reserva
                .Include(r => r.Huesped)
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Habitacion)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reserva = await _context.Reserva.FindAsync(id);
            if (reserva != null)
            {
                // 1. Antes de borrar la reserva, liberamos la habitación
                await CambiarEstadoHabitacion(reserva.IdHabitacion, true);

                _context.Reserva.Remove(reserva);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

       /* // POST: Reserva/Finalizar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(int id)
        {
            // Incluimos el TipoHabitacion para sacar el PrecioBase
            var reserva = await _context.Reserva
                .Include(r => r.TipoHabitacion)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva != null && reserva.TipoHabitacion != null)
            {
                // 1. Calculamos la cantidad de noches (mínimo 1 noche)
                int noches = (reserva.FechaFin - reserva.FechaInicio).Days;
                if (noches <= 0) noches = 1; 

                // 2. Calculamos el total
                reserva.PrecioTotal = noches * reserva.TipoHabitacion.PrecioBase;

                // 3. Liberamos la habitación y cambiamos estado
                await CambiarEstadoHabitacion(reserva.IdHabitacion, true);
                reserva.Estado = "Finalizada";

                _context.Update(reserva);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }*/

        // MÉTODO PARA CHECK-IN
        [HttpPost]
        public async Task<IActionResult> CheckIn(int id)
        {
            var reserva = await _context.Reserva.FindAsync(id);
            if (reserva != null && reserva.IdHabitacion.HasValue)
            {
                reserva.Estado = "Hospedado"; // O "Activa", según tu lógica
                await CambiarEstadoHabitacion(reserva.IdHabitacion, false); // Ocupada
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // MÉTODO PARA CHECK-OUT
        [HttpPost]
        public async Task<IActionResult> CheckOut(int id)
        {
            var reserva = await _context.Reserva.FindAsync(id);
            if (reserva != null)
            {
                reserva.Estado = "Finalizada";
                if (reserva.IdHabitacion.HasValue)
                {
                    await CambiarEstadoHabitacion(reserva.IdHabitacion, true); // Liberar
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            // 1. Verificación del Composite
            if (!SesionUsuario.Instancia.TienePermiso("Cancelar Reserva")) return Forbid();

            var reserva = await _context.Reserva.FindAsync(id);
            if (reserva == null) return NotFound();

            // 2. Verificación de Propiedad (Solo el dueño o un Admin pueden cancelar)
            if (!SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios") && 
                reserva.IdUsuario != SesionUsuario.Instancia.IdUsuario)
            {
                return Forbid();
            }

            // 3. Ejecución
            reserva.Estado = "Cancelada";
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Reserva cancelada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool ReservaExists(int id)
        {
            // Verifica si existe al menos una reserva con ese ID
            return _context.Reserva.Any(e => e.IdReserva == id);
        }

        private async Task CambiarEstadoHabitacion(int? idHabitacion, bool disponible)
        {
            if (idHabitacion.HasValue)
            {
                var habitacion = await _context.Habitacion.FindAsync(idHabitacion);
                if (habitacion != null)
                {
                    habitacion.Disponible = disponible;
                    _context.Update(habitacion);
                    // No hacemos SaveChangesAsync aquí, dejamos que el método principal lo haga 
                    // junto con el resto de los cambios para mantener la "Atomicidad".
                }
            }
        } 

        private decimal CalcularPresupuesto(DateTime inicio, DateTime fin, decimal precioBase)
        {
            int noches = (fin - inicio).Days;
            if (noches <= 0) noches = 1; // Se cobra al menos una noche
            return noches * precioBase;
        }   
    }
}