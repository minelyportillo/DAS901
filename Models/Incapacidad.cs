using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("incapacidades")]
    public class Incapacidad
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_consulta")]
        public int IdConsulta { get; set; }

        [Required]
        [Column("fecha_inicio", TypeName = "date")]
        public DateTime FechaInicio { get; set; }

        [Required]
        [Column("fecha_fin", TypeName = "date")]
        public DateTime FechaFin { get; set; }

        [Required]
        [Column("motivo", TypeName = "nvarchar(max)")]
        public string Motivo { get; set; } = string.Empty;

        [ForeignKey(nameof(IdConsulta))]
        public virtual Consulta Consulta { get; set; } = null!;
    }
}
