using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Patters;
using SistemaReserva.Patters.Strategy;
using SistemaReserva.Utils;

namespace SistemaReserva.Controllers
{
    public class CobroController : Controller
    {
        private readonly SistemaReservaContext _context;

        public CobroController(SistemaReservaContext context)
        {
            _context = context;
        }

        // GET: /Cobro/Pagar?idReserva=5
        [HttpGet]
        public async Task<IActionResult> Pagar(int idReserva)
        {
            var reserva = await _context.Reserva
                .Include(r => r.TipoHabitacion)
                .Include(r => r.Huesped)
                .FirstOrDefaultAsync(r => r.IdReserva == idReserva);

            if (reserva == null)
            {
                TempData["Error"] = "Reserva no encontrada.";
                return RedirectToAction("Index", "Home");
            }

            // Si ya tiene un cobro asociado, no lo dejamos pagar dos veces
            bool yaPagado = await _context.Cobro.AnyAsync(c => c.IdReserva == idReserva);
            if (yaPagado)
            {
                TempData["Success"] = "Esta reserva ya cuenta con un pago o garantía registrada.";
                return RedirectToAction("Index", "Reserva");
            }

            // ====================================================================
            // 👇 NUEVO: Obtenemos la cotización y la pasamos por el ViewBag
            // ====================================================================
            var dolarService = new DolarService();
            ViewBag.CotizacionDolar = await dolarService.ObtenerCotizaciónBlue();

            return View(reserva);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(int IdReserva, string TipoOperacion, string NumeroTarjeta, string CVV, string Moneda = "ARS")
        {
            var reserva = await _context.Reserva.FindAsync(IdReserva);
            if (reserva == null) return NotFound();

            // 1. VALIDACIÓN DE SEGURIDAD PARA DÓLARES (CORREGIDA)
            if (Moneda == "USD")
            {
                // A. Solo permitimos Efectivo o Tarjetas
                if (TipoOperacion != "Efectivo" && TipoOperacion != "PagoTotalDebito" && TipoOperacion != "GarantiaCredito" && TipoOperacion != "PagoTarjeta")
                {
                    TempData["Error"] = "Operación rechazada: Los pagos en dólares solo pueden realizarse en Efectivo o con Tarjeta.";
                    return RedirectToAction("Pagar", new { idReserva = IdReserva });
                }

                // B. Si es Efectivo, verificamos si ya tenía tarjeta o si la está ingresando ahora
                if (TipoOperacion == "Efectivo")
                {
                    bool tieneGarantiaPrevia = await _context.Cobro.AnyAsync(c => c.IdReserva == IdReserva && c.Estado == "En Garantía");
                    bool ingresoTarjetaAhora = !string.IsNullOrEmpty(NumeroTarjeta);

                    if (!tieneGarantiaPrevia && !ingresoTarjetaAhora)
                    {
                        TempData["Error"] = "Para abonar en Dólares en Efectivo, debe ingresar una Tarjeta de Crédito en el formulario o tener una previamente registrada.";
                        return RedirectToAction("Pagar", new { idReserva = IdReserva });
                    }

                    if (ingresoTarjetaAhora)
                    {
                        var tipoTarjetaIngresada = DetectorTarjeta.IdentificarTipo(NumeroTarjeta);
                        if (tipoTarjetaIngresada == TipoTarjeta.Debito)
                        {
                            TempData["Error"] = "La tarjeta ingresada como garantía para el pago en efectivo debe ser de CRÉDITO.";
                            return RedirectToAction("Pagar", new { idReserva = IdReserva });
                        }
                    }
                }
            }

            var datosPago = new Dictionary<string, string>
            {
                { "NumeroTarjeta", NumeroTarjeta ?? "" },
                { "CVV", CVV ?? "" }
            };

            var procesador = new ProcesadorDePagos();
            TipoTarjeta tipoTarjeta = TipoTarjeta.Desconocida;
            
            if (!string.IsNullOrEmpty(NumeroTarjeta))
            {
                tipoTarjeta = DetectorTarjeta.IdentificarTipo(NumeroTarjeta);
            }

            switch (TipoOperacion)
            {
                case "GarantiaCredito":
                    if (tipoTarjeta == TipoTarjeta.Desconocida)
                    {
                        TempData["Error"] = "Número de tarjeta no reconocido. Ingrese una tarjeta Visa, Mastercard o Amex válida.";
                        return RedirectToAction("Pagar", new { idReserva = IdReserva });
                    }
                    
                    if (tipoTarjeta == TipoTarjeta.Debito)
                    {
                        TempData["Error"] = "Operación rechazada: Las reservas en garantía solo pueden realizarse con Tarjetas de Crédito.";
                        return RedirectToAction("Pagar", new { idReserva = IdReserva });
                    }

                    procesador.EstablecerEstrategia(new EstrategiaGarantiaTarjeta());
                    break;

                case "PagoTotalDebito":
                case "PagoTarjeta":
                    if (tipoTarjeta == TipoTarjeta.Desconocida)
                    {
                        TempData["Error"] = "Número de tarjeta no reconocido. Ingrese una tarjeta Visa, Mastercard o Amex válida.";
                        return RedirectToAction("Pagar", new { idReserva = IdReserva });
                    }

                    if (TipoOperacion == "GarantiaCredito" && tipoTarjeta == TipoTarjeta.Debito)
                    {
                        TempData["Error"] = "Operación rechazada: Las reservas en garantía solo pueden realizarse con Tarjetas de Crédito.";
                        return RedirectToAction("Pagar", new { idReserva = IdReserva });
                    }

                    if (tipoTarjeta == TipoTarjeta.Credito)
                    {
                        procesador.EstablecerEstrategia(new EstrategiaPagoTarjeta());
                    }
                    else if (tipoTarjeta == TipoTarjeta.Debito)
                    {
                        procesador.EstablecerEstrategia(new EstrategiaPagoDebito());
                    }
                    break;

                case "Transferencia":
                    procesador.EstablecerEstrategia(new EstrategiaPagoTransferencia());
                    break;

                case "PagoModo":
                    procesador.EstablecerEstrategia(new EstrategiaPagoModo());
                    break;

                case "Efectivo":
                    procesador.EstablecerEstrategia(new EstrategiaPagoEfectivo()); 
                    break;

                default:
                    TempData["Error"] = "Método de pago no válido.";
                    return RedirectToAction("Pagar", new { idReserva = IdReserva });
            }

            ResultadoPago resultado = procesador.EjecutarCobro(reserva, reserva.PrecioTotal, datosPago);

            if (!resultado.Exito)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction("Pagar", new { idReserva = IdReserva });
            }

            // =========================================================
            // 2. INYECCIÓN DE DATOS BIMONETARIOS ANTES DE GUARDAR
            // =========================================================
            var cobroAGuardar = resultado.CobroGenerado;
            cobroAGuardar.MonedaPago = Moneda; 

            if (Moneda == "USD")
            {
                var dolarService = new DolarService();
                decimal cotizacionActual = await dolarService.ObtenerCotizaciónBlue();
                
                cobroAGuardar.CotizacionAplicada = cotizacionActual;
                
                // Evitamos división por cero por seguridad
                if (cotizacionActual > 0) 
                {
                    cobroAGuardar.MontoEnDolares = cobroAGuardar.MontoTotal / cotizacionActual;
                }
            }
            // =========================================================

            _context.Cobro.Add(cobroAGuardar);
            
            reserva.Estado = "Confirmada"; 
            _context.Update(reserva);

            await _context.SaveChangesAsync();

            TempData["Success"] = resultado.Mensaje;
            
            // Le pasamos el ID del Cobro (no de la reserva) para que el comprobante levante el registro exacto
            return RedirectToAction("Comprobante", new { idReserva = cobroAGuardar.IdReserva });
        }

