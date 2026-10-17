using ClinicaApp.Services;

namespace ClinicaApp.ViewModels;

public sealed class PatientSearchViewModel
{
    public string? Term { get; set; }
    public IReadOnlyList<PatientListItem> Patients { get; set; } = [];
}
