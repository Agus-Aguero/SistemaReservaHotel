// scripts.js para ArgenTower
document.addEventListener("DOMContentLoaded", function () {
    const configElem = document.getElementById("reservaConfig");
    
    // Verificamos que existan los datos de configuración y los inputs
    if (configElem) {
        const preciosBase = JSON.parse(configElem.getAttribute("data-precios"));
        const cotizacionDolar = parseFloat(configElem.getAttribute("data-cotizacion"));

        // Usamos querySelector por nombre para asegurar la compatibilidad con ASP.NET
        const inputTipo = document.querySelector("[name='IdTipoHabitacion']");
        const inputInicio = document.querySelector("[name='FechaInicio']");
        const inputFin = document.querySelector("[name='FechaFin']");

        // Si los inputs existen, configuramos la lógica
        if (inputTipo && inputInicio && inputFin) {
            
            function calcularTotal() {
                const tipoId = inputTipo.value;
                const fechaIn = new Date(inputInicio.value + "T00:00:00");
                const fechaOut = new Date(inputFin.value + "T00:00:00");
                
                // Debug en consola para verificar valores
                console.log("Calculando presupuesto para ArgenTower...");

                if (tipoId && !isNaN(fechaIn) && !isNaN(fechaOut) && fechaOut > fechaIn) {
                    const diferencia = fechaOut - fechaIn;
                    const noches = Math.ceil(diferencia / (1000 * 60 * 60 * 24));
                    
                    // ... dentro de la función calcularTotal ...

                    const habitacion = preciosBase.find(p => p.IdTipoHabitacion == tipoId);

                    if (habitacion) {
                        // Intentamos leer PrecioBase (C#) o precioBase (JSON estándar)
                        const costoNoche = habitacion.PrecioBase || habitacion.precioBase;

                        if (costoNoche) {
                            const totalARS = noches * costoNoche;
                            const totalUSD = totalARS / cotizacionDolar;

                            // Actualizamos la vista de ArgenTower
                            document.getElementById("resumenNoches").innerText = noches;
                            document.getElementById("resumenARS").innerText = new Intl.NumberFormat('es-AR', { 
                                style: 'currency', currency: 'ARS' 
                            }).format(totalARS);
                            document.getElementById("resumenUSD").innerText = "USD " + totalUSD.toFixed(2);
                        }
                    }
                } else {
                    // Resetear si los datos son incompletos o inválidos
                    document.getElementById("resumenNoches").innerText = "0";
                    document.getElementById("resumenARS").innerText = "$ 0,00";
                    document.getElementById("resumenUSD").innerText = "USD 0.00";
                }
            }

            // Escuchamos los cambios en los campos
            inputTipo.addEventListener("change", calcularTotal);
            inputInicio.addEventListener("change", calcularTotal);
            inputFin.addEventListener("change", calcularTotal);

            // EJECUCIÓN INICIAL: Fundamental para AGUERO
            calcularTotal();
        }
    }
});