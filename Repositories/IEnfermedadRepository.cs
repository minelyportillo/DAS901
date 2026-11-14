using ClinicaApp.Models;

namespace ClinicaApp.Repositories;

public interface IEnfermedadRepository : IRepository<Enfermedad>
{
    Task<IReadOnlyList<Enfermedad>> GetByPatientAsync(int patientId);
    Task<Enfermedad?> GetByIdAndPatientAsync(int id, int patientId);
}
