using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("Pacientes")]
    public class Paciente
    {
        [Key]
        public int IdPaciente { get; set; }

        [Required]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "date")]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Alergias { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedad calculada en memoria (no genera columna en la base de datos)
        [NotMapped]
        public int Edad
        {
            get
            {
                var hoy = DateTime.Today;
                var edad = hoy.Year - FechaNacimiento.Year;
                if (FechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
                return edad;
            }
        }

        // Propiedades de navegación hacia las demás entidades
        public virtual ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
        public virtual ICollection<AntecedenteMedico> Antecedentes { get; set; } = new List<AntecedenteMedico>();
    }
}