using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("signos_vitales")]
    public class SignosVitales
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_consulta")]
        public int IdConsulta { get; set; }

        [Column("temperatura")]
        public double? Temperatura { get; set; }

        [Column("altura")]
        public double? Altura { get; set; }

        [Column("peso")]
        public double? Peso { get; set; }

        [Column("presion_sistolica")]
        public int? PresionSistolica { get; set; }

        [Column("presion_diastolica")]
        public int? PresionDiastolica { get; set; }

        [Column("concentracion_oxigeno")]
        public int? ConcentracionOxigeno { get; set; }

        [Column("ritmo_cardiaco")]
        public int? RitmoCardiaco { get; set; }

        [ForeignKey(nameof(IdConsulta))]
        public virtual Consulta Consulta { get; set; } = null!;
    }
}
