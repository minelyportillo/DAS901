using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("receta")]
    public class Receta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_consulta")]
        public int IdConsulta { get; set; }

        [ForeignKey(nameof(IdConsulta))]
        public virtual Consulta Consulta { get; set; } = null!;

        public virtual ICollection<DetalleReceta> Detalles { get; set; } = new List<DetalleReceta>();
    }
}
