using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaReserva.Models.Seguridad
{
    public abstract class Componente
    {
        [Key]
        [Column("IdComponente")]
        public int IdComponente { get; set; }
        public string Nombre { get; set; }
        public abstract List<Componente> Hijos { get; set;}
        public abstract void Agregar(Componente c);
        public abstract void Quitar(Componente c);
        public abstract bool TienePermiso(string nombre);
    }
}