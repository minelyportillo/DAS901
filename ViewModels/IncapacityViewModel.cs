using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class IncapacityViewModel
{
    [Required]
    public int ConsultationId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de fin")]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "El motivo de la incapacidad es obligatorio.")]
    [StringLength(1000)]
    [Display(Name = "Motivo")]
    public string Motivo { get; set; } = string.Empty;
}
