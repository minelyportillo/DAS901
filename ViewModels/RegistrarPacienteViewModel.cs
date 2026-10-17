using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels
{
    public class RegistrarPacienteViewModel
    {
        [Required(ErrorMessage = "El nombre del paciente es obligatorio.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-18);

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^[267][0-9]{3}-?[0-9]{4}$", ErrorMessage = "Formato de teléfono no válido (ej. 7123-4567 o 2222-3333).")]
        [Display(Name = "Teléfono de contacto")]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Las alergias no pueden exceder los 500 caracteres.")]
        [Display(Name = "Alergias conocidas")]
        public string? Alergias { get; set; }
    }
}