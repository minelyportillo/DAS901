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
            TempData["Mensaje"] = $"Paciente {modelo.Nombre.Trim()} registrado correctamente.";
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
        public async Task<IActionResult> AgregarAlergia(AlergiaPacienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Completa el tipo de alergia, la sustancia/elemento y la severidad.";
                return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
            }

            if (!await clinicData.AddAlergiaAsync(model))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La alergia se registró en el expediente del paciente.";
            return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
        }

        [HttpPost]
        [Authorize(Roles = "Doctora")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarEnfermedad(EnfermedadViewModel model)
        {
            if (model.AnioDiagnostico > DateTime.Today.Year)
            {
                ModelState.AddModelError(nameof(model.AnioDiagnostico), "El año de diagnóstico no puede ser futuro.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Completa el nombre de la enfermedad y un año de diagnóstico válido.";
                return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
            }

            if (!await clinicData.AddEnfermedadAsync(model))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La enfermedad se registró en el expediente del paciente.";
            return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
        }

        private IActionResult RedirectAfterPatientUpdate(int patientId, int? consultationId)
        {
            if (consultationId.HasValue)
            {
                return RedirectToAction("Atender", "Consultas", new { id = consultationId.Value });
            }

            return RedirectToAction(nameof(Expediente), new { id = patientId });
        }
    }
}
