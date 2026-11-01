using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels
{
    public class RegistrarPacienteViewModel
    {
        [Required(ErrorMessage = "El nombre del paciente es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-18);

        [RegularExpression(@"^\d{8}-?\d$", ErrorMessage = "Formato de DUI no válido (ej. 01234567-8).")]
        [StringLength(10)]
        [Display(Name = "DUI (opcional)")]
        public string? Dui { get; set; }
    }
}
