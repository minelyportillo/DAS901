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
    public async Task<IActionResult> Atender(ClinicalConsultationViewModel model, string? irA = null)
    {
        var data = await clinicData.GetConsultationAsync(model.ConsultationId);
        if (data is null || data.Consultation.Estado != Models.Enums.EstadoConsulta.EnAtencion)
        {
            return NotFound();
        }

        PopulateContext(model, data);

        // Si la doctora presionó uno de los botones para ir a Receta, OrdenExamen, Estudio o Incapacidad,
        // guardamos automáticamente el borrador actual de la consulta (incluso si Motivo/Diagnóstico aún están en progreso)
        // antes de redirigir al formulario correspondiente.
        if (!string.IsNullOrWhiteSpace(irA))
        {
            await clinicData.SaveClinicalNotesAsync(model);
            return irA switch
            {
                "Receta" => RedirectToAction("Receta", "Documentos", new { consultaId = model.ConsultationId }),
                "OrdenExamen" => RedirectToAction("OrdenExamen", "Documentos", new { consultaId = model.ConsultationId }),
                "Estudio" => RedirectToAction("Estudio", "Documentos", new { consultaId = model.ConsultationId }),
                "Incapacidad" => RedirectToAction("Incapacidad", "Documentos", new { consultaId = model.ConsultationId }),
                "Imprimir" => RedirectToAction("Imprimir", "Documentos", new { id = model.ConsultationId }),
                _ => RedirectToAction(nameof(Atender), new { id = model.ConsultationId })
            };
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.SaveClinicalNotesAsync(model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "Los datos clínicos y signos vitales de la consulta se guardaron.";
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

        TempData["Mensaje"] = "La consulta se finalizó y quedó disponible en el historial.";
        return RedirectToAction("Index", "SalaEspera");
    }

    private static void PopulateContext(ClinicalConsultationViewModel model, ConsultationPageData data)
    {
        model.Patient = data.Patient;
        model.Alergias = data.Alergias;
        model.Enfermedades = data.Enfermedades;
        model.Previous = data.Previous;
        model.Consultation = data.Consultation;
        model.EstudiosLeidos = data.EstudiosLeidos;
        model.DetallesOrdenPaciente = data.DetallesOrdenPaciente;
    }

    private static ClinicalConsultationViewModel ToViewModel(ConsultationPageData data) =>
        new()
        {
            ConsultationId = data.Consultation.Id,
            Patient = data.Patient,
            Alergias = data.Alergias,
            Enfermedades = data.Enfermedades,
            Previous = data.Previous,
            Consultation = data.Consultation,
            EstudiosLeidos = data.EstudiosLeidos,
            DetallesOrdenPaciente = data.DetallesOrdenPaciente,
            Motivo = data.Consultation.Motivo ?? string.Empty,
            ExamenFisico = data.Consultation.ExamenFisico,
            Diagnostico = data.Consultation.Diagnostico ?? string.Empty,
            Peso = data.Consultation.SignosVitales?.Peso,
            Altura = data.Consultation.SignosVitales?.Altura,
            Temperatura = data.Consultation.SignosVitales?.Temperatura,
            PresionSistolica = data.Consultation.SignosVitales?.PresionSistolica,
            PresionDiastolica = data.Consultation.SignosVitales?.PresionDiastolica,
            RitmoCardiaco = data.Consultation.SignosVitales?.RitmoCardiaco,
            ConcentracionOxigeno = data.Consultation.SignosVitales?.ConcentracionOxigeno
        };
}
