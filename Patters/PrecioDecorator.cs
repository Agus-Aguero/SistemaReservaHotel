namespace SistemaReserva.Patters
{
    public interface IPrecioDisplay {
        string Formatear(decimal monto);
    }

    // Componente Concreto (Pesos)
    public class PrecioPesosDisplay : IPrecioDisplay {
        public string Formatear(decimal monto) => monto.ToString("C2") + " ARS";
    }

    // Decorador Base
    public abstract class PrecioDecorator : IPrecioDisplay {
        protected IPrecioDisplay _decorado;
        public PrecioDecorator(IPrecioDisplay decorado) => _decorado = decorado;
        public abstract string Formatear(decimal monto);
    }

    // Decorador Concreto (Dólares)
    public class PrecioDolarDecorator : PrecioDecorator {
        private readonly decimal _cotizacion;
        public PrecioDolarDecorator(IPrecioDisplay decorado, decimal cotizacion) : base(decorado) 
        {
            _cotizacion = cotizacion;
        }
        public override string Formatear(decimal monto) {
            decimal totalDolar = monto / _cotizacion;
            return "USD " + totalDolar.ToString("N2");
        }
    }
}