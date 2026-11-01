using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class EnfermedadViewModel
{
    [Required]
    public int PatientId { get; set; }

    public int? ConsultationId { get; set; }

    [Required(ErrorMessage = "El nombre de la enfermedad es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre de la enfermedad")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El año de diagnóstico es obligatorio.")]
    [Range(1900, 2100, ErrorMessage = "Ingresa un año de diagnóstico válido.")]
    [Display(Name = "Año de diagnóstico")]
    public int AnioDiagnostico { get; set; } = DateTime.Today.Year;
}
