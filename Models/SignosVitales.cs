using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("SignosVitales")]
    public class SignosVitales
    {
        [Key]
        public int IdSignoVital { get; set; }

        [Required]
        public int IdConsulta { get; set; }

        // Datos registrados por Secretaría
        [Column(TypeName = "decimal(5,2)")]
        public decimal? PesoKg { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? AlturaMetros { get; set; }

        [Column(TypeName = "decimal(4,1)")]
        public decimal? Temperatura { get; set; }

        // Datos complementados por Doctora
        [StringLength(10)]
        public string? PresionArterial { get; set; }

        public int? FrecuenciaCardiaca { get; set; }
        public int? SaturacionOxigeno { get; set; }

        // Relación 1 a 1 con Consulta
        [ForeignKey("IdConsulta")]
        public virtual Consulta Consulta { get; set; } = null!;
    }
}