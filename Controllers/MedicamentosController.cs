using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Controllers
{
    [Authorize]
    public class MedicamentosController(ClinicaDbContext context) : Controller
    {
        // 3.2 CONSULTAR REGISTROS (SELECT en SQL Server)
        [HttpGet]
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
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Medicamento());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicamento medicamento)
        {
            // 3.5 VALIDACIONES DE DATOS
            if (!string.IsNullOrWhiteSpace(medicamento.Nombre))
            {
                medicamento.Nombre = medicamento.Nombre.Trim();
                bool existe = await context.Medicamentos
                    .AnyAsync(m => m.Nombre.ToLower() == medicamento.Nombre.ToLower());

                if (existe)
                {
                    ModelState.AddModelError(nameof(medicamento.Nombre), "Este medicamento ya está registrado.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(medicamento);
            }

            // 3.6 MANEJO DE ERRORES
            try
            {
                context.Medicamentos.Add(medicamento);
                await context.SaveChangesAsync();
                TempData["Exito"] = "Medicamento creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Error al guardar en SQL Server: " + (ex.InnerException?.Message ?? ex.Message));
                return View(medicamento);
            }
        }

        // 3.3 ACTUALIZAR REGISTROS (UPDATE en SQL Server)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var medicamento = await context.Medicamentos.FindAsync(id);
            if (medicamento == null) return NotFound();

            return View(medicamento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicamento medicamento)
        {
            if (id != medicamento.Id) return BadRequest();

            // 3.5 VALIDACIONES DE DATOS
            if (!string.IsNullOrWhiteSpace(medicamento.Nombre))
            {
                medicamento.Nombre = medicamento.Nombre.Trim();
                bool duplicado = await context.Medicamentos
                    .AnyAsync(m => m.Id != id && m.Nombre.ToLower() == medicamento.Nombre.ToLower());

                if (duplicado)
                {
                    ModelState.AddModelError(nameof(medicamento.Nombre), "Ya existe otro medicamento con ese nombre.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(medicamento);
            }

            // 3.6 MANEJO DE ERRORES
            try
            {
                var existente = await context.Medicamentos.FindAsync(id);
                if (existente == null) return NotFound();

                existente.Nombre = medicamento.Nombre;
                await context.SaveChangesAsync();

                TempData["Exito"] = "Medicamento actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "El registro fue modificado por otro usuario.");
                return View(medicamento);
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError(string.Empty, "Error al actualizar en SQL Server: " + (ex.InnerException?.Message ?? ex.Message));
                return View(medicamento);
            }
        }

        // 3.4 ELIMINAR REGISTROS (DELETE en SQL Server)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // 3.6 MANEJO DE ERRORES
            try
            {
                var medicamento = await context.Medicamentos.FindAsync(id);
                if (medicamento == null)
                {
                    TempData["Error"] = "El medicamento no existe o ya fue eliminado.";
                    return RedirectToAction(nameof(Index));
                }

                context.Medicamentos.Remove(medicamento);
                await context.SaveChangesAsync();

                TempData["Exito"] = "Medicamento eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar el medicamento porque está asociado a recetas médicas existentes.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}