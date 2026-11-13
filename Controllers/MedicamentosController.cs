using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Controllers
{
    [Authorize]
    [Route("Medicamentos")]
    public class MedicamentosController(ClinicaDbContext context) : Controller
    {
        // 3.2 CONSULTAR REGISTROS (SELECT en SQL Server)
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? buscar)
        {
            ViewData["FiltroActual"] = buscar;
            var consulta = context.Medicamentos.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                consulta = consulta.Where(m => m.Nombre.Contains(buscar.Trim()));
            }

            var lista = await consulta.OrderBy(m => m.Nombre).ToListAsync();
            return View(lista);
        }

        // 3.1 CREAR REGISTROS (INSERT en SQL Server)
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new Medicamento());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicamento medicamento)
        {
            // 3.5 VALIDACIONES DE DATOS
            if (!string.IsNullOrWhiteSpace(medicamento.Nombre))
            {
                var nombreLimpio = medicamento.Nombre.Trim();
                bool existe = await context.Medicamentos
                    .AnyAsync(m => m.Nombre.ToLower() == nombreLimpio.ToLower());

                if (existe)
                {
                    ModelState.AddModelError(nameof(Medicamento.Nombre), "Ya existe un medicamento registrado con este nombre.");
                }
                else
                {
                    medicamento.Nombre = nombreLimpio;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(medicamento);
            }

            context.Medicamentos.Add(medicamento);
            await context.SaveChangesAsync();

            TempData["Exito"] = "Medicamento registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // 3.3 ACTUALIZAR REGISTROS (UPDATE en SQL Server)
        [HttpGet("Edit/{id:int?}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var medicamento = await context.Medicamentos.FindAsync(id);
            if (medicamento is null)
            {
                return NotFound();
            }

            return View(medicamento);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicamento medicamento)
        {
            if (id != medicamento.Id)
            {
                return NotFound();
            }

            // 3.5 VALIDACIONES DE DATOS
            if (!string.IsNullOrWhiteSpace(medicamento.Nombre))
            {
                var nombreLimpio = medicamento.Nombre.Trim();
                bool existe = await context.Medicamentos
                    .AnyAsync(m => m.Id != id && m.Nombre.ToLower() == nombreLimpio.ToLower());

                if (existe)
                {
                    ModelState.AddModelError(nameof(Medicamento.Nombre), "Ya existe otro medicamento con este nombre.");
                }
                else
                {
                    medicamento.Nombre = nombreLimpio;
                }
            }

            if (!ModelState.IsValid)
            {
                return View(medicamento);
            }

            try
            {
                context.Update(medicamento);
                await context.SaveChangesAsync();

                TempData["Exito"] = "Medicamento actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "El registro fue modificado por otro usuario.");
                return View(medicamento);
            }
        }

        // 3.4 ELIMINAR REGISTROS (DELETE en SQL Server)
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var medicamento = await context.Medicamentos.FindAsync(id);
            if (medicamento is null)
            {
                TempData["Error"] = "No se encontró el medicamento que intentas eliminar.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                context.Medicamentos.Remove(medicamento);
                await context.SaveChangesAsync();
                TempData["Exito"] = "Medicamento eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar este medicamento porque está asociado a recetas o consultas registradas.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}