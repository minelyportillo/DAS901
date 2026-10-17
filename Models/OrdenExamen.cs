using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicaApp.Models.Enums;

namespace ClinicaApp.Models
{
    [Table("OrdenesExamen")]
    public class OrdenExamen
    {
        [Key]
        public int IdOrdenExamen { get; set; }

        [Required]
        public int IdConsulta { get; set; }

        [Required]
        [StringLength(150)]
        public string TipoExamen { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Indicaciones { get; set; }

        [Required]
        public EstadoOrdenExamen Estado { get; set; } = EstadoOrdenExamen.Pendiente;

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [ForeignKey("IdConsulta")]
        public virtual Consulta Consulta { get; set; } = null!;
    }
}