using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models.Seguridad;
using SistemaReserva.Patters;
using System.Text.Json;

namespace SistemaReserva.Models
{
    public class SistemaReservaContext : DbContext
    {
        public SistemaReservaContext(DbContextOptions<SistemaReservaContext> opciones) : base(opciones) { }

        public DbSet<Persona> Persona { get; set; }
        public DbSet<Huesped> Huesped { get; set; }
        public DbSet<Recepcionista> Recepcionista { get; set; }
        public DbSet<TipoHabitacion> TipoHabitacion { get; set; }
        public DbSet<Habitacion> Habitacion { get; set; }
        public DbSet<Reserva> Reserva { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Cobro> Cobro { get; set; }
        public DbSet<Componente> Componente { get; set; }
        public DbSet<AuditoriaReserva> AuditoriaReserva { get; set; }
        public DbSet<AuditoriaSesion> AuditoriaSesion { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configuración de tablas existentes
            modelBuilder.Entity<Persona>()
                .HasDiscriminator<string>("TipoPersona")
                .HasValue<Huesped>("Huesped")
                .HasValue<Recepcionista>("Recepcionista")
                .HasValue<Persona>("Base");
            
            modelBuilder.Entity<TipoHabitacion>().Property(t => t.PrecioBase).HasColumnType("decimal(8,2)");
            modelBuilder.Entity<Habitacion>().ToTable("Habitacion");
            modelBuilder.Entity<Reserva>().ToTable("Reserva");

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. CONFIGURACIÓN DEL PATRÓN COMPOSITE (Recursividad)
            // Esto crea la tabla intermedia que permite que una Familia tenga Patentes u otras Familias
            modelBuilder.Entity<Componente>()
                .HasMany(p => p.Hijos)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "PermisoRelacion", // Nombre de la tabla intermedia en SQL
                    j => j.HasOne<Componente>().WithMany().HasForeignKey("HijoId"),
                    j => j.HasOne<Componente>().WithMany().HasForeignKey("PadreId")
                );

            // Configuramos TPH (Table Per Hierarchy) para los Componentes
            modelBuilder.Entity<Componente>()
                .HasDiscriminator<string>("TipoComponente")
                .HasValue<Patente>("Patente")
                .HasValue<Familia>("Familia");

            // 4. Otras configuraciones

            var dateOnlyConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateOnly, DateTime>(
                dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                dateTime => DateOnly.FromDateTime(dateTime));

            modelBuilder.Entity<Persona>().Property(p => p.FechaNacimiento).HasConversion(dateOnlyConverter);
        }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. CAPTURA (ANTES DE GUARDAR): 
            // Buscamos todas las Reservas que estén siendo creadas, modificadas o eliminadas.
            var reservasModificadas = ChangeTracker.Entries<Reserva>()
                .Where(e => e.State == EntityState.Added || 
                            e.State == EntityState.Modified || 
                            e.State == EntityState.Deleted)
                .Select(e => new 
                {
                    Entidad = e.Entity,
                    EstadoAccion = e.State,
                    // Clonamos los datos planos (sin tablas relacionadas) para evitar errores de ciclo JSON
                    ValoresViejos = e.State == EntityState.Added ? null : e.OriginalValues.Clone().ToObject(),
                    ValoresNuevos = e.State == EntityState.Deleted ? null : e.CurrentValues.Clone().ToObject()
                })
                .ToList();

            // 2. GUARDADO REAL: 
            // Ejecutamos el guardado normal. Si es una reserva nueva, acá SQL le asigna el IdReserva definitivo.
            var resultado = await base.SaveChangesAsync(cancellationToken);

            // 3. AUDITORÍA (DESPUÉS DE GUARDAR):
            // Si hubo cambios en reservas, generamos su huella de auditoría.
            if (reservasModificadas.Any())
            {
                // Interceptamos quién hizo el clic usando tu Singleton
                string usuarioActual = SesionUsuario.Instancia.Email ?? "Sistema/Desconocido";

                foreach (var item in reservasModificadas)
                {
                    // 1. Convertimos a texto PRIMERO
                    string jsonOriginal = item.ValoresViejos != null ? JsonSerializer.Serialize(item.ValoresViejos) : "N/A";
                    string jsonNuevo = item.ValoresNuevos != null ? JsonSerializer.Serialize(item.ValoresNuevos) : "N/A";

                    // 2. LA MAGIA: Si es una modificación, pero los JSON son idénticos, lo ignoramos
                    if (item.EstadoAccion == EntityState.Modified && jsonOriginal == jsonNuevo)
                    {
                        continue; 
                    }

                    // 3. Si pasó el filtro, armamos y guardamos la auditoría
                    var auditoria = new AuditoriaReserva
                    {
                        IdReserva = item.Entidad.IdReserva,
                        EmailUsuario = usuarioActual,
                        FechaHora = DateTime.Now,
                        TipoOperacion = item.EstadoAccion switch 
                        {
                            EntityState.Added => "CREACIÓN",
                            EntityState.Modified => "MODIFICACIÓN",
                            EntityState.Deleted => "ELIMINACIÓN",
                            _ => "DESCONOCIDA"
                        },
                        ValoresOriginales = jsonOriginal,
                        ValoresNuevos = jsonNuevo
                    };

                    AuditoriaReserva.Add(auditoria);
                }

                // Guardamos el historial (Esto no entra en bucle infinito porque ChangeTracker ya limpió los estados de Reserva)
                await base.SaveChangesAsync(cancellationToken);
            }

            return resultado;
        }
    }
}