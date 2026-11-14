using ClinicaApp.ViewModels;

namespace ClinicaApp.Services;

public interface IMedicamentoService
{
    Task<IReadOnlyList<MedicationOption>> ObtenerCatalogoAsync();
    Task<MedicationOption?> ObtenerPorIdAsync(int id);
    Task<int> RegistrarAsync(string nombre);
    Task<bool> ActualizarAsync(int id, string nombre);
    Task<bool> EliminarSiNoEstaPrescritoAsync(int id);
}
