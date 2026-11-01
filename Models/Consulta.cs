using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ClinicaApp.Models.Enums;

namespace ClinicaApp.Models
{
    [Table("consultas")]
    public class Consulta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Required]
        [Column("fecha_hora_inicio")]
        public DateTime FechaHoraInicio { get; set; } = DateTime.Now;

        [Column("fecha_hora_finalizacion")]
        public DateTime? FechaHoraFinalizacion { get; set; }

        [Column("motivo", TypeName = "nvarchar(max)")]
        public string? Motivo { get; set; }

        [Column("examen_fisico", TypeName = "nvarchar(max)")]
        public string? ExamenFisico { get; set; }

        [StringLength(500)]
        [Column("diagnostico")]
        public string? Diagnostico { get; set; }

        [Required]
        [Column("prioridad")]
        public PrioridadAtencion Prioridad { get; set; } = PrioridadAtencion.Media;

        [Required]
        [Column("estado")]
        public EstadoConsulta Estado { get; set; } = EstadoConsulta.Esperando;

        [ForeignKey(nameof(IdPaciente))]
        public virtual Paciente Paciente { get; set; } = null!;

        // Relaciones 1:1 y 1:0..1
        public virtual SignosVitales? SignosVitales { get; set; }
        public virtual Incapacidad? Incapacidad { get; set; }

        // Relaciones 1:N
        public virtual ICollection<Receta> Recetas { get; set; } = new List<Receta>();
        public virtual ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
        public virtual ICollection<Estudio> EstudiosLeidos { get; set; } = new List<Estudio>();
    }
}
