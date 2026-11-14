using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class AlergiaPacienteViewModel
{
    public int? Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    public int? ConsultationId { get; set; }

    [Required(ErrorMessage = "El tipo de alergia es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Tipo de alergia")]
    public string TipoAlergia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La sustancia o elemento es obligatoria.")]
    [StringLength(150)]
    [Display(Name = "Sustancia o elemento")]
    public string SustanciaElemento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La severidad es obligatoria.")]
    [StringLength(50)]
    [Display(Name = "Severidad")]
    public string Severidad { get; set; } = "Moderada";
}
