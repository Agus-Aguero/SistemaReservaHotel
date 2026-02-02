using System.Collections.Generic;
using System.Linq;

namespace SistemaReserva.Models.Seguridad
{
    public class Familia : Componente
    {
        // Esta lista es la que guarda las Patentes o incluso otras Familias
        private List<Componente> _hijos = new List<Componente>();

        public Familia() { }

        // Propiedad de navegación para Entity Framework
        public override List<Componente> Hijos 
        { 
            get => _hijos; 
            set => _hijos = value; 
        }
        public override void Agregar(Componente c)
        {
            _hijos.Add(c);
        }

        public override void Quitar(Componente c)
        {
            _hijos.Remove(c);
        }

        // LA MAGIA: Aquí sucede la recursividad del T04
        public override bool TienePermiso(string nombre)
        {
            foreach (var hijo in _hijos)
            {
                // Si el hijo se llama igual O si el hijo (que puede ser otra familia) lo tiene
                if (hijo.Nombre == nombre || hijo.TienePermiso(nombre))
                {
                    return true;
                }
            }
            return false;
        }
    }
}