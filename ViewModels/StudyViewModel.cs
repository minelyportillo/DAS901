using System.ComponentModel.DataAnnotations;
using ClinicaApp.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaApp.ViewModels;

public sealed class StudyViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    public int? EstudioId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Display(Name = "Orden médica previa asociada (opcional si es examen externo)")]
    public int? IdDetalleOrden { get; set; }

    [StringLength(150)]
    [Display(Name = "Nombre del estudio (obligatorio si es examen externo)")]
    public string? Nombre { get; set; }

    [Required(ErrorMessage = "La fecha del estudio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de realización")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Las observaciones de lectura del estudio son obligatorias.")]
    [StringLength(2000)]
    [Display(Name = "Observaciones / Resultados leídos")]
    public string Observaciones { get; set; } = string.Empty;

    [ValidateNever]
    public IReadOnlyList<PendingOrderDetailItem> OrdenesDisponibles { get; set; } = [];
}
