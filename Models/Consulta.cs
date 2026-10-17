using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicaApp.Models.Enums;

namespace ClinicaApp.Models
{
    [Table("Consultas")]
    public class Consulta
    {
        [Key]
        public int IdConsulta { get; set; }

        [Required]
        public int IdPaciente { get; set; }

        [Required]
        public DateTime HoraLlegada { get; set; } = DateTime.Now;

        public DateTime? HoraInicioAtencion { get; set; }
        public DateTime? HoraCierre { get; set; }

        [Required]
        public PrioridadAtencion Prioridad { get; set; } = PrioridadAtencion.Media;

        [Required]
        public EstadoConsulta Estado { get; set; } = EstadoConsulta.Esperando;

        [StringLength(300)]
        public string? Motivo { get; set; }

        [StringLength(1000)]
        public string? Sintomas { get; set; }

        [StringLength(1000)]
        public string? EvaluacionFisica { get; set; }

        [StringLength(500)]
        public string? Diagnostico { get; set; }

        [StringLength(500)]
        public string? ResultadosExamenesClave { get; set; }

        // Relación con el paciente
        [ForeignKey("IdPaciente")]
        public virtual Paciente Paciente { get; set; } = null!;

        // Relaciones con los datos clínicos
        public virtual SignosVitales? SignosVitales { get; set; }
        public virtual ICollection<RecetaDetalle> Recetas { get; set; } = new List<RecetaDetalle>();
        public virtual ICollection<Incapacidad> Incapacidades { get; set; } = new List<Incapacidad>();
        public virtual ICollection<OrdenExamen> OrdenesExamen { get; set; } = new List<OrdenExamen>();
    }
}