using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Repositories
{
    public sealed class PacienteRepository(ClinicaDbContext context)
        : EfRepository<Paciente>(context), IPacienteRepository
    {
        public async Task<Paciente?> GetByDuiAsync(string dui)
        {
            return await Context.Pacientes.FirstOrDefaultAsync(p => p.Dui == dui);
        }

        public async Task<IEnumerable<Paciente>> SearchAsync(string query)
        {
            var normalizedQuery = query.Trim();

            return await Context.Pacientes
                .Where(p =>
                    p.Nombre.Contains(normalizedQuery) ||
                    (p.Dui != null && p.Dui.Contains(normalizedQuery)))
                .ToListAsync();
        }

        public async Task<bool> HasClinicalHistoryAsync(int patientId)
        {
            return await Context.AlergiasPaciente.AnyAsync(item => item.IdPaciente == patientId)
                || await Context.Enfermedades.AnyAsync(item => item.IdPaciente == patientId)
                || await Context.Consultas.AnyAsync(item => item.IdPaciente == patientId)
                || await Context.Estudios.AnyAsync(item => item.IdPaciente == patientId);
        }
    }
}
