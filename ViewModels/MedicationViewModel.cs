using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class MedicationViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    public int? DetalleId { get; set; }

    public int? MedicamentoId { get; set; }

    [Required(ErrorMessage = "El nombre del medicamento es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    [Display(Name = "Nombre del medicamento")]
    public string Nombre { get; set; } = string.Empty;
}
