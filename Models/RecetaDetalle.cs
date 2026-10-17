using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("RecetasDetalle")]
    public class RecetaDetalle
    {
        [Key]
        public int IdRecetaDetalle { get; set; }

        [Required]
        public int IdConsulta { get; set; }

        [Required]
        [StringLength(150)]
        public string Medicamento { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Concentracion { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Dosis { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Duracion { get; set; } = string.Empty;

        [ForeignKey("IdConsulta")]
        public virtual Consulta Consulta { get; set; } = null!;
    }
}