using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers;

[Authorize(Roles = "Doctora")]
public sealed class DocumentosController(ClinicDataStore clinicData) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Receta(int consultaId)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        return View(new PrescriptionViewModel { ConsultationId = consultaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receta(PrescriptionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.AddPrescriptionAsync(model.ConsultationId, model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "El medicamento se agregó a la receta.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpGet]
    public async Task<IActionResult> OrdenExamen(int consultaId)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        return View(new ExamOrderViewModel { ConsultationId = consultaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrdenExamen(ExamOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.AddExamOrderAsync(model.ConsultationId, model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La orden de examen se agregó a la consulta.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpGet]
    public async Task<IActionResult> Incapacidad(int consultaId)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        return View(new IncapacityViewModel { ConsultationId = consultaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Incapacidad(IncapacityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.AddIncapacityAsync(model.ConsultationId, model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La incapacidad se agregó a la consulta.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(int id)
    {
        var data = await clinicData.GetConsultationAsync(id);
        return data is null ? NotFound() : View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEstadoExamen(
        int consultaId,
        int ordenId,
        Models.Enums.EstadoOrdenExamen estado)
    {
        if (!Enum.IsDefined(estado))
        {
            return BadRequest();
        }

        if (!await clinicData.SetExamOrderStateAsync(consultaId, ordenId, estado))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "El estado de la orden se actualizó.";
        var consultation = await clinicData.GetConsultationAsync(consultaId);
        return consultation?.Consultation.State == Models.Enums.EstadoConsulta.EnAtencion
            ? RedirectToAction("Atender", "Consultas", new { id = consultaId })
            : RedirectToAction("Detalle", "Consultas", new { id = consultaId });
    }

    private async Task<ConsultationPageData?> GetActiveConsultationAsync(int id)
    {
        var data = await clinicData.GetConsultationAsync(id);
        return data?.Consultation.State == Models.Enums.EstadoConsulta.EnAtencion ? data : null;
    }
}
