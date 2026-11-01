using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("pacientes")]
    public class Paciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("fecha_nacimiento", TypeName = "date")]
        public DateTime FechaNacimiento { get; set; }

        [StringLength(10)]
        [Column("DUI")]
        public string? Dui { get; set; }

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

        // Propiedades de navegación
        public virtual ICollection<Enfermedad> Enfermedades { get; set; } = new List<Enfermedad>();
        public virtual ICollection<AlergiaPaciente> Alergias { get; set; } = new List<AlergiaPaciente>();
        public virtual ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
        public virtual ICollection<Estudio> Estudios { get; set; } = new List<Estudio>();
    }
}
