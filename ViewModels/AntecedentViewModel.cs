using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class AntecedentViewModel
{
    [Required]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "La categoría del antecedente es obligatoria.")]
    [StringLength(50)]
    [Display(Name = "Categoría")]
    public string Category { get; set; } = "General";

    [Required(ErrorMessage = "La descripción del antecedente es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Antecedente")]
    public string Description { get; set; } = string.Empty;
}
