using ClinicaApp.Models;

namespace ClinicaApp.Repositories;

public interface IAlergiaPacienteRepository : IRepository<AlergiaPaciente>
{
    Task<IReadOnlyList<AlergiaPaciente>> GetByPatientAsync(int patientId);
    Task<AlergiaPaciente?> GetByIdAndPatientAsync(int id, int patientId);
}
