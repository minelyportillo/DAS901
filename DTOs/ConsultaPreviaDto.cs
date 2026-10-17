using System;
using System.Collections.Generic;

namespace ClinicaApp.DTOs
{
    public class ConsultaPreviaDto
    {
        public int IdConsulta { get; set; }
        public DateTime FechaConsulta { get; set; }
        public string DiagnosticoAnterior { get; set; } = string.Empty;
        public string? MotivoAnterior { get; set; }
        public List<string> MedicamentosPrescritos { get; set; } = new();
    }
}