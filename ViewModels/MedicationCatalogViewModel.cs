namespace ClinicaApp.ViewModels;

public sealed class MedicationCatalogViewModel
{
    public int ConsultationId { get; set; }
    public IReadOnlyList<MedicationOption> Medications { get; set; } = [];
}
