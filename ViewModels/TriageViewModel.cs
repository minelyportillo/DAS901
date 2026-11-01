using System.ComponentModel.DataAnnotations;
using ClinicaApp.Models.Enums;

namespace ClinicaApp.ViewModels;

public sealed class TriageViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [Required(ErrorMessage = "El peso es obligatorio.")]
    [Range(1.0, 350.0, ErrorMessage = "El peso debe estar entre 1 y 350 kg.")]
    [Display(Name = "Peso (kg)")]
    public double? Peso { get; set; }

    [Required(ErrorMessage = "La altura es obligatoria.")]
    [Range(0.3, 2.5, ErrorMessage = "La altura debe estar entre 0.3 y 2.5 m.")]
    [Display(Name = "Altura (m)")]
    public double? Altura { get; set; }

    [Required(ErrorMessage = "La temperatura es obligatoria.")]
    [Range(25.0, 45.0, ErrorMessage = "La temperatura debe estar entre 25 y 45 °C.")]
    [Display(Name = "Temperatura (°C)")]
    public double? Temperatura { get; set; }

    [Required]
    [EnumDataType(typeof(PrioridadAtencion))]
    [Display(Name = "Prioridad")]
    public PrioridadAtencion Prioridad { get; set; } = PrioridadAtencion.Media;

    public string PatientName { get; set; } = string.Empty;
}
