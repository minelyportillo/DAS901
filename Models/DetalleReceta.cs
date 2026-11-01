using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("detalle_receta")]
    public class DetalleReceta
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_receta")]
        public int IdReceta { get; set; }

        [Column("id_medicamento")]
        public int? IdMedicamento { get; set; }

        [Required]
        [StringLength(100)]
        [Column("dosis")]
        public string Dosis { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("frecuencia")]
        public string Frecuencia { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("duracion")]
        public string Duracion { get; set; } = string.Empty;

        [Column("indicaciones", TypeName = "nvarchar(max)")]
        public string Indicaciones { get; set; } = string.Empty;

        [ForeignKey(nameof(IdReceta))]
        public virtual Receta Receta { get; set; } = null!;

        [ForeignKey(nameof(IdMedicamento))]
        public virtual Medicamento? Medicamento { get; set; }
    }
}
