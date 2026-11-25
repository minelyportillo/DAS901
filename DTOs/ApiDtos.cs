using System.ComponentModel.DataAnnotations;
using ClinicaApp.Models.Enums;

namespace ClinicaApp.DTOs;

public sealed record ApiMessageResponse(string Mensaje);

public sealed class PacienteRequestDto
{
    [Required(ErrorMessage = "El nombre del paciente es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateTime FechaNacimiento { get; set; } = DateTime.Today;

    [RegularExpression(@"^\d{8}-\d$", ErrorMessage = "El DUI debe tener el formato 12345678-9.")]
    [StringLength(10)]
    public string? Dui { get; set; }
}

public sealed record PacienteResponseDto(
    int Id,
    string Nombre,
    DateTime FechaNacimiento,
    int Edad,
    string? Dui);

public sealed class AlergiaRequestDto
{
    [Required(ErrorMessage = "El tipo de alergia es obligatorio.")]
    [StringLength(100)]
    public string TipoAlergia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La sustancia o elemento es obligatorio.")]
    [StringLength(150)]
    public string SustanciaElemento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La severidad es obligatoria.")]
    [StringLength(50)]
    public string Severidad { get; set; } = string.Empty;
}

public sealed class EnfermedadRequestDto
{
    [Required(ErrorMessage = "El nombre de la enfermedad es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "El año de diagnóstico debe estar entre 1900 y 2100.")]
    public int AnioDiagnostico { get; set; } = DateTime.Today.Year;
}

public sealed class MedicamentoRequestDto
{
    [Required(ErrorMessage = "El nombre del medicamento es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre del medicamento debe tener entre 2 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}

public sealed class PrioridadConsultaRequestDto
{
    [Required]
    public PrioridadAtencion Prioridad { get; set; }
}

public sealed class TriajeRequestDto
{
    [Range(0.5, 400, ErrorMessage = "El peso debe estar entre 0.5 y 400 kg.")]
    public double? Peso { get; set; }

    [Range(0.3, 2.5, ErrorMessage = "La altura debe estar entre 0.30 y 2.50 m.")]
    public double? Altura { get; set; }

    [Range(30, 45, ErrorMessage = "La temperatura debe estar entre 30 y 45 °C.")]
    public double? Temperatura { get; set; }
}
