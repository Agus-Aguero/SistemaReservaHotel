using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaReserva.Models;

namespace SistemaReserva.Controllers
{
    [Route("[controller]")]
    public class PersonaController : Controller
    {
        private readonly ILogger<PersonaController> _logger;
        private readonly SistemaReservaContext _context; //Creamos una variable privada para context

        public PersonaController(SistemaReservaContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Persona.ToList());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}