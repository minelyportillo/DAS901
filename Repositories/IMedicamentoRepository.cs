using ClinicaApp.Models;

namespace ClinicaApp.Repositories;

public interface IMedicamentoRepository : IRepository<Medicamento>
{
    Task<Medicamento?> GetByNameAsync(string name);
    Task<bool> IsUsedAsync(int id);
}
