using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers;

[Authorize(Roles = "Doctora")]
public sealed class ConsultasController(ClinicDataStore clinicData) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Atender(int id)
    {
        var data = await clinicData.StartConsultationAsync(id);
        if (data is null)
        {
            return NotFound();
        }

        return View(ToViewModel(data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Atender(ClinicalConsultationViewModel model)
    {
        var data = await clinicData.GetConsultationAsync(model.ConsultationId);
        if (data is null || data.Consultation.State != Models.Enums.EstadoConsulta.EnAtencion)
        {
            return NotFound();
        }

        model.Patient = data.Patient;
        model.Previous = data.Previous;
        model.Consultation = data.Consultation;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.SaveClinicalNotesAsync(model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La nota clínica se guardó.";
        return RedirectToAction(nameof(Atender), new { id = model.ConsultationId });
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var data = await clinicData.GetConsultationAsync(id);
        if (data is null)
        {
            return NotFound();
        }

        return View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        try
        {
            if (!await clinicData.CloseConsultationAsync(id))
            {
                return NotFound();
            }
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Atender), new { id });
        }

        TempData["Mensaje"] = "La consulta se cerró y quedó disponible en el historial.";
        return RedirectToAction("Index", "SalaEspera");
    }

    private static ClinicalConsultationViewModel ToViewModel(ConsultationPageData data) =>
        new()
        {
            ConsultationId = data.Consultation.Id,
            Patient = data.Patient,
            Previous = data.Previous,
            Consultation = data.Consultation,
            Reason = data.Consultation.Reason ?? string.Empty,
            Symptoms = data.Consultation.Symptoms,
            PhysicalEvaluation = data.Consultation.PhysicalEvaluation,
            Diagnosis = data.Consultation.Diagnosis ?? string.Empty,
            ExamResults = data.Consultation.ExamResults,
            BloodPressure = data.Consultation.Vitals?.BloodPressure,
            HeartRate = data.Consultation.Vitals?.HeartRate,
            OxygenSaturation = data.Consultation.Vitals?.OxygenSaturation
        };
}
