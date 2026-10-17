using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class IncapacityViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [Range(1, 365, ErrorMessage = "Los días de reposo deben estar entre 1 y 365.")]
    [Display(Name = "Días de reposo")]
    public int Days { get; set; } = 1;

    [Required(ErrorMessage = "La justificación médica es obligatoria.")]
    [StringLength(500)]
    [Display(Name = "Justificación")]
    public string Justification { get; set; } = string.Empty;
}
