using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class ExamOrderViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    public int? DetalleOrdenId { get; set; }

    [Required(ErrorMessage = "El nombre del examen es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre del examen")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Indicaciones")]
    public string? Indicaciones { get; set; }
}
