using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("ordenes")]
    public class Orden
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("consulta_id")]
        public int ConsultaId { get; set; }

        [Required]
        [Column("fecha_hora_emision")]
        public DateTime FechaHoraEmision { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ConsultaId))]
        public virtual Consulta Consulta { get; set; } = null!;

        public virtual ICollection<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();
    }
}
