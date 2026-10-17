using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class ExamOrderViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [Required(ErrorMessage = "El examen solicitado es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Examen solicitado")]
    public string ExamType { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Indicaciones")]
    public string? Instructions { get; set; }
}
