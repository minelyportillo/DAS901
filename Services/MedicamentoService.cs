using ClinicaApp.Data;
using ClinicaApp.Models;
using ClinicaApp.Repositories;
using ClinicaApp.ViewModels;

namespace ClinicaApp.Services;

public sealed class MedicamentoService(
    IRepository<Medicamento> repository,
    IMedicamentoRepository medicamentoRepository,
    IUnitOfWork unitOfWork) : IMedicamentoService
{
    public async Task<IReadOnlyList<MedicationOption>> ObtenerCatalogoAsync()
    {
        var medications = await repository.GetAllAsync();
        return medications
            .OrderBy(item => item.Nombre)
            .Select(item => new MedicationOption(item.Id, item.Nombre))
            .ToArray();
    }

    public async Task<MedicationOption?> ObtenerPorIdAsync(int id)
    {
        var medication = await repository.GetByIdAsync(id);
        return medication is null ? null : new MedicationOption(medication.Id, medication.Nombre);
    }

    public async Task<int> RegistrarAsync(string nombre)
    {
        var cleanName = nombre.Trim();
        var existing = await medicamentoRepository.GetByNameAsync(cleanName);
        if (existing is not null)
        {
            return existing.Id;
        }

        var medication = new Medicamento { Nombre = cleanName };
        await repository.AddAsync(medication);
        await unitOfWork.SaveChangesAsync();
        return medication.Id;
    }

    public async Task<bool> ActualizarAsync(int id, string nombre)
    {
        var medication = await repository.GetByIdAsync(id);
        if (medication is null)
        {
            return false;
        }

        var cleanName = nombre.Trim();
        var duplicate = await medicamentoRepository.GetByNameAsync(cleanName);
        if (duplicate is not null && duplicate.Id != id)
        {
            return false;
        }

        medication.Nombre = cleanName;
        repository.Update(medication);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarSiNoEstaPrescritoAsync(int id)
    {
        if (await medicamentoRepository.IsUsedAsync(id))
        {
            return false;
        }

        var medication = await repository.GetByIdAsync(id);
        if (medication is null)
        {
            return false;
        }

        repository.Delete(medication);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
