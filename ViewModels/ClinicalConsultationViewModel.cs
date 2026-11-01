using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ClinicaApp.Services;

namespace ClinicaApp.ViewModels;

public sealed class ClinicalConsultationViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [ValidateNever]
    public PatientListItem Patient { get; set; } = null!;

    [ValidateNever]
    public IReadOnlyList<ClinicDataStore.AlergiaRecord> Alergias { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<ClinicDataStore.EnfermedadRecord> Enfermedades { get; set; } = [];

    [ValidateNever]
    public PreviousConsultationData? Previous { get; set; }

    [ValidateNever]
    public ClinicDataStore.ConsultationRecord Consultation { get; set; } = null!;

    [ValidateNever]
    public IReadOnlyList<StudyDisplayItem> EstudiosLeidos { get; set; } = [];

    [ValidateNever]
    public IReadOnlyList<PendingOrderDetailItem> DetallesOrdenPaciente { get; set; } = [];

    // Datos de tabla consultas
    [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
    [StringLength(1000)]
    [Display(Name = "Motivo de consulta")]
    public string Motivo { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Examen físico")]
    public string? ExamenFisico { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
    [StringLength(500)]
    [Display(Name = "Diagnóstico")]
    public string Diagnostico { get; set; } = string.Empty;

    // Datos de tabla signos_vitales
    [Range(1.0, 350.0, ErrorMessage = "El peso debe estar entre 1 y 350 kg.")]
    [Display(Name = "Peso (kg)")]
    public double? Peso { get; set; }

    [Range(0.3, 2.5, ErrorMessage = "La altura debe estar entre 0.3 y 2.5 m.")]
    [Display(Name = "Altura (m)")]
    public double? Altura { get; set; }

    [Range(25.0, 45.0, ErrorMessage = "La temperatura debe estar entre 25 y 45 °C.")]
    [Display(Name = "Temperatura (°C)")]
    public double? Temperatura { get; set; }

    [Range(40, 300, ErrorMessage = "La presión sistólica debe estar entre 40 y 300 mmHg.")]
    [Display(Name = "Presión sistólica (mmHg)")]
    public int? PresionSistolica { get; set; }

    [Range(20, 200, ErrorMessage = "La presión diastólica debe estar entre 20 y 200 mmHg.")]
    [Display(Name = "Presión diastólica (mmHg)")]
    public int? PresionDiastolica { get; set; }

    [Range(20, 250, ErrorMessage = "El ritmo cardíaco debe estar entre 20 y 250 lpm.")]
    [Display(Name = "Ritmo cardíaco (lpm)")]
    public int? RitmoCardiaco { get; set; }

    [Range(50, 100, ErrorMessage = "La concentración de oxígeno debe estar entre 50 y 100 %.")]
    [Display(Name = "Concentración de oxígeno (%)")]
    public int? ConcentracionOxigeno { get; set; }
}
