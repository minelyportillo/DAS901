using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicaApp.Models
{
    [Table("AntecedentesMedicos")]
    public class AntecedenteMedico
    {
        [Key]
        public int IdAntecedente { get; set; }

        [Required]
        public int IdPaciente { get; set; }

        [Required]
        [StringLength(150)]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(50)]
        public string Categoria { get; set; } = "General";

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey("IdPaciente")]
        public virtual Paciente Paciente { get; set; } = null!;
    }
}