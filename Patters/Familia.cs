using System.Collections.Generic;
using System.Linq;

namespace SistemaReserva.Models.Seguridad
{
    public class Familia : Componente
    {
        private List<Componente> _hijos = new List<Componente>();
        public virtual ICollection<Usuario> Usuarios { get; set; }

        public Familia()
        {
            Usuarios = new List<Usuario>();
        }

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

        //Aquí sucede la recursividad
        public override bool TienePermiso(string nombre)
        {
            foreach (var hijo in _hijos)
            {
                if (hijo.Nombre == nombre || hijo.TienePermiso(nombre))
                {
                    return true;
                }
            }
            return false;
        }

    }
}