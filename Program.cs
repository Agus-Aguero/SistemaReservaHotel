using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad;
using SistemaReserva.Patters;

// 1. Declaración del BUILDER (Esto es lo que te falta)
var builder = WebApplication.CreateBuilder(args);

// 2. Configuración de Servicios (Inyección de dependencias)
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("SistemaReservaContext");
builder.Services.AddDbContext<SistemaReservaContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Creación de la APP (Aquí es donde builder se transforma en app)
var app = builder.Build();

// 4. EL SEEDER (Implementación del Patrón Composite T04)
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

        // B. Creamos Familias (Compuestos del Composite)
        var familiaAdmin = new Familia { Nombre = "Administrador Global" };
        var familiaRecepcion = new Familia { Nombre = "Recepcion" };
        var familiaHuesped = new Familia { Nombre = "Huesped" };

        // C. Guardamos primero para obtener los IDs
        context.Componente.AddRange(patVerReservas, patCrearReserva, patCancelarReserva, patModificarReserva, patGestionarHabitaciones, patGestionarUsuarios, familiaRecepcion, familiaAdmin, familiaHuesped);
        context.SaveChanges();

        // D. Armamos la Jerarquía (Recursividad)

        // --- PERMISOS PARA EL HUÉSPED ---
        familiaHuesped.Agregar(patVerReservas);    // Para ver su historial
        familiaHuesped.Agregar(patCrearReserva);   // Para hacer reservas nuevas
        familiaHuesped.Agregar(patCancelarReserva); // Para anular sus reservas

        // Agregamos Ver Reservas a Recepción
        familiaRecepcion.Agregar(familiaHuesped);  // Así el recepcionista también puede crear/ver/cancelar
        familiaRecepcion.Agregar(patModificarReserva);

        // El Admin hereda TODO lo de Recepción (incluyendo Ver Reservas)
        familiaAdmin.Agregar(familiaRecepcion); 
        familiaAdmin.Agregar(patGestionarHabitaciones);
        familiaAdmin.Agregar(patGestionarUsuarios);

        // Forzamos a EF a que vea que las familias cambiaron
        context.Entry(familiaRecepcion).State = EntityState.Modified;
        context.Entry(familiaAdmin).State = EntityState.Modified;

        // Guardamos las relaciones en la tabla intermedia
        context.SaveChanges();

        // E. Creamos el Usuario Admin
        if (!context.Usuario.Any())
        {
            context.Usuario.Add(new Usuario
            {
                Email = "admin@hotel.com",
                Password = Encriptador.GenerarHash("admin123"),
                Perfil = familiaAdmin 
            });
            context.SaveChanges();
        }
    }
}

// 5. Configuración del Pipeline (Middleware)
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

// 6. Ejecución
app.Run();