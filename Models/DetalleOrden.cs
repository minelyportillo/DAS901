using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("detalle_orden")]
    public class DetalleOrden
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_orden")]
        public int IdOrden { get; set; }

        [Required]
        [StringLength(150)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("indicaciones", TypeName = "nvarchar(max)")]
        public string Indicaciones { get; set; } = string.Empty;

        [ForeignKey(nameof(IdOrden))]
        public virtual Orden Orden { get; set; } = null!;

        public virtual ICollection<Estudio> Estudios { get; set; } = new List<Estudio>();
    }
}
