using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("alergias_paciente")]
    public class AlergiaPaciente
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_paciente")]
        public int IdPaciente { get; set; }

        [Required]
        [StringLength(100)]
        [Column("tipo_alergia")]
        public string TipoAlergia { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Column("sustancia_elemento")]
        public string SustanciaElemento { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("severidad")]
        public string Severidad { get; set; } = string.Empty;

        [ForeignKey(nameof(IdPaciente))]
        public virtual Paciente Paciente { get; set; } = null!;
    }
}
