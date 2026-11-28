using ClinicaApp.Data;
using ClinicaApp.Models.Enums;
using ClinicaApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Services;

public sealed class ConsultationAnalysisService(ClinicaDbContext db) : IConsultationAnalysisService
{
    private const int MaxPageSize = 100;

    public async Task<ConsultationAnalysisViewModel> GetReportAsync(
        ConsultationAnalysisViewModel filters,
        CancellationToken cancellationToken)
    {
        var from = filters.Desde.Date;
        var untilExclusive = filters.Hasta.Date.AddDays(1);
        var requestedPage = Math.Max(1, filters.Pagina);
        var pageSize = Math.Clamp(filters.TamanoPagina, 1, MaxPageSize);

        var consultations = db.Consultas
            .AsNoTracking()
            .Where(consultation => consultation.FechaHoraInicio >= from
                && consultation.FechaHoraInicio < untilExclusive);

        if (filters.Estado.HasValue)
        {
            consultations = consultations.Where(consultation => consultation.Estado == filters.Estado.Value);
        }

        if (filters.Prioridad.HasValue)
        {
            consultations = consultations.Where(consultation => consultation.Prioridad == filters.Prioridad.Value);
        }

        var total = await consultations.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var page = totalPages == 0 ? 1 : Math.Min(requestedPage, totalPages);
        var rawStates = await consultations
            .GroupBy(consultation => consultation.Estado)
            .Select(group => new
            {
                State = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .ToListAsync(cancellationToken);
        var states = rawStates
            .Select(group => new ConsultationGroupRow(group.State.ToString(), group.Count))
            .ToList();

        var rawByDay = await consultations
            .GroupBy(consultation => consultation.FechaHoraInicio.Date)
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Count()
            })
            .OrderBy(group => group.Date)
            .ToListAsync(cancellationToken);
        var byDay = rawByDay
            .Select(group => new ConsultationGroupRow(
                group.Date.ToString("yyyy-MM-dd"),
                group.Count))
            .ToList();

        var rawDiagnoses = await consultations
            .Where(consultation => consultation.Diagnostico != null
                && consultation.Diagnostico != string.Empty)
            .GroupBy(consultation => consultation.Diagnostico!)
            .Select(group => new
            {
                Diagnosis = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Diagnosis)
            .Take(5)
            .ToListAsync(cancellationToken);
        var frequentDiagnoses = rawDiagnoses
            .Select(group => new ConsultationGroupRow(group.Diagnosis, group.Count))
            .ToList();

        var rawMedications = await db.DetallesReceta
            .AsNoTracking()
            .Where(detail => detail.Receta.Consulta.FechaHoraInicio >= from
                && detail.Receta.Consulta.FechaHoraInicio < untilExclusive
                && (!filters.Estado.HasValue || detail.Receta.Consulta.Estado == filters.Estado.Value)
                && (!filters.Prioridad.HasValue || detail.Receta.Consulta.Prioridad == filters.Prioridad.Value))
            .GroupBy(detail => detail.Medicamento == null
                ? "Medicamento no disponible"
                : detail.Medicamento.Nombre)
            .Select(group => new
            {
                Medication = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Medication)
            .Take(5)
            .ToListAsync(cancellationToken);
        var frequentMedications = rawMedications
            .Select(group => new ConsultationGroupRow(group.Medication, group.Count))
            .ToList();

        var averageDuration = await consultations
            .Where(consultation => consultation.FechaHoraFinalizacion.HasValue)
            .Select(consultation => (double?)EF.Functions.DateDiffMinute(
                consultation.FechaHoraInicio,
                consultation.FechaHoraFinalizacion!.Value))
            .AverageAsync(cancellationToken);

        var rows = await consultations
            .OrderByDescending(consultation => consultation.Prioridad)
            .ThenByDescending(consultation => consultation.FechaHoraInicio)
            .ThenByDescending(consultation => consultation.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(consultation => new ConsultationAnalysisRow(
                consultation.Id,
                consultation.FechaHoraInicio,
                consultation.Paciente.Nombre,
                consultation.Paciente.Dui,
                consultation.Estado,
                consultation.Prioridad,
                consultation.Diagnostico,
                consultation.SignosVitales == null ? null : consultation.SignosVitales.Peso,
                consultation.SignosVitales == null ? null : consultation.SignosVitales.Temperatura))
            .ToListAsync(cancellationToken);

        return new ConsultationAnalysisViewModel
        {
            Desde = from,
            Hasta = filters.Hasta.Date,
            Estado = filters.Estado,
            Prioridad = filters.Prioridad,
            Pagina = page,
            TamanoPagina = pageSize,
            TotalConsultas = total,
            TotalPaginas = totalPages,
            DuracionPromedioMinutos = averageDuration,
            Consultas = rows,
            PorEstado = states,
            PorDia = byDay,
            DiagnosticosFrecuentes = frequentDiagnoses,
            MedicamentosFrecuentes = frequentMedications
        };
    }
}
