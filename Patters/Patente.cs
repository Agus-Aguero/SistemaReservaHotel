using System.Collections.Generic;

namespace SistemaReserva.Models.Seguridad
{
    public class Patente : Componente
    {
        public Patente() 
        { 
            Hijos = new List<Componente>(); 
        }

        public override List<Componente> Hijos 
        { 
            get => new List<Componente>();
            set {  } 
        }

        // Una patente no permite agregar hijos
        public override void Agregar(Componente c) 
        {
  
        }

        public override void Quitar(Componente c) { }

        // Lógica de validación: una patente solo tiene permiso si se llama igual a lo buscado
        public override bool TienePermiso(string nombre)
        {
            return this.Nombre == nombre;
        }
    }
}