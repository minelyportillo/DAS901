using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaApp.ViewModels;

public sealed class PrescriptionViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    public int? DetalleId { get; set; }

    [Required(ErrorMessage = "Selecciona un medicamento del catálogo.")]
    [Display(Name = "Medicamento")]
    public int? IdMedicamento { get; set; }

    [Required(ErrorMessage = "La dosis es obligatoria.")]
    [StringLength(100)]
    [Display(Name = "Dosis")]
    public string Dosis { get; set; } = string.Empty;

    [Required(ErrorMessage = "La frecuencia es obligatoria.")]
    [StringLength(100)]
    [Display(Name = "Frecuencia")]
    public string Frecuencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La duración es obligatoria.")]
    [StringLength(100)]
    [Display(Name = "Duración")]
    public string Duracion { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Indicaciones")]
    public string? Indicaciones { get; set; }

    [ValidateNever]
    public IReadOnlyList<MedicationOption> CatalogoMedicamentos { get; set; } = [];
}
