using ClinicaApp.ViewModels;
using ClinicaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers
{
    [Authorize(Roles = "Secretaria,Doctora")]
    public sealed class PacientesController(ClinicDataStore clinicData) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Buscar(string? termino)
        {
            return View(new PatientSearchViewModel
            {
                Term = termino,
                Patients = await clinicData.SearchPatientsAsync(termino)
            });
        }

        [HttpGet]
        [Authorize(Roles = "Secretaria")]
        public IActionResult Registrar()
        {
            return View(new RegistrarPacienteViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Secretaria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(RegistrarPacienteViewModel modelo)
        {
            if (modelo.FechaNacimiento == default)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaNacimiento),
                    "La fecha de nacimiento es obligatoria.");
            }
            else if (modelo.FechaNacimiento.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(modelo.FechaNacimiento),
                    "La fecha de nacimiento no puede ser futura.");
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var patientId = await clinicData.CreatePatientAsync(modelo);
            TempData["Mensaje"] = $"Paciente {modelo.NombreCompleto.Trim()} registrado correctamente.";
            return RedirectToAction(nameof(Expediente), new { id = patientId });
        }

        [HttpGet]
        public async Task<IActionResult> Expediente(int id)
        {
            var chart = await clinicData.GetPatientChartAsync(id);
            if (chart is null)
            {
                return NotFound();
            }

            return View(chart);
        }

        [HttpPost]
        [Authorize(Roles = "Doctora")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAntecedente(AntecedentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Completa la categoría y descripción del antecedente.";
                return RedirectToAction(nameof(Expediente), new { id = model.PatientId });
            }

            if (!await clinicData.AddAntecedentAsync(model))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "El antecedente médico se agregó al expediente.";
            return RedirectToAction(nameof(Expediente), new { id = model.PatientId });
        }
    }
}