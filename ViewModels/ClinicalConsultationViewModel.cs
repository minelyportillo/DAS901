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
    public PreviousConsultationData? Previous { get; set; }

    [ValidateNever]
    public ClinicDataStore.ConsultationRecord Consultation { get; set; } = null!;

    [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
    [StringLength(300)]
    [Display(Name = "Motivo de consulta")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Síntomas")]
    public string? Symptoms { get; set; }

    [StringLength(1000)]
    [Display(Name = "Exploración física")]
    public string? PhysicalEvaluation { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
    [StringLength(500)]
    [Display(Name = "Diagnóstico")]
    public string Diagnosis { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Resultados de exámenes")]
    public string? ExamResults { get; set; }

    [RegularExpression(@"^\d{2,3}/\d{2,3}$", ErrorMessage = "Usa el formato sistólica/diastólica, por ejemplo 120/80.")]
    [StringLength(7)]
    [Display(Name = "Presión arterial")]
    public string? BloodPressure { get; set; }

    [Range(20, 250, ErrorMessage = "La frecuencia cardíaca debe estar entre 20 y 250 lpm.")]
    [Display(Name = "Frecuencia cardíaca (lpm)")]
    public int? HeartRate { get; set; }

    [Range(50, 100, ErrorMessage = "La saturación debe estar entre 50 y 100 %.")]
    [Display(Name = "Saturación de oxígeno (%)")]
    public int? OxygenSaturation { get; set; }
}
