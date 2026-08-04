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
            // 1. VALIDACIÓN: Sesión iniciada
            if (string.IsNullOrEmpty(SesionUsuario.Instancia.Email))
            {
                TempData["Error"] = "Debes iniciar sesión para acceder a tus reservas.";
                return RedirectToAction("Index", "Home");
            }

            // 2. NUEVOS CANDADOS DE AUTORIZACIÓN POSITIVA
            bool esStaff = SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios") || 
                        SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") ||
                        SesionUsuario.Instancia.TienePermiso("Ver Reservas") ||
                        SesionUsuario.Instancia.TienePermiso("Gestionar Habitaciones"); 

            bool esHuesped = SesionUsuario.Instancia.TienePermiso("Acceso Huesped");

            // Si no tiene NINGUNO de los perfiles válidos, lo frenamos
            if (!esStaff && !esHuesped)
            {
                TempData["Error"] = "Tu perfil no tiene los permisos necesarios para ver las reservas.";
                return RedirectToAction("Index", "Home");
            }

            // 3. PREPARAMOS LA CONSULTA
            var query = _context.Reserva
                .Include(r => r.Huesped)
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Habitacion)
                .Include(r => r.Cobro)
                .AsQueryable();

            // 4. FILTRO DE PRIVACIDAD ACTUALIZADO
            if (esHuesped && !esStaff)
            {
                // Es un Huésped puro: solo traemos sus reservas.
                var emailLogueado = SesionUsuario.Instancia.Email;
                query = query.Where(r => r.Huesped.Email == emailLogueado);
            }
            else if (esStaff) 
            {
                // Es Staff: ve todo, ordenado por fecha
                query = query.OrderByDescending(r => r.FechaInicio);
            }

            // 5. EJECUTAMOS LA CONSULTA FILTRADA
            var reservas = await query.ToListAsync();

            // 6. LÓGICA DE MONEDA
            ViewBag.Moneda = moneda;

            IPrecioDisplay display = new PrecioPesosDisplay();

            if (moneda == "USD") 
            {
                var service = new DolarService();
                decimal cotizacion = await service.ObtenerCotizaciónBlue();
                ViewBag.Cotizacion = cotizacion;

                display = new PrecioDolarDecorator(display, cotizacion);
            }

            ViewBag.Display = display;

            return View(reservas);
        }

        // GET: Reserva/Create
        public async Task<IActionResult> Create()
        {
            //  1. CANDADO DE AUTORIZACIÓN POSITIVA
            bool esStaffOperativo = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                                    SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
                                    
            bool esHuesped = SesionUsuario.Instancia.TienePermiso("Acceso Huesped");

            // Si no es empleado operativo Y TAMPOCO es un huésped válido, lo frenamos en seco.
            // (Esto bloquea automáticamente a los Auditores y a roles futuros sin acceso).
            if (!esStaffOperativo && !esHuesped)
            {
                TempData["Error"] = "Acceso Denegado: Tu perfil no tiene permisos para crear reservas.";
                return RedirectToAction(nameof(Index));
            }

            // 2. OBTENCIÓN DE DATOS DEL USUARIO Y PERMISOS
            var emailLogueado = SesionUsuario.Instancia.Email?.Trim().ToLower();

            var personaLogueada = await _context.Persona
                .FirstOrDefaultAsync(p => p.Email.ToLower() == emailLogueado);

            ViewBag.TienePerfil = esStaffOperativo || (personaLogueada != null);

            // 3. DEFINICIÓN DE CONSULTA DE HUÉSPEDES
            IQueryable<Huesped> consulta = _context.Huesped;

            if (esHuesped && !esStaffOperativo)
            {
                // El Huésped validado entra aquí y solo se ve a sí mismo en el combo desplegable
                consulta = consulta.Where(h => h.Email.ToLower() == emailLogueado);
            }
            else 
            {
                // Los empleados operativos ven todo el listado alfabéticamente
                consulta = consulta.OrderBy(h => h.Apellido).ThenBy(h => h.Nombre);
            }

            var listaHuespedes = await consulta.ToListAsync();

            // 4. CARGA DE DATOS PARA EL PRESUPUESTO DINÁMICO
            var tiposHabitacion = await _context.TipoHabitacion.ToListAsync();

            var preciosHabitaciones = tiposHabitacion.Select(t => new { 
                t.IdTipoHabitacion, 
                t.PrecioBase 
            }).ToList();

            ViewBag.PreciosJson = JsonSerializer.Serialize(preciosHabitaciones);

            var service = new DolarService();
            ViewBag.Cotizacion = await service.ObtenerCotizaciónBlue();

            // 5. DATOS PARA LOS SELECTS DE LA VISTA
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
            //  1. EVALUACIÓN DE PERMISOS
            bool esStaffOperativo = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                                    SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
                                    
            bool esHuesped = SesionUsuario.Instancia.TienePermiso("Acceso Huesped");

            // Si logró bypassear la vista y manda un POST sin permisos, rebota.
            if (!esStaffOperativo && !esHuesped)
            {
                TempData["Error"] = "Acceso Denegado: No tienes permisos para crear reservas.";
                return RedirectToAction(nameof(Index));
            }

            // 2. LÓGICA EXCLUSIVA DEL HUÉSPED
            if (esHuesped && !esStaffOperativo) 
            {
                var emailLogueado = SesionUsuario.Instancia.Email?.Trim().ToLower();
                var personaLogueada = await _context.Persona.FirstOrDefaultAsync(p => p.Email.ToLower() == emailLogueado);

                // Si por algún motivo entró alguien que no tiene sus datos cargados, lo frenamos
                if (personaLogueada == null)
                {
                    TempData["Error"] = "Error: No tienes un perfil de huésped asociado para reservar.";
                    return RedirectToAction(nameof(Index));
                }

                // CANDADO MAESTRO: Como es Huésped validado, la reserva se hace obligatoriamente a su nombre
                reserva.IdPersona = personaLogueada.IdPersona;
                ModelState.Remove("IdPersona");
            }

            // 3. Asignamos el usuario desde la sesión
            reserva.IdUsuario = SesionUsuario.Instancia.IdUsuario;

            // 4. Limpieza para validación manual
            ModelState.Remove("Usuario");
            ModelState.Remove("Huesped");
            ModelState.Remove("TipoHabitacion");
            ModelState.Remove("Habitacion");
            ModelState.Remove("Cobro");

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
                return RedirectToAction("Pagar", "Cobro", new { idReserva = reserva.IdReserva });
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
            bool puedeModificar = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                                SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            if (!puedeModificar) 
            {
                TempData["Error"] = "Acceso Denegado: Tu perfil es de solo lectura y no permite modificaciones.";
                return RedirectToAction(nameof(Index));
            }
            if (id == null) return NotFound();

            // 1. Traemos la reserva actual
            var reserva = await _context.Reserva
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Huesped)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            // 2. LÓGICA DE DISPONIBILIDAD REAL (POR FECHAS)
            var idsHabitacionesOcupadas = await _context.Reserva
                .Where(r => r.IdReserva != id) 
                .Where(r => r.IdHabitacion != null) 
                .Where(r => r.Estado != "Cancelada") 
                .Where(r => r.FechaInicio < reserva.FechaFin && r.FechaFin > reserva.FechaInicio) 
                .Select(r => r.IdHabitacion.Value)
                .ToListAsync();

            // AGREGAMOS EL POOL DE COMPATIBILIDAD QUE TENÍAS EN EL CREATE
            List<int> idsCompatibles = new List<int> { reserva.IdTipoHabitacion };
            if (reserva.IdTipoHabitacion == 1 || reserva.IdTipoHabitacion == 2)
            {
                idsCompatibles = new List<int> { 1, 2 }; // Pool Matrimonial/Twin
            }
            else if (reserva.IdTipoHabitacion == 4 || reserva.IdTipoHabitacion == 5)
            {
                idsCompatibles = new List<int> { 4, 5 }; // Pool Cuádruples
            }

            // 3. FILTRADO FINAL CON EL POOL
            var habitacionesDisponibles = await _context.Habitacion
                .Where(h => idsCompatibles.Contains(h.IdTipoHabitacion))
                .Where(h => !idsHabitacionesOcupadas.Contains(h.IdHabitacion)) 
                .Where(h => h.Disponible || h.IdHabitacion == reserva.IdHabitacion)
                .Select(h => new 
                {
                    IdHabitacion = h.IdHabitacion,
                    Numero = $"Habitación {h.Numero}" 
                })
                .ToListAsync();

            // 4. Cargamos los ViewData
            if (!habitacionesDisponibles.Any())
            {
                habitacionesDisponibles.Add(new { IdHabitacion = 0, Numero = "No hay habitaciones libres en estas fechas" });
            }

            ViewData["IdHabitacion"] = new SelectList(habitacionesDisponibles, "IdHabitacion", "Numero", reserva.IdHabitacion);
            
            // Mantenemos los otros selects
            ViewData["IdPersona"] = new SelectList(_context.Persona, "IdPersona", "Apellido", reserva.IdPersona);
            ViewData["IdTipoHabitacion"] = new SelectList(_context.TipoHabitacion, "IdTipoHabitacion", "Nombre", reserva.IdTipoHabitacion);

            return View(reserva);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reserva reserva)
        {
            // 0. CANDADO DE SEGURIDAD ABSOLUTA
            bool puedeModificar = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                                SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            if (!puedeModificar) 
            {
                TempData["Error"] = "Acceso Denegado: Tu perfil es de solo lectura y no permite modificaciones.";
                return RedirectToAction(nameof(Index));
            }

            if (id != reserva.IdReserva) return NotFound();

            // 1. LIMPIEZA DE VALIDACIONES
            ModelState.Remove("Huesped");
            ModelState.Remove("Usuario");
            ModelState.Remove("TipoHabitacion");
            ModelState.Remove("Habitacion");
            ModelState.Remove("Cobro");

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
                        
                        // Mantenemos el estado original para no alterarlo sin querer
                        reserva.Estado = reservaOriginal.Estado;
                    }

                    // 3. VALIDACIÓN DE DISPONIBILIDAD DEL POOL
                    List<int> idsCompatibles = new List<int> { reserva.IdTipoHabitacion };

                    if (reserva.IdTipoHabitacion == 1 || reserva.IdTipoHabitacion == 2)
                    {
                        idsCompatibles = new List<int> { 1, 2 }; // Pool Matrimonial/Twin
                    }
                    else if (reserva.IdTipoHabitacion == 4 || reserva.IdTipoHabitacion == 5)
                    {
                        idsCompatibles = new List<int> { 4, 5 }; // Pool Cuádruples
                    }

                    var totalHabitaciones = await _context.Habitacion
                        .CountAsync(h => idsCompatibles.Contains(h.IdTipoHabitacion));

                    var reservasOcupadas = await _context.Reserva
                        .CountAsync(r => idsCompatibles.Contains(r.IdTipoHabitacion) &&
                                        r.IdReserva != id &&
                                        r.Estado != "Cancelada" &&
                                        reserva.FechaInicio < r.FechaFin && 
                                        reserva.FechaFin > r.FechaInicio);

                    if (reservasOcupadas >= totalHabitaciones)
                    {
                        var tipo = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                        ModelState.AddModelError("", $"No hay cupo para el tipo '{tipo?.Nombre}' en esas fechas.");
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
                        
                        // OPCIONAL: Si estaba en "Pendiente" y le asignamos cuarto, la pasamos a "Confirmada"
                        if (reserva.Estado == "Pendiente") 
                        {
                            reserva.Estado = "Confirmada";
                        }
                    }

                    // NUEVO: INYECCIÓN MANUAL PARA LA AUDITORÍA DE HABITACIÓN
                    // Comparamos si le asignaron una habitación que antes no tenía, o si se la cambiaron.
                    if (reservaOriginal != null && reserva.IdHabitacion != null && reserva.IdHabitacion != reservaOriginal.IdHabitacion)
                    {
                        // 1. Buscamos el NÚMERO REAL de la habitación vieja (si tenía)
                        string habVieja = "Sin asignar";
                        if (reservaOriginal.IdHabitacion != null)
                        {
                            var cuartoViejo = await _context.Habitacion.FindAsync(reservaOriginal.IdHabitacion);
                            if (cuartoViejo != null) habVieja = cuartoViejo.Numero.ToString();
                        }

                        // 2. Buscamos el NÚMERO REAL de la habitación nueva
                        var cuartoNuevo = await _context.Habitacion.FindAsync(reserva.IdHabitacion);
                        string habNueva = cuartoNuevo != null ? cuartoNuevo.Numero.ToString() : reserva.IdHabitacion.ToString();

                        // 3. Armamos el JSON para la vista
                        string jsonViejo = "{\"IdHabitacion\": \"" + habVieja + "\"}";
                        string jsonNuevo = "{\"IdHabitacion\": \"" + habNueva + "\"}";

                        string usuarioActual = SesionUsuario.Instancia.Email ?? "Recepcionista";

                        var auditoriaHabitacion = new AuditoriaReserva 
                        {
                            IdReserva = reserva.IdReserva,
                            EmailUsuario = usuarioActual,
                            FechaHora = DateTime.Now,
                            TipoOperacion = "MODIFICACIÓN",
                            ValoresOriginales = jsonViejo,
                            ValoresNuevos = jsonNuevo
                        };
                        
                        _context.Add(auditoriaHabitacion); 
                    }

                    // 5. RECALCULAR PRECIO Y GUARDAR
                    var tipoHab = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);
                    if (tipoHab != null) {
                        reserva.PrecioTotal = CalcularPresupuesto(reserva.FechaInicio, reserva.FechaFin, tipoHab.PrecioBase);
                    }

                    _context.Update(reserva);
                    await _context.SaveChangesAsync();
                    
                    TempData["Success"] = "¡Reserva actualizada correctamente!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservaExists(reserva.IdReserva)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            
            await RecargarCombosEditFiltrado(reserva); 
            if (reserva.Huesped == null) 
                reserva.Huesped = await _context.Huesped.FindAsync(reserva.IdPersona);

            if (reserva.TipoHabitacion == null) 
                reserva.TipoHabitacion = await _context.TipoHabitacion.FindAsync(reserva.IdTipoHabitacion);

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
            bool puedeModificar = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                      SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            if (!puedeModificar) 
            {
                TempData["Error"] = "Acceso Denegado: Tu perfil es de solo lectura y no permite modificaciones.";
                return RedirectToAction(nameof(Index));
            }

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
            bool puedeModificar = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                      SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");

            if (!puedeModificar) 
            {
                TempData["Error"] = "Acceso Denegado: Tu perfil es de solo lectura y no permite modificaciones.";
                return RedirectToAction(nameof(Index));
            }
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
            var reserva = await _context.Reserva.FindAsync(id);
            if (reserva == null) return NotFound();

            // 1. REGLA DE SEGURIDAD ACTUALIZADA
            // Usamos las patentes operativas maestras en lugar del permiso individual
            bool tienePermisoStaff = SesionUsuario.Instancia.TienePermiso("Gestionar Huespedes") || 
                                    SesionUsuario.Instancia.TienePermiso("Gestionar Usuarios");
                                    
            // Mantenemos esto por si en el futuro querés que el huésped cancele sus propias reservas
            bool esDueñoDeLaReserva = reserva.IdUsuario == SesionUsuario.Instancia.IdUsuario;

            if (!tienePermisoStaff && !esDueñoDeLaReserva)
            {
                TempData["Error"] = "Acceso denegado: No tienes permisos para cancelar esta reserva.";
                return RedirectToAction(nameof(Index));
            }

            // 2. REGLA DE ESTADO: ¿Se puede cancelar?
            if (reserva.Estado != "Pendiente" && reserva.Estado != "Confirmada")
            {
                TempData["Error"] = "Solo se pueden cancelar reservas que estén Pendientes o Confirmadas.";
                return RedirectToAction(nameof(Index));
            }

            // 3. EJECUCIÓN: Cancelamos y liberamos la habitación
            reserva.Estado = "Cancelada";
            reserva.IdHabitacion = null; 

            _context.Update(reserva);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reserva #" + id + " cancelada correctamente. La habitación ha sido liberada.";
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