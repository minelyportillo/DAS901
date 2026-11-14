using ClinicaApp.Models;
using ClinicaApp.ViewModels;

namespace ClinicaApp.Services
{
    public interface IPacienteService
    {
        Task<IReadOnlyList<Paciente>> BuscarAsync(string? query);
        Task<IEnumerable<Paciente>> ObtenerTodosAsync();
        Task<Paciente?> ObtenerPorIdAsync(int id);
        Task<Paciente?> ObtenerPorDuiAsync(string dui);
        Task<Paciente?> RegistrarPacienteAsync(RegistrarPacienteViewModel model);
        Task<bool> ActualizarPacienteAsync(int id, RegistrarPacienteViewModel model);
        Task<bool> EliminarPacienteSinHistorialAsync(int id);
        Task<IReadOnlyList<AlergiaPaciente>> ObtenerAlergiasAsync(int patientId);
        Task<bool> AgregarAlergiaAsync(AlergiaPacienteViewModel model);
        Task<bool> ActualizarAlergiaAsync(AlergiaPacienteViewModel model);
        Task<bool> EliminarAlergiaAsync(int id, int patientId);
        Task<IReadOnlyList<Enfermedad>> ObtenerEnfermedadesAsync(int patientId);
        Task<bool> AgregarEnfermedadAsync(EnfermedadViewModel model);
        Task<bool> ActualizarEnfermedadAsync(EnfermedadViewModel model);
        Task<bool> EliminarEnfermedadAsync(int id, int patientId);
    }
}
