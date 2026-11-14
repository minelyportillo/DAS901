using ClinicaApp.Models;

namespace ClinicaApp.Repositories
{
    public interface IPacienteRepository : IRepository<Paciente>
    {
        Task<Paciente?> GetByDuiAsync(string dui);
        Task<IEnumerable<Paciente>> SearchAsync(string query);
        Task<bool> HasClinicalHistoryAsync(int patientId);
    }
}
