using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Repositories;

public sealed class AlergiaPacienteRepository(ClinicaDbContext context)
    : EfRepository<AlergiaPaciente>(context), IAlergiaPacienteRepository
{
    public async Task<IReadOnlyList<AlergiaPaciente>> GetByPatientAsync(int patientId) =>
        await Context.AlergiasPaciente
            .AsNoTracking()
            .Where(item => item.IdPaciente == patientId)
            .OrderBy(item => item.TipoAlergia)
            .ThenBy(item => item.SustanciaElemento)
            .ToListAsync();

    public async Task<AlergiaPaciente?> GetByIdAndPatientAsync(int id, int patientId) =>
        await Context.AlergiasPaciente
            .FirstOrDefaultAsync(item => item.Id == id && item.IdPaciente == patientId);
}
