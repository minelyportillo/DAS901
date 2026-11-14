using ClinicaApp.ViewModels;
using ClinicaApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers
{
    [Authorize(Roles = "Secretaria,Doctora")]
    public sealed class PacientesController(
        IClinicDataStore clinicData,
        IPacienteService pacienteService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Buscar(string? termino)
        {
            var patients = await pacienteService.BuscarAsync(termino);
            return View(new PatientSearchViewModel
            {
                Term = termino,
                Patients = patients
                    .Select(patient => new PatientListItem(
                        patient.Id,
                        patient.Nombre,
                        patient.FechaNacimiento,
                        patient.Dui))
                    .ToArray()
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
            ValidateBirthDate(modelo);

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var patient = await pacienteService.RegistrarPacienteAsync(modelo);
            if (patient is null)
            {
                ModelState.AddModelError(nameof(modelo.Dui), "Ya existe un paciente registrado con ese DUI.");
                return View(modelo);
            }

            TempData["Mensaje"] = $"Paciente {modelo.Nombre.Trim()} registrado correctamente.";
            return RedirectToAction(nameof(Expediente), new { id = patient.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Secretaria")]
        public async Task<IActionResult> Editar(int id)
        {
            var patient = await pacienteService.ObtenerPorIdAsync(id);
            if (patient is null)
            {
                return NotFound();
            }

            ViewData["PatientId"] = patient.Id;
            return View(new RegistrarPacienteViewModel
            {
                Nombre = patient.Nombre,
                FechaNacimiento = patient.FechaNacimiento,
                Dui = patient.Dui
            });
        }

        [HttpPost]
        [Authorize(Roles = "Secretaria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, RegistrarPacienteViewModel modelo)
        {
            ValidateBirthDate(modelo);
            if (!ModelState.IsValid)
            {
                ViewData["PatientId"] = id;
                return View(modelo);
            }

            var updated = await pacienteService.ActualizarPacienteAsync(id, modelo);
            if (!updated)
            {
                if (await pacienteService.ObtenerPorIdAsync(id) is null)
                {
                    return NotFound();
                }

                ModelState.AddModelError(nameof(modelo.Dui), "Ya existe otro paciente registrado con ese DUI.");
                ViewData["PatientId"] = id;
                return View(modelo);
            }

            TempData["Mensaje"] = "Los datos del paciente se actualizaron correctamente.";
            return RedirectToAction(nameof(Expediente), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Secretaria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (!await pacienteService.EliminarPacienteSinHistorialAsync(id))
            {
                if (await pacienteService.ObtenerPorIdAsync(id) is null)
                {
                    return NotFound();
                }

                TempData["Error"] = "No se puede eliminar un paciente que tiene historial clínico.";
                return RedirectToAction(nameof(Expediente), new { id });
            }

            TempData["Mensaje"] = "El paciente se eliminó correctamente.";
            return RedirectToAction(nameof(Buscar));
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

            if (!await pacienteService.AgregarAlergiaAsync(model))
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

            if (!await pacienteService.AgregarEnfermedadAsync(model))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La enfermedad se registró en el expediente del paciente.";
            return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
        }

        [HttpPost]
        [Authorize(Roles = "Doctora")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarAlergia(AlergiaPacienteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revisa los datos de la alergia.";
                return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
            }

            if (!await pacienteService.ActualizarAlergiaAsync(model))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La alergia se actualizó en el expediente.";
            return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
        }

        [HttpPost]
        [Authorize(Roles = "Doctora")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarAlergia(int id, int patientId, int? consultationId)
        {
            if (!await pacienteService.EliminarAlergiaAsync(id, patientId))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La alergia se eliminó del expediente.";
            return RedirectAfterPatientUpdate(patientId, consultationId);
        }

        [HttpPost]
        [Authorize(Roles = "Doctora")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEnfermedad(EnfermedadViewModel model)
        {
            if (model.AnioDiagnostico > DateTime.Today.Year)
            {
                ModelState.AddModelError(nameof(model.AnioDiagnostico), "El año de diagnóstico no puede ser futuro.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Revisa los datos de la enfermedad.";
                return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
            }

            if (!await pacienteService.ActualizarEnfermedadAsync(model))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La enfermedad se actualizó en el expediente.";
            return RedirectAfterPatientUpdate(model.PatientId, model.ConsultationId);
        }

        [HttpPost]
        [Authorize(Roles = "Doctora")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEnfermedad(int id, int patientId, int? consultationId)
        {
            if (!await pacienteService.EliminarEnfermedadAsync(id, patientId))
            {
                return NotFound();
            }

            TempData["Mensaje"] = "La enfermedad se eliminó del expediente.";
            return RedirectAfterPatientUpdate(patientId, consultationId);
        }

        private IActionResult RedirectAfterPatientUpdate(int patientId, int? consultationId)
        {
            if (consultationId.HasValue)
            {
                return RedirectToAction("Atender", "Consultas", new { id = consultationId.Value });
            }

            return RedirectToAction(nameof(Expediente), new { id = patientId });
        }

        private void ValidateBirthDate(RegistrarPacienteViewModel model)
        {
            if (model.FechaNacimiento == default)
            {
                ModelState.AddModelError(
                    nameof(model.FechaNacimiento),
                    "La fecha de nacimiento es obligatoria.");
            }
            else if (model.FechaNacimiento.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.FechaNacimiento),
                    "La fecha de nacimiento no puede ser futura.");
            }
        }
    }
}
