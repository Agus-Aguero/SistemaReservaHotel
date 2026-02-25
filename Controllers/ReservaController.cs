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

            // 5. LÓGICA DE MONEDA
            ViewBag.Moneda = moneda;

            // 5.1. Componente Concreto Base
            IPrecioDisplay display = new PrecioPesosDisplay();

            if (moneda == "USD") 
            {
                var service = new DolarService();
                decimal cotizacion = await service.ObtenerCotizaciónBlue();
                ViewBag.Cotizacion = cotizacion;

                display = new PrecioDolarDecorator(display, cotizacion);
            }

            // 5.3. Pasamos el decorador a la vista
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

            if (reserva.FechaFin < reserva.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de salida no puede ser anterior a la fecha de ingreso.");
            }

            if (ModelState.IsValid)
            {
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

            // 1. Traemos la reserva actual
            var reserva = await _context.Reserva
                .Include(r => r.TipoHabitacion) // Ojo: Verifica si tu propiedad se llama 'Tipo' o 'TipoHabitacion' en el modelo
                .Include(r => r.Huesped)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            // 2. LÓGICA DE DISPONIBILIDAD REAL (POR FECHAS)
            // Buscamos los IDs de habitaciones que YA están ocupadas por OTROS en esas fechas
            var idsHabitacionesOcupadas = await _context.Reserva
                .Where(r => r.IdReserva != id) // Importante: Ignoramos la reserva actual (no compite consigo misma)
                .Where(r => r.IdHabitacion != null) // Solo reservas que ya tienen cuarto asignado
                .Where(r => r.Estado != "Cancelada") // Ignoramos las canceladas
                .Where(r => r.FechaInicio < reserva.FechaFin && r.FechaFin > reserva.FechaInicio) // Lógica de solapamiento de fechas
                .Select(r => r.IdHabitacion.Value)
                .ToListAsync();

            // 3. FILTRADO FINAL
            // Traemos las habitaciones que:
            // A. Son del mismo TIPO que la reserva
            // B. No están en la lista de ocupadas (idsHabitacionesOcupadas)
            // C. Están operativas (h.Disponible = true) O es la habitación que ya tiene esta reserva
            var habitacionesDisponibles = await _context.Habitacion
                .Where(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion)
                .Where(h => !idsHabitacionesOcupadas.Contains(h.IdHabitacion)) // ¡Aquí está la magia!
                .Where(h => h.Disponible || h.IdHabitacion == reserva.IdHabitacion)
                .Select(h => new 
                {
                    IdHabitacion = h.IdHabitacion,
                    Numero = $"Habitación {h.Numero}" // Formato bonito para el DropDown
                })
                .ToListAsync();

            // 4. Cargamos los ViewData
            // Si la lista está vacía, agregamos una opción manual para avisar visualmente
            if (!habitacionesDisponibles.Any())
            {
                habitacionesDisponibles.Add(new { IdHabitacion = 0, Numero = "No hay habitaciones libres en estas fechas" });
            }

            ViewData["IdHabitacion"] = new SelectList(habitacionesDisponibles, "IdHabitacion", "Numero", reserva.IdHabitacion);
            
            // Mantenemos los otros selects por si acaso, aunque en Edit generalmente solo tocamos la habitación
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
            ModelState.Remove("Huesped");
            ModelState.Remove("Usuario");
            ModelState.Remove("TipoHabitacion");
            ModelState.Remove("Habitacion");

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. RECUPERAR DATOS ORIGINALES
                    var reservaOriginal = await _context.Reserva.AsNoTracking()
                        .FirstOrDefaultAsync(r => r.IdReserva == id);
                    
                    if (reservaOriginal != null)
                    {
                        reserva.IdUsuario = reservaOriginal.IdUsuario;
                        // Importante: Si la vista no envía IdPersona, mantener el original
                        if (reserva.IdPersona == 0) reserva.IdPersona = reservaOriginal.IdPersona;
                    }

                    // 3. VALIDACIÓN DE DISPONIBILIDAD DEL POOL (La que ya tenías)
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
                        // OJO: Aquí debes llamar a tu método de recarga, pero corregido para que filtre habitaciones
                        await RecargarCombosEditFiltrado(reserva); 
                        return View(reserva);
                    }

                    // 4. NUEVA VALIDACIÓN: DISPONIBILIDAD FÍSICA DE LA HABITACIÓN (Si eligió una)
                    if (reserva.IdHabitacion != null)
                    {
                        bool habitacionOcupada = await _context.Reserva
                            .AnyAsync(r => r.IdReserva != id
                                        && r.IdHabitacion == reserva.IdHabitacion
                                        && r.Estado != "Cancelada"
                                        && r.FechaInicio < reserva.FechaFin 
                                        && r.FechaFin > reserva.FechaInicio);

                        if (habitacionOcupada)
                        {
                            ModelState.AddModelError("IdHabitacion", "La habitación seleccionada ya está ocupada en esas fechas.");
                            await RecargarCombosEditFiltrado(reserva);
                            return View(reserva);
                        }
                    }

                    // 5. RECALCULAR PRECIO Y GUARDAR
                    var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    if (tipoHab != null) {
                        reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
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
            
            await RecargarCombosEditFiltrado(reserva); 
            return View(reserva);
        }

        private async Task RecargarCombosEditFiltrado(Reserva reserva)
        {
            var idsOcupadas = await _context.Reserva
                .Where(r => r.IdReserva != reserva.IdReserva && r.IdHabitacion != null && r.Estado != "Cancelada" 
                            && r.FechaInicio < reserva.FechaFin && r.FechaFin > reserva.FechaInicio)
                .Select(r => r.IdHabitacion.Value)
                .ToListAsync();

            var habitacionesDisponibles = await _context.Habitacion
                .Where(h => h.IdTipoHabitacion == reserva.IdTipoHabitacion 
                            && !idsOcupadas.Contains(h.IdHabitacion)
                            && (h.Disponible || h.IdHabitacion == reserva.IdHabitacion))
                .Select(h => new { IdHabitacion = h.IdHabitacion, Numero = $"Habitación {h.Numero}" })
                .ToListAsync();

            if (!habitacionesDisponibles.Any()) 
                habitacionesDisponibles.Add(new { IdHabitacion = 0, Numero = "Sin disponibilidad física" });

            ViewData["IdHabitacion"] = new SelectList(habitacionesDisponibles, "IdHabitacion", "Numero", reserva.IdHabitacion);
            ViewData["IdPersona"] = new SelectList(_context.Persona, "IdPersona", "Apellido", reserva.IdPersona);
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", reserva.IdTipoHabitacion);
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
                }
            }
        } 

        private decimal CalcularPresupuesto(DateTime inicio, DateTime fin, decimal precioBase)
        {
            int noches = (fin - inicio).Days;
            if (noches <= 0) noches = 1;
            return noches * precioBase;
        }   
    }
}