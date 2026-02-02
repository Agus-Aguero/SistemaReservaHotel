using SistemaReserva.Models;
using SistemaReserva.Models.Seguridad; // Asegúrate de que este sea el namespace de tu Composite

namespace SistemaReserva.Patters
{
    public class SesionUsuario
    {
        private static SesionUsuario? _instancia;
        
        // Propiedades de la sesión
        public int IdUsuario { get; set; }
        public string? Email { get; private set; }
        public Componente? Perfil { get; private set; } // El Composite completo
        public DateTime FechaLogin { get; private set; }

        private SesionUsuario() { } // Constructor privado para el Singleton

        public static SesionUsuario Instancia
        {
            get
            {
                if (_instancia == null) _instancia = new SesionUsuario();
                return _instancia;
            }
        }

        // Método de entrada al sistema
        public void Login(int id, string email, Componente perfil)
        {
            this.IdUsuario = id;
            this.Email = email;
            this.Perfil = perfil;
        }

        // Método de salida
        public void Logout()
        {
            Email = null;
            Perfil = null;
        }

        // --- MÉDOTO CLAVE PARA EL T04 (Recursividad) ---
        // Este método permite preguntar en cualquier lado: 
        // if (SesionUsuario.Instancia.TienePermiso("CrearReserva")) { ... }
        public bool TienePermiso(string nombrePermiso)
        {
            if (Perfil == null) return false;
            return ValidarRecursivo(Perfil, nombrePermiso);
        }

        private bool ValidarRecursivo(Componente comp, string nombre)
        {
            // Si el nombre coincide (sea Familia o Patente), tiene el permiso
            if (comp.Nombre == nombre) return true;

            // Si no coincide, buscamos recursivamente en sus hijos
            foreach (var hijo in comp.Hijos)
            {
                if (ValidarRecursivo(hijo, nombre)) return true;
            }
            return false;
        }
    }
}