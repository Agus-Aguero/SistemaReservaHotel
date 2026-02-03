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
            // 1. VALIDACIÓN DE SEGURIDAD (T04 + T02)
            // Usamos el Singleton para verificar el permiso de forma recursiva
            if (!SesionUsuario.Instancia.TienePermiso("Ver Reservas"))
            {
                // Si no tiene permiso, lo mandamos al Home con un aviso
                TempData["Error"] = "No tienes permisos para acceder a la gestión de reservas.";
                return RedirectToAction("Index", "Home");
            }

            // 2. LÓGICA DE NEGOCIO EXISTENTE
            var reservas = await _context.Reserva
                .Include(r => r.Huesped)
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Habitacion)
                .ToListAsync();

            ViewBag.Moneda = moneda;
            if (moneda == "USD") {
                var service = new DolarService();
                ViewBag.Cotizacion = await service.ObtenerCotizaciónBlue();
            }

            return View(reservas);
        }

        // GET: Reservas/Create
        public IActionResult Create()
        {
            if (!SesionUsuario.Instancia.TienePermiso("Crear Reserva")) return Forbid();

            var emailLogueado = SesionUsuario.Instancia.Email;
            var esAdminORecepcion = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            // Buscamos en la tabla de Huespedes (que hereda de Persona)
            IQueryable<Huesped> listaHuespedes = _context.Persona.OfType<Huesped>();

            if (!esAdminORecepcion)
            {
                // Filtramos para que el Huésped solo se vea a sí mismo
                listaHuespedes = listaHuespedes.Where(h => h.Email == emailLogueado);
            }

            // El nombre del campo en el SelectList sigue siendo IdPersona porque lo hereda
            ViewData["IdPersona"] = new SelectList(listaHuespedes, "IdPersona", "Apellido");
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            if (ModelState.IsValid)
            {
                // 1. Contar cuántas habitaciones totales existen de ese TIPO
                var totalHabitaciones = await _context.Habitacion
                    .CountAsync(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion);

                // 2. Contar reservas que se SOLAPAN con las fechas elegidas
                // Una reserva se solapa si: (Inicio < FinReservaExistente) Y (Fin > InicioReservaExistente)
                var reservasOcupadas = await _context.Reserva
                    .CountAsync(r => r.IdTipoHabitacion == reserva.IdTipoHabitacion &&
                                    r.Estado != "Cancelada" && // Ignoramos las canceladas
                                    reserva.FechaInicio < r.FechaFin && 
                                    reserva.FechaFin > r.FechaInicio);

                // 3. Validar si hay cupo
                if (reservasOcupadas >= totalHabitaciones)
                {
                    var tipo = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    ModelState.AddModelError("", $"Lo sentimos, no hay habitaciones de tipo '{tipo?.Nombre}' disponibles para el rango de fechas seleccionado.");
                    
                    // Recargamos los combos para volver a la vista
                    ViewData["IdPersona"] = new SelectList(_context.Persona, "IdPersona", "Apellido", reserva.IdPersona);
                    ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", reserva.IdTipoHabitacion);
                    return View(reserva);
                }

                // --- Si pasó la validación, seguimos con el guardado normal ---
                var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
                reserva.Estado = "Pendiente";
                reserva.IdUsuario = SesionUsuario.Instancia.IdUsuario;

                _context.Add(reserva);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reserva);
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

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. VALIDACIÓN DE DISPONIBILIDAD (Igual que el Create pero excluyendo esta reserva)
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
                        ModelState.AddModelError("", $"No se pueden cambiar las fechas: El tipo '{tipo?.Nombre}' está lleno para ese período.");
                        
                        // Recargar datos para la vista
                        CargarCombosEdit(reserva); 
                        return View(reserva);
                    }

                    // 2. RECALCULAR PRECIO
                    var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    if (tipoHab != null) {
                        reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
                    }

                    // 3. AUTOMATISMO DE ESTADO 
                    if (reserva.IdHabitacion.HasValue)
                    {
                        reserva.Estado = "Activa";
                        await CambiarEstadoHabitacion(reserva.IdHabitacion, false);
                    }

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

        // POST: Reserva/Finalizar/5
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