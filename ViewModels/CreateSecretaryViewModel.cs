using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.ViewModels;

public sealed class CreateSecretaryViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 100 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [StringLength(254)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Usa al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña temporal")]
    public string Password { get; set; } = string.Empty;
}
