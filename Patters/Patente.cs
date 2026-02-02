using System.Collections.Generic;

namespace SistemaReserva.Models.Seguridad
{
    public class Patente : Componente
    {
        public Patente() 
        { 
            // Inicializamos la lista para que no sea nula, 
            // aunque una patente nunca tendrá elementos aquí.
            Hijos = new List<Componente>(); 
        }

        // Implementación obligatoria de la base
        public override List<Componente> Hijos 
        { 
            get => new List<Componente>(); // Siempre devuelve una lista vacía
            set {  } 
        }

        // Una patente no permite agregar hijos
        public override void Agregar(Componente c) 
        {
            // se podria lanzar una excepción si alguien intenta agregar
            // pero para el TP con dejarlo vacío alcanza.
        }

        public override void Quitar(Componente c) { /* No aplica */ }

        // Lógica de validación: una patente solo tiene permiso si se llama igual a lo buscado
        public override bool TienePermiso(string nombre)
        {
            return this.Nombre == nombre;
        }
    }
}