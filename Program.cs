using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad;
using SistemaReserva.Patters;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("SistemaReservaContext");
builder.Services.AddDbContext<SistemaReservaContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// EL SEEDER 
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SistemaReservaContext>();

    // Solo ejecutamos si la tabla de componentes está vacía
    if (!context.Componente.Any())
    {
        // A. Creamos Patentes (Hojas del Composite)
        var patVerReservas = new Patente { Nombre = "Ver Reservas" };
        var patCrearReserva = new Patente { Nombre = "Crear Reserva" };
        var patCancelarReserva = new Patente { Nombre = "Cancelar Reserva" };
        var patModificarReserva = new Patente { Nombre = "Modificar Reserva" };
        var patGestionarHabitaciones = new Patente { Nombre = "Gestionar Habitaciones" };
        var patGestionarUsuarios = new Patente { Nombre = "Gestionar Usuarios" };
        var patGestionarHuespedes = new Patente { Nombre = "Gestionar Huespedes" };
        var patCheckIn = new Patente { Nombre = "CheckIn" };
        var patCheckOut = new Patente { Nombre = "CheckOut" };

        // B. Creamos Familias (Compuestos del Composite)
        var familiaAdmin = new Familia { Nombre = "Administrador Global" };
        var familiaRecepcion = new Familia { Nombre = "Recepcion" };
        var familiaHuesped = new Familia { Nombre = "Huesped" };

        // C. Guardamos primero para obtener los IDs
        context.Componente.AddRange(patVerReservas, patCrearReserva, patCancelarReserva, patModificarReserva, patGestionarHabitaciones, patGestionarUsuarios, familiaRecepcion, familiaAdmin, familiaHuesped);
        context.SaveChanges();

        // D. Armamos la Jerarquía (Recursividad)

        // --- PERMISOS PARA EL HUÉSPED ---
        familiaHuesped.Agregar(patVerReservas);
        familiaHuesped.Agregar(patCrearReserva); 
        familiaHuesped.Agregar(patCancelarReserva);

        // Agregamos Ver Reservas a Recepción
        familiaRecepcion.Agregar(familiaHuesped); 
        familiaRecepcion.Agregar(patModificarReserva);
        familiaRecepcion.Agregar(patGestionarHuespedes);
        familiaRecepcion.Agregar(patCheckIn);
        familiaRecepcion.Agregar(patCheckOut);

        // El Admin hereda TODO lo de Recepción (incluyendo Ver Reservas)
        familiaAdmin.Agregar(familiaRecepcion); 
        familiaAdmin.Agregar(patGestionarHabitaciones);
        familiaAdmin.Agregar(patGestionarUsuarios);
        familiaAdmin.Agregar(patCheckIn);
        familiaAdmin.Agregar(patCheckOut);

        // Forzamos a EF a que vea que las familias cambiaron
        context.Entry(familiaRecepcion).State = EntityState.Modified;
        context.Entry(familiaAdmin).State = EntityState.Modified;

        // Guardamos las relaciones en la tabla intermedia
        context.SaveChanges();

        // E. Creamos el Usuario Admin
        // Dentro de la lógica de creación de usuarios en el Seeder
        if (!context.Usuario.Any(u => u.Email == "admin@argentower.com"))
        {
            // El Administrador de uso diario
            var adminUser = new Usuario {
                Email = "admin@argentower.com",
                Password = Encriptador.GenerarHash("Admin123"),
                Grupos = new List<Familia> { familiaAdmin },
                PreguntaSeguridad = "¿Nombre del hotel?",
                RespuestaSeguridad = "ArgenTower"
            };

            // El Usuario Root de emergencia
            var rootUser = new Usuario {
                Email = "root@argentower.com",
                Password = Encriptador.GenerarHash("RootEmergency2026"),
                Grupos = new List<Familia> { familiaAdmin },
                PreguntaSeguridad = "¿Nombre del hotel?",
                RespuestaSeguridad = "ArgenTower"
            };

            context.Usuario.AddRange(adminUser, rootUser);
            context.SaveChanges();
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();