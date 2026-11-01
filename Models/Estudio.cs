using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("estudios")]
    public class Estudio
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Column("id_detalle_orden")]
        public int? IdDetalleOrden { get; set; }

        [Required]
        [Column("id_consulta_lectura")]
        public int IdConsultaLectura { get; set; }

        [StringLength(150)]
        [Column("nombre")]
        public string? Nombre { get; set; }

        [Required]
        [Column("observaciones", TypeName = "nvarchar(max)")]
        public string Observaciones { get; set; } = string.Empty;

        [Required]
        [Column("fecha", TypeName = "date")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [ForeignKey(nameof(IdPaciente))]
        public virtual Paciente Paciente { get; set; } = null!;

        [ForeignKey(nameof(IdDetalleOrden))]
        public virtual DetalleOrden? DetalleOrden { get; set; }

        [ForeignKey(nameof(IdConsultaLectura))]
        public virtual Consulta ConsultaLectura { get; set; } = null!;
    }
}
