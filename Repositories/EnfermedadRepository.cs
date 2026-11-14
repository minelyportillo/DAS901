using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Repositories;

public sealed class EnfermedadRepository(ClinicaDbContext context)
    : EfRepository<Enfermedad>(context), IEnfermedadRepository
{
    public async Task<IReadOnlyList<Enfermedad>> GetByPatientAsync(int patientId) =>
        await Context.Enfermedades
            .AsNoTracking()
            .Where(item => item.IdPaciente == patientId)
            .OrderByDescending(item => item.AnioDiagnostico)
            .ThenBy(item => item.Nombre)
            .ToListAsync();

    public async Task<Enfermedad?> GetByIdAndPatientAsync(int id, int patientId) =>
        await Context.Enfermedades
            .FirstOrDefaultAsync(item => item.Id == id && item.IdPaciente == patientId);
}
