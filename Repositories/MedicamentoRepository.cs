using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Repositories;

public sealed class MedicamentoRepository(ClinicaDbContext context)
    : EfRepository<Medicamento>(context), IMedicamentoRepository
{
    public async Task<Medicamento?> GetByNameAsync(string name) =>
        await Context.Medicamentos.FirstOrDefaultAsync(item => item.Nombre == name);

    public async Task<bool> IsUsedAsync(int id) =>
        await Context.DetallesReceta.AnyAsync(item => item.IdMedicamento == id);
}