        // GET: /Cobro/Comprobante?idReserva=5
        [HttpGet]
        public async Task<IActionResult> Comprobante(int idReserva)
        {
            var cobro = await _context.Cobro
                .Include(c => c.Reserva)
                .ThenInclude(r => r.Huesped)
                .FirstOrDefaultAsync(c => c.IdReserva == idReserva);

            if (cobro == null) return RedirectToAction("Index", "Home");

            return View(cobro);
        }

        [HttpPost]
        public IActionResult AprobarTransferencia(int idCobro)
        {
            var cobro = _context.Cobro.Find(idCobro);
            
            if (cobro == null) 
            {
                TempData["Error"] = "No se encontró el registro del cobro.";
                return RedirectToAction("Index", "Reserva"); 
            }

            // 1. AUDITORÍA: Capturamos cómo estaba ANTES de cambiarlo
            string estadoViejo = cobro.Estado ?? "Pendiente";
            string jsonViejo = "{\"EstadoPago\": \"" + estadoViejo + "\"}";
            string jsonNuevo = "{\"EstadoPago\": \"Aprobado\"}";

            // Intentamos sacar el usuario de la sesión, si falla ponemos "Recepcionista"
            string usuarioActual = SesionUsuario.Instancia.Email ?? "Recepcionista";

            var auditoriaCobro = new AuditoriaReserva
            {
                IdReserva = cobro.IdReserva,
                EmailUsuario = usuarioActual,
                FechaHora = DateTime.Now,
                TipoOperacion = "MODIFICACIÓN",
                ValoresOriginales = jsonViejo,
                ValoresNuevos = jsonNuevo
            };
            
            _context.Add(auditoriaCobro);

            // 2. El recepcionista validó el pago, lo pasamos al estado que lee el reporte
            cobro.Estado = "Aprobado";
            
            _context.SaveChanges();

            TempData["Mensaje"] = "Pago aprobado. La recaudación ha sido actualizada y el movimiento auditado.";
            return RedirectToAction("Index", "Reserva");
        }

        [HttpPost]
        public IActionResult EjecutarCobroGarantia(int idCobro)
        {
            var cobro = _context.Cobro.Find(idCobro);
            
            if (cobro == null) return RedirectToAction("Index", "Reserva");

            // 1. Lo pasamos al estado que el reporte detecta
            cobro.Estado = "Aprobado";
            
            // 2. ACTUALIZAMOS LA FECHA A HOY. 
            // Esto es clave para que los pesos entren en la recaudación del mes actual
            cobro.FechaCobro = DateTime.Now;

            // Armamos un JSON ficticio usando las mismas claves del "traductor" de la vista
            string jsonViejo = "{\"EstadoPago\": \"En Garantía\"}";
            string jsonNuevo = "{\"EstadoPago\": \"Aprobado\"}";
            string usuarioActual = SesionUsuario.Instancia.Email ?? "Recepcionista";

            var auditoriaCobro = new AuditoriaReserva
            {
                IdReserva = cobro.IdReserva,
                EmailUsuario = usuarioActual,
                FechaHora = DateTime.Now,
                TipoOperacion = "MODIFICACIÓN",
                ValoresOriginales = jsonViejo,
                ValoresNuevos = jsonNuevo
            };
            _context.Add(auditoriaCobro); 

            _context.SaveChanges();

            TempData["Mensaje"] = "Pago con tarjeta aprobado correctamente.";
            return RedirectToAction("Index", "Reserva");
        }
    }
}