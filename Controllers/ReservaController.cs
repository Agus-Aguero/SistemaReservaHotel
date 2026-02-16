using System.Text.Json;
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

            // 2. PREPARAMOS LA CONSULTA
            var query = _context.Reserva
                .Include(r => r.Huesped)
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Habitacion)
                .AsQueryable();

            // 3. FILTRO DE PRIVACIDAD ACTUALIZADO:
            // Agregamos la validación para el perfil "Recepcion"
            bool esStaff = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios") || 
                        SesionUsuario.Instancia.TienePermiso("Recepcion");

            if (!esStaff)
            {
                // Si es un Huésped (no es staff), solo traemos sus reservas.
                var emailLogueado = SesionUsuario.Instancia.Email;
                query = query.Where(r => r.Huesped.Email == emailLogueado);
            }
            else 
            {
                // Si es Staff, ordenamos para que lo más reciente aparezca primero
                query = query.OrderByDescending(r => r.FechaInicio);
            }

            // 4. EJECUTAMOS LA CONSULTA FILTRADA
            var reservas = await query.ToListAsync();

            // 5. LÓGICA DE MONEDA (Se mantiene igual)
            ViewBag.Moneda = moneda;

            // 5.1. Componente Concreto Base (Pesos)
            IPrecioDisplay display = new PrecioPesosDisplay();

            if (moneda == "USD") 
            {
                var service = new DolarService();
                decimal cotizacion = await service.ObtenerCotizaciónBlue();
                ViewBag.Cotizacion = cotizacion;

                // 5.2. Envolvemos el objeto base con el Decorador de Dólares
                display = new PrecioDolarDecorator(display, cotizacion);
            }

            // 5.3. Pasamos el decorador (ya sea simple o decorado) a la vista
            ViewBag.Display = display;

            return View(reservas);
        }

        // GET: Reserva/Create
        public async Task<IActionResult> Create()
        {
            // 1. OBTENCIÓN DE DATOS DEL USUARIO Y PERMISOS
            var emailLogueado = SesionUsuario.Instancia.Email?.Trim().ToLower();
            
            // Aquí ya incluiste correctamente al Recepcionista
            bool esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios") || 
                        SesionUsuario.Instancia.TienePermiso("Recepcion");

            var personaLogueada = await _context.Persona
                .FirstOrDefaultAsync(p => p.Email.ToLower() == emailLogueado);

            ViewBag.TienePerfil = esAdmin || (personaLogueada != null);

            // 2. DEFINICIÓN DE CONSULTA DE HUÉSPEDES
            IQueryable<Huesped> consulta = _context.Huesped;

            // CAMBIO CLAVE: Si es Admin o Recepcionista, entra al ELSE y ve a todos.
            if (!esAdmin)
            {
                // Solo el Huésped común entra aquí y se filtra a sí mismo
                consulta = consulta.Where(h => h.Email.ToLower() == emailLogueado);
            }
            else 
            {
                // Admin y Recepcionista ven todo el listado
                consulta = consulta.OrderBy(h => h.Apellido).ThenBy(h => h.Nombre);
            }

            var listaHuespedes = await consulta.ToListAsync();

            // 3. CARGA DE DATOS PARA EL PRESUPUESTO DINÁMICO
            var tiposHabitacion = await _context.TipoHabitacion.ToListAsync();

            var preciosHabitaciones = tiposHabitacion.Select(t => new { 
                t.IdTipoHabitacion, 
                t.PrecioBase 
            }).ToList();

            ViewBag.PreciosJson = JsonSerializer.Serialize(preciosHabitaciones);

            var service = new DolarService();
            ViewBag.Cotizacion = await service.ObtenerCotizaciónBlue();

            // 4. DATOS PARA LOS SELECTS DE LA VISTA
            ViewData["IdPersona"] = new SelectList(listaHuespedes.Select(h => new {
                h.IdPersona,
                NombreCompleto = $"{h.Apellido}, {h.Nombre}"
            }), "IdPersona", "NombreCompleto");

            ViewBag.TiposConCamas = tiposHabitacion.Select(t => new {
                t.IdTipoHabitacion,
                DetalleFull = $"{t.Nombre} ({(t.DescripcionCamas ?? "Sin descripción")})"
            }).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            // 1. Asignamos el usuario desde la sesión
            reserva.IdUsuario = SesionUsuario.Instancia.IdUsuario;

            // 2. Limpieza para validación manual
            ModelState.Remove("Usuario");
            ModelState.Remove("Huesped");
            ModelState.Remove("TipoHabitacion");

            if (ModelState.IsValid)
            {
                // --- LÓGICA DE POOLS COMPATIBLES ---
                // IDs: 1:Twin, 2:Doble, 3:Doble Premium, 4:Cuadr, 5:Cuadr Indiv, 6:Suite
                List<int> idsCompatibles = new List<int> { reserva.IdTipoHabitacion };

                if (reserva.IdTipoHabitacion == 1 || reserva.IdTipoHabitacion == 2)
                {
                    idsCompatibles = new List<int> { 1, 2 }; // Pool Matrimonial/Twin
                }
                else if (reserva.IdTipoHabitacion == 4 || reserva.IdTipoHabitacion == 5)
                {
                    idsCompatibles = new List<int> { 4, 5 }; // Pool Cuádruples
                }

                // A. Buscamos todas las habitaciones físicas candidatas
                var habitacionesCandidatas = await _context.Habitacion
                    .Where(h => idsCompatibles.Contains(h.IdTipoHabitacion))
                    .Select(h => h.IdHabitacion)
                    .ToListAsync();

                int totalFisico = habitacionesCandidatas.Count;

                // B. Contamos ocupación física real
                var ocupadasFisicamente = await _context.Reserva
                    .CountAsync(r => r.IdHabitacion != null &&
                                    habitacionesCandidatas.Contains(r.IdHabitacion.Value) &&
                                    r.Estado != "Cancelada" &&
                                    reserva.FechaInicio < r.FechaFin && 
                                    reserva.FechaFin > r.FechaInicio);

                // C. Contamos reservas pendientes del pool sin habitación asignada
                var pendientesPool = await _context.Reserva
                    .CountAsync(r => idsCompatibles.Contains(r.IdTipoHabitacion) &&
                                    r.IdHabitacion == null &&
                                    r.Estado == "Pendiente" &&
                                    reserva.FechaInicio < r.FechaFin && 
                                    reserva.FechaFin > r.FechaInicio);

                // D. Verificación de Disponibilidad
                if ((ocupadasFisicamente + pendientesPool) >= totalFisico)
                {
                    var tipo = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    ModelState.AddModelError("", $"No hay disponibilidad física para {tipo?.Nombre} en esas fechas.");
                    await RecargarDatosVista(reserva);
                    return View(reserva);
                }

                // --- GUARDADO ---
                var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
                reserva.Estado = "Pendiente";

                _context.Add(reserva);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await RecargarDatosVista(reserva);
            return View(reserva);
        }

        // Método auxiliar para evitar repetir código y errores de Nulo
        private async Task RecargarDatosVista(Reserva reserva = null)
        {
            var emailLogueado = SesionUsuario.Instancia.Email?.Trim().ToLower();
            bool esAdmin = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            // 1. Carga de Huéspedes
            IQueryable<Huesped> consulta = _context.Huesped;
            if (!esAdmin) {
                consulta = consulta.Where(h => h.Email.ToLower() == emailLogueado);
            } else {
                consulta = consulta.OrderBy(h => h.Apellido).ThenBy(h => h.Nombre);
            }
            var listaHuespedes = await consulta.ToListAsync();

            // 2. Carga de Tipos con la descripción de camas (Lo que pide la vista)
            var tiposDb = await _context.TipoHabitacion.ToListAsync();
            
            // Mapeamos a una lista anónima para el SelectList de la vista
            ViewBag.TiposConCamas = tiposDb.Select(t => new {
                IdTipoHabitacion = t.IdTipoHabitacion,
                DetalleFull = $"{t.Nombre} ({(t.DescripcionCamas ?? "Sin especificar")})"
            }).ToList();

            // 3. Precios para el JavaScript (Presupuesto)
            var preciosJson = tiposDb.Select(t => new { 
                t.IdTipoHabitacion, 
                t.PrecioBase 
            }).ToList();
            ViewBag.PreciosJson = System.Text.Json.JsonSerializer.Serialize(preciosJson);

            // 4. Datos para el SelectList de Huéspedes
            ViewData["IdPersona"] = new SelectList(listaHuespedes.Select(h => new {
                h.IdPersona,
                NombreCompleto = $"{h.Apellido}, {h.Nombre}"
            }), "IdPersona", "NombreCompleto", reserva?.IdPersona);

            // 5. Cotización del Dólar
            var service = new DolarService();
            ViewBag.Cotizacion = await service.ObtenerCotizaciónBlue();
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