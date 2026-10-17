using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class PrescriptionViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [Required(ErrorMessage = "El medicamento es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Medicamento")]
    public string Medication { get; set; } = string.Empty;

    [Required(ErrorMessage = "La concentración es obligatoria.")]
    [StringLength(50)]
    [Display(Name = "Concentración")]
    public string Concentration { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dosis es obligatoria.")]
    [StringLength(100)]
    [Display(Name = "Dosis")]
    public string Dose { get; set; } = string.Empty;

    [Required(ErrorMessage = "La duración es obligatoria.")]
    [StringLength(50)]
    [Display(Name = "Duración")]
    public string Duration { get; set; } = string.Empty;
}
