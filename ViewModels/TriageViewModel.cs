using System.ComponentModel.DataAnnotations;
using ClinicaApp.Models.Enums;

namespace ClinicaApp.ViewModels;

public sealed class TriageViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [Required(ErrorMessage = "El peso es obligatorio.")]
    [Range(typeof(decimal), "1", "350", ErrorMessage = "El peso debe estar entre 1 y 350 kg.")]
    [Display(Name = "Peso")]
    public decimal? WeightKg { get; set; }

    [Required(ErrorMessage = "La altura es obligatoria.")]
    [Range(typeof(decimal), "0.3", "2.5", ErrorMessage = "La altura debe estar entre 0.3 y 2.5 m.")]
    [Display(Name = "Altura")]
    public decimal? HeightMeters { get; set; }

    [Required(ErrorMessage = "La temperatura es obligatoria.")]
    [Range(typeof(decimal), "25", "45", ErrorMessage = "La temperatura debe estar entre 25 y 45 °C.")]
    [Display(Name = "Temperatura")]
    public decimal? Temperature { get; set; }

    [Required]
    [EnumDataType(typeof(PrioridadAtencion))]
    [Display(Name = "Prioridad")]
    public PrioridadAtencion Priority { get; set; } = PrioridadAtencion.Media;

    public string PatientName { get; set; } = string.Empty;
}
