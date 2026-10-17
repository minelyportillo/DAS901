using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("Incapacidades")]
    public class Incapacidad
    {
        [Key]
        public int IdIncapacidad { get; set; }

        [Required]
        public int IdConsulta { get; set; }

        [Required]
        [Range(1, 365)]
        public int DiasReposo { get; set; }

        [Required]
        [StringLength(500)]
        public string Justificacion { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; } = DateTime.Now;

        [ForeignKey("IdConsulta")]
        public virtual Consulta Consulta { get; set; } = null!;
    }
}