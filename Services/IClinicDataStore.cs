using ClinicaApp.Models.Enums;
using ClinicaApp.ViewModels;

namespace ClinicaApp.Services;

public interface IClinicDataStore
{
    Task<IReadOnlyList<PatientListItem>> SearchPatientsAsync(string? term);
    Task<PatientChartData?> GetPatientChartAsync(int patientId);
    Task<IReadOnlyList<WaitingRoomItem>> GetWaitingRoomAsync(DateTime date);
    Task<int> RegisterArrivalAsync(int patientId);
    Task<bool> SetPriorityAsync(int consultationId, PrioridadAtencion priority);
    Task<bool> SaveTriageAsync(TriageViewModel model);
    Task<ConsultationPageData?> StartConsultationAsync(int consultationId);
    Task<ConsultationPageData?> GetConsultationAsync(int consultationId);
    Task<bool> SaveClinicalNotesAsync(ClinicalConsultationViewModel model);
    Task<bool> CloseConsultationAsync(int consultationId);
    Task<bool> SavePrescriptionAsync(int consultationId, PrescriptionViewModel model);
    Task<bool> DeletePrescriptionDetailAsync(int consultationId, int detalleId);
    Task<bool> SaveExamOrderAsync(int consultationId, ExamOrderViewModel model);
    Task<bool> DeleteExamOrderDetailAsync(int consultationId, int detalleOrdenId);
    Task<bool> SaveStudyAsync(StudyViewModel model);
    Task<bool> DeleteStudyAsync(int consultationId, int estudioId);
    Task<bool> AddIncapacityAsync(int consultationId, IncapacityViewModel model);
    Task<bool> DeleteIncapacityAsync(int consultationId);
}
