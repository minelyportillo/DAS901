using ClinicaApp.Models.Enums;
using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers;

[Authorize(Roles = "Secretaria,Doctora")]
public sealed class AnalisisController(IConsultationAnalysisService analysisService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? desde,
        DateTime? hasta,
        EstadoConsulta? estado,
        PrioridadAtencion? prioridad,
        int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        if ((estado.HasValue && !Enum.IsDefined(estado.Value))
            || (prioridad.HasValue && !Enum.IsDefined(prioridad.Value)))
        {
            return BadRequest();
        }

        var filters = new ConsultationAnalysisViewModel
        {
            Desde = desde?.Date ?? DateTime.Today.AddDays(-29),
            Hasta = hasta?.Date ?? DateTime.Today,
            Estado = estado,
            Prioridad = prioridad,
            Pagina = pagina
        };

        if (filters.Desde > filters.Hasta)
        {
            ModelState.AddModelError(string.Empty, "La fecha inicial debe ser anterior o igual a la fecha final.");
            return View(filters);
        }

        var report = await analysisService.GetReportAsync(filters, cancellationToken);
        return View(report);
    }
}
