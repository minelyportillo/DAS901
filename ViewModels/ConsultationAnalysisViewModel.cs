using ClinicaApp.Models.Enums;

namespace ClinicaApp.ViewModels;

public sealed class ConsultationAnalysisViewModel
{
    public DateTime Desde { get; set; } = DateTime.Today.AddDays(-29);
    public DateTime Hasta { get; set; } = DateTime.Today;
    public EstadoConsulta? Estado { get; set; }
    public PrioridadAtencion? Prioridad { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;

    public int TotalConsultas { get; set; }
    public int TotalPaginas { get; set; }
    public double? DuracionPromedioMinutos { get; set; }
    public IReadOnlyList<ConsultationAnalysisRow> Consultas { get; set; } = [];
    public IReadOnlyList<ConsultationGroupRow> PorEstado { get; set; } = [];
    public IReadOnlyList<ConsultationGroupRow> PorDia { get; set; } = [];
    public IReadOnlyList<ConsultationGroupRow> DiagnosticosFrecuentes { get; set; } = [];
    public IReadOnlyList<ConsultationGroupRow> MedicamentosFrecuentes { get; set; } = [];
}

public sealed record ConsultationAnalysisRow(
    int Id,
    DateTime FechaHoraInicio,
    string Paciente,
    string? Dui,
    EstadoConsulta Estado,
    PrioridadAtencion Prioridad,
    string? Diagnostico,
    double? Peso,
    double? Temperatura);

public sealed record ConsultationGroupRow(string Etiqueta, int Cantidad);
