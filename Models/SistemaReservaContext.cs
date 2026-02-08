using Microsoft.EntityFrameworkCore;
using SistemaReserva.Models.Seguridad; // Namespace donde están Componente, Familia y Patente

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
        
        // IMPORTANTES PARA EL COMPOSITE
        public DbSet<Componente> Componente { get; set; }

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
    }
}