using ClinicaApp.Models.Enums;
using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers;

[Authorize(Roles = "Secretaria,Doctora")]
public sealed class SalaEsperaController(ClinicDataStore clinicData) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var patients = await clinicData.GetWaitingRoomAsync(DateTime.Today);
        return View(patients);
    }

    [HttpPost]
    [Authorize(Roles = "Secretaria")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarLlegada(int pacienteId)
    {
        try
        {
            await clinicData.RegisterArrivalAsync(pacienteId);
            TempData["Mensaje"] = "La llegada del paciente quedó registrada.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Secretaria")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarPrioridad(int consultaId, PrioridadAtencion prioridad)
    {
        if (!Enum.IsDefined(prioridad))
        {
            return BadRequest();
        }

        if (!await clinicData.SetPriorityAsync(consultaId, prioridad))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La prioridad se actualizó.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Secretaria")]
    public async Task<IActionResult> SignosVitales(int id)
    {
        var consultation = (await clinicData.GetWaitingRoomAsync(DateTime.Today))
            .FirstOrDefault(item => item.ConsultationId == id);
        if (consultation is null)
        {
            return NotFound();
        }

        return View(new TriageViewModel
        {
            ConsultationId = id,
            PatientName = consultation.PatientName,
            Peso = consultation.Peso,
            Altura = consultation.Altura,
            Temperatura = consultation.Temperatura,
            Prioridad = consultation.Priority
        });
    }

    [HttpPost]
    [Authorize(Roles = "Secretaria")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignosVitales(TriageViewModel model)
    {
        var consultation = (await clinicData.GetWaitingRoomAsync(DateTime.Today))
            .FirstOrDefault(item => item.ConsultationId == model.ConsultationId);
        if (consultation is null)
        {
            return NotFound();
        }

        model.PatientName = consultation.PatientName;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.SaveTriageAsync(model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "Los signos vitales iniciales quedaron registrados.";
        return RedirectToAction(nameof(Index));
    }
}
