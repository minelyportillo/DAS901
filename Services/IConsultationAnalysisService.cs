using ClinicaApp.ViewModels;

namespace ClinicaApp.Services;

public interface IConsultationAnalysisService
{
    Task<ConsultationAnalysisViewModel> GetReportAsync(
        ConsultationAnalysisViewModel filters,
        CancellationToken cancellationToken);
}
