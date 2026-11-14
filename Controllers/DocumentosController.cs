using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers;

[Authorize(Roles = "Doctora")]
public sealed class DocumentosController(
    IClinicDataStore clinicData,
    IMedicamentoService medicamentoService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Receta(int consultaId, int? detalleId = null, int? medicamentoId = null)
    {
        var data = await GetActiveConsultationAsync(consultaId);
        if (data is null)
        {
            return NotFound();
        }

        var viewModel = new PrescriptionViewModel
        {
            ConsultationId = consultaId,
            DetalleId = detalleId,
            IdMedicamento = medicamentoId,
            CatalogoMedicamentos = await medicamentoService.ObtenerCatalogoAsync()
        };

        if (detalleId.HasValue)
        {
            var existente = data.Consultation.Recetas
                .SelectMany(r => r.Detalles)
                .FirstOrDefault(d => d.Id == detalleId.Value);
            if (existente is null)
            {
                return NotFound();
            }

            viewModel.IdMedicamento = medicamentoId ?? existente.IdMedicamento;
            viewModel.Dosis = existente.Dosis;
            viewModel.Frecuencia = existente.Frecuencia;
            viewModel.Duracion = existente.Duracion;
            viewModel.Indicaciones = existente.Indicaciones;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receta(PrescriptionViewModel model)
    {
        model.CatalogoMedicamentos = await medicamentoService.ObtenerCatalogoAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.SavePrescriptionAsync(model.ConsultationId, model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = model.DetalleId.HasValue
            ? "El medicamento de la receta fue actualizado."
            : "El medicamento se agregó al detalle de la receta.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDetalleReceta(int consultaId, int detalleId)
    {
        if (!await clinicData.DeletePrescriptionDetailAsync(consultaId, detalleId))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "El medicamento fue eliminado de la receta.";
        return RedirectToAction("Atender", "Consultas", new { id = consultaId });
    }

    [HttpGet]
    public async Task<IActionResult> NuevoMedicamento(int consultaId, int? detalleId = null)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        return View(new MedicationViewModel
        {
            ConsultationId = consultaId,
            DetalleId = detalleId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NuevoMedicamento(MedicationViewModel model)
    {
        if (await GetActiveConsultationAsync(model.ConsultationId) is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var nuevoId = await medicamentoService.RegistrarAsync(model.Nombre);
        TempData["Mensaje"] = $"El medicamento '{model.Nombre.Trim()}' fue registrado en la tabla medicamentos.";
        return RedirectToAction(nameof(Receta), new
        {
            consultaId = model.ConsultationId,
            detalleId = model.DetalleId,
            medicamentoId = nuevoId
        });
    }

    [HttpGet]
    public async Task<IActionResult> CatalogoMedicamentos(int consultaId)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        return View(new MedicationCatalogViewModel
        {
            ConsultationId = consultaId,
            Medications = await medicamentoService.ObtenerCatalogoAsync()
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditarMedicamento(int consultaId, int medicamentoId)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        var medication = await medicamentoService.ObtenerPorIdAsync(medicamentoId);
        return medication is null
            ? NotFound()
            : View(new MedicationViewModel
            {
                ConsultationId = consultaId,
                MedicamentoId = medication.Id,
                Nombre = medication.Nombre
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarMedicamento(MedicationViewModel model)
    {
        if (!model.MedicamentoId.HasValue
            || await GetActiveConsultationAsync(model.ConsultationId) is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await medicamentoService.ActualizarAsync(model.MedicamentoId.Value, model.Nombre))
        {
            if (await medicamentoService.ObtenerPorIdAsync(model.MedicamentoId.Value) is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(nameof(model.Nombre), "Ya existe otro medicamento con ese nombre.");
            return View(model);
        }

        TempData["Mensaje"] = "El medicamento se actualizó correctamente.";
        return RedirectToAction(nameof(CatalogoMedicamentos), new { consultaId = model.ConsultationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarMedicamento(int consultaId, int medicamentoId)
    {
        if (await GetActiveConsultationAsync(consultaId) is null)
        {
            return NotFound();
        }

        if (!await medicamentoService.EliminarSiNoEstaPrescritoAsync(medicamentoId))
        {
            if (await medicamentoService.ObtenerPorIdAsync(medicamentoId) is null)
            {
                return NotFound();
            }

            TempData["Error"] = "No se puede eliminar un medicamento que ya aparece en una receta.";
            return RedirectToAction(nameof(CatalogoMedicamentos), new { consultaId });
        }

        TempData["Mensaje"] = "El medicamento se eliminó del catálogo.";
        return RedirectToAction(nameof(CatalogoMedicamentos), new { consultaId });
    }

    [HttpGet]
    public async Task<IActionResult> OrdenExamen(int consultaId, int? detalleOrdenId = null)
    {
        var data = await GetActiveConsultationAsync(consultaId);
        if (data is null)
        {
            return NotFound();
        }

        var viewModel = new ExamOrderViewModel
        {
            ConsultationId = consultaId,
            DetalleOrdenId = detalleOrdenId
        };

        if (detalleOrdenId.HasValue)
        {
            var existente = data.Consultation.Ordenes
                .SelectMany(o => o.Detalles)
                .FirstOrDefault(d => d.Id == detalleOrdenId.Value);
            if (existente is null)
            {
                return NotFound();
            }

            viewModel.Nombre = existente.Nombre;
            viewModel.Indicaciones = existente.Indicaciones;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrdenExamen(ExamOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.SaveExamOrderAsync(model.ConsultationId, model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = model.DetalleOrdenId.HasValue
            ? "La orden de examen fue actualizada."
            : "El examen solicitado se agregó al detalle de la orden.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDetalleOrden(int consultaId, int detalleOrdenId)
    {
        if (!await clinicData.DeleteExamOrderDetailAsync(consultaId, detalleOrdenId))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La orden de examen fue eliminada.";
        return RedirectToAction("Atender", "Consultas", new { id = consultaId });
    }

    [HttpGet]
    public async Task<IActionResult> Estudio(int consultaId, int? estudioId = null)
    {
        var data = await GetActiveConsultationAsync(consultaId);
        if (data is null)
        {
            return NotFound();
        }

        var viewModel = new StudyViewModel
        {
            ConsultationId = consultaId,
            EstudioId = estudioId,
            PatientId = data.Patient.Id,
            Fecha = DateTime.Today,
            OrdenesDisponibles = data.DetallesOrdenPaciente
        };

        if (estudioId.HasValue)
        {
            var existente = data.EstudiosLeidos.FirstOrDefault(e => e.Id == estudioId.Value);
            if (existente is null)
            {
                return NotFound();
            }

            viewModel.IdDetalleOrden = existente.IdDetalleOrden;
            viewModel.Nombre = existente.NombrePropio;
            viewModel.Fecha = existente.Fecha;
            viewModel.Observaciones = existente.Observaciones;
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Estudio(StudyViewModel model)
    {
        var data = await GetActiveConsultationAsync(model.ConsultationId);
        if (data is null)
        {
            return NotFound();
        }

        model.PatientId = data.Patient.Id;
        model.OrdenesDisponibles = data.DetallesOrdenPaciente;

        if (!model.IdDetalleOrden.HasValue && string.IsNullOrWhiteSpace(model.Nombre))
        {
            ModelState.AddModelError(
                nameof(model.Nombre),
                "El nombre del estudio es obligatorio cuando es un examen externo sin orden médica previa.");
        }

        if (model.Fecha.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.Fecha),
                "La fecha del estudio no puede ser futura.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.SaveStudyAsync(model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = model.EstudioId.HasValue
            ? "El estudio médico fue actualizado."
            : "El estudio médico y su lectura quedaron registrados.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEstudio(int consultaId, int estudioId)
    {
        if (!await clinicData.DeleteStudyAsync(consultaId, estudioId))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "El estudio médico fue eliminado de la consulta.";
        return RedirectToAction("Atender", "Consultas", new { id = consultaId });
    }

    [HttpGet]
    public async Task<IActionResult> Incapacidad(int consultaId)
    {
        var data = await GetActiveConsultationAsync(consultaId);
        if (data is null)
        {
            return NotFound();
        }

        var existing = data.Consultation.Incapacidad;
        return View(new IncapacityViewModel
        {
            ConsultationId = consultaId,
            FechaInicio = existing?.FechaInicio ?? DateTime.Today,
            FechaFin = existing?.FechaFin ?? DateTime.Today.AddDays(1),
            Motivo = existing?.Motivo ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Incapacidad(IncapacityViewModel model)
    {
        if (model.FechaFin.Date < model.FechaInicio.Date)
        {
            ModelState.AddModelError(
                nameof(model.FechaFin),
                "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await clinicData.AddIncapacityAsync(model.ConsultationId, model))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La incapacidad médica se guardó en la consulta.";
        return RedirectToAction("Atender", "Consultas", new { id = model.ConsultationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarIncapacidad(int consultaId)
    {
        if (!await clinicData.DeleteIncapacityAsync(consultaId))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La incapacidad fue eliminada de la consulta.";
        return RedirectToAction("Atender", "Consultas", new { id = consultaId });
    }

    [HttpGet]
    public async Task<IActionResult> Imprimir(int id)
    {
        var data = await clinicData.GetConsultationAsync(id);
        return data is null ? NotFound() : View(data);
    }

    private async Task<ConsultationPageData?> GetActiveConsultationAsync(int id)
    {
        var data = await clinicData.GetConsultationAsync(id);
        return data?.Consultation.Estado == Models.Enums.EstadoConsulta.EnAtencion ? data : null;
    }
}
