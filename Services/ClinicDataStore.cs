using System.Text.Json;
using ClinicaApp.Models.Enums;
using ClinicaApp.ViewModels;

namespace ClinicaApp.Services;

public sealed class ClinicDataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ClinicDataStore(IWebHostEnvironment environment)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "App_Data", "clinic-data.json");
    }

    public async Task<IReadOnlyList<PatientListItem>> SearchPatientsAsync(string? term)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            return data.Patients
                .Where(patient => string.IsNullOrWhiteSpace(term)
                    || patient.Name.Contains(term.Trim(), StringComparison.CurrentCultureIgnoreCase)
                    || patient.Phone.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(patient => patient.Name)
                .Select(ToListItem)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CreatePatientAsync(RegistrarPacienteViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            var patient = new PatientRecord
            {
                Id = data.Patients.Count == 0 ? 1 : data.Patients.Max(item => item.Id) + 1,
                Name = model.NombreCompleto.Trim(),
                DateOfBirth = model.FechaNacimiento.Date,
                Phone = model.Telefono.Trim(),
                Allergies = model.Alergias?.Trim(),
                RegisteredAt = DateTime.Now
            };
            data.Patients.Add(patient);
            await WriteAsync(data);
            return patient.Id;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PatientChartData?> GetPatientChartAsync(int patientId)
    {
        await _gate.WaitAsync();
        try
        {
            var patient = (await ReadAsync()).Patients.FirstOrDefault(item => item.Id == patientId);
            if (patient is null)
            {
                return null;
            }

            var consultations = patient.Consultations
                .OrderByDescending(consultation => consultation.ArrivalTime)
                .ToArray();
            return new PatientChartData(
                ToListItem(patient),
                patient.Allergies,
                patient.Antecedents.ToArray(),
                consultations);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddAntecedentAsync(AntecedentViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            var patient = data.Patients.FirstOrDefault(item => item.Id == model.PatientId);
            if (patient is null)
            {
                return false;
            }

            patient.Antecedents.Add(new AntecedentRecord
            {
                Id = patient.Antecedents.Select(item => item.Id).DefaultIfEmpty(0).Max() + 1,
                Category = model.Category.Trim(),
                Description = model.Description.Trim(),
                RecordedAt = DateTime.Now
            });
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<WaitingRoomItem>> GetWaitingRoomAsync(DateTime date)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            return data.Patients
                .SelectMany(patient => patient.Consultations
                    .Where(consultation => consultation.ArrivalTime.Date == date.Date
                        && consultation.State != EstadoConsulta.Cerrada)
                    .Select(consultation => new WaitingRoomItem(
                        consultation.Id,
                        patient.Id,
                        patient.Name,
                        patient.DateOfBirth,
                        patient.Phone,
                        consultation.ArrivalTime,
                        consultation.StartTime,
                        consultation.Priority,
                        consultation.State,
                        consultation.Vitals?.WeightKg,
                        consultation.Vitals?.HeightMeters,
                        consultation.Vitals?.Temperature)))
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.ArrivalTime)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> RegisterArrivalAsync(int patientId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            var patient = data.Patients.FirstOrDefault(item => item.Id == patientId)
                ?? throw new KeyNotFoundException("No se encontró el paciente.");
            if (patient.Consultations.Any(item =>
                item.ArrivalTime.Date == DateTime.Today && item.State != EstadoConsulta.Cerrada))
            {
                throw new InvalidOperationException("Este paciente ya tiene una atención pendiente hoy.");
            }

            var consultation = new ConsultationRecord
            {
                Id = data.Patients.SelectMany(item => item.Consultations)
                    .Select(item => item.Id)
                    .DefaultIfEmpty(0)
                    .Max() + 1,
                ArrivalTime = DateTime.Now,
                Priority = PrioridadAtencion.Media,
                State = EstadoConsulta.Esperando
            };
            patient.Consultations.Add(consultation);
            await WriteAsync(data);
            return consultation.Id;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SetPriorityAsync(int consultationId, PrioridadAtencion priority)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.Esperando)
            {
                return false;
            }

            consultation.Priority = priority;
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SaveTriageAsync(TriageViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(model.ConsultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.Esperando)
            {
                return false;
            }

            consultation.Vitals ??= new VitalsRecord();
            consultation.Vitals.WeightKg = model.WeightKg;
            consultation.Vitals.HeightMeters = model.HeightMeters;
            consultation.Vitals.Temperature = model.Temperature;
            consultation.Priority = model.Priority;
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ConsultationPageData?> StartConsultationAsync(int consultationId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            var found = FindConsultation(data, consultationId);
            if (found.Patient is null || found.Consultation is null
                || found.Consultation.State == EstadoConsulta.Cerrada)
            {
                return null;
            }

            if (found.Consultation.State == EstadoConsulta.Esperando)
            {
                found.Consultation.State = EstadoConsulta.EnAtencion;
                found.Consultation.StartTime = DateTime.Now;
                await WriteAsync(data);
            }

            return BuildConsultationPage(data, found.Patient, found.Consultation);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ConsultationPageData?> GetConsultationAsync(int consultationId)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            var found = FindConsultation(data, consultationId);
            return found.Patient is null || found.Consultation is null
                ? null
                : BuildConsultationPage(data, found.Patient, found.Consultation);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SaveClinicalNotesAsync(ClinicalConsultationViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(model.ConsultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            consultation.Reason = model.Reason.Trim();
            consultation.Symptoms = model.Symptoms?.Trim();
            consultation.PhysicalEvaluation = model.PhysicalEvaluation?.Trim();
            consultation.Diagnosis = model.Diagnosis.Trim();
            consultation.ExamResults = model.ExamResults?.Trim();
            consultation.Vitals ??= new VitalsRecord();
            consultation.Vitals.BloodPressure = model.BloodPressure?.Trim();
            consultation.Vitals.HeartRate = model.HeartRate;
            consultation.Vitals.OxygenSaturation = model.OxygenSaturation;
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> CloseConsultationAsync(int consultationId)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(consultation.Reason)
                || string.IsNullOrWhiteSpace(consultation.Diagnosis))
            {
                throw new InvalidOperationException("Guarda el motivo y el diagnóstico antes de cerrar la consulta.");
            }

            consultation.State = EstadoConsulta.Cerrada;
            consultation.CloseTime = DateTime.Now;
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddPrescriptionAsync(int consultationId, PrescriptionViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            consultation.Prescriptions.Add(new PrescriptionRecord
            {
                Id = NextId(data, item => item.Patients
                    .SelectMany(record => record.Consultations)
                    .SelectMany(record => record.Prescriptions)
                    .Select(record => record.Id)),
                Medication = model.Medication.Trim(),
                Concentration = model.Concentration.Trim(),
                Dose = model.Dose.Trim(),
                Duration = model.Duration.Trim()
            });
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddExamOrderAsync(int consultationId, ExamOrderViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            consultation.ExamOrders.Add(new ExamOrderRecord
            {
                Id = NextId(data, item => item.Patients
                    .SelectMany(record => record.Consultations)
                    .SelectMany(record => record.ExamOrders)
                    .Select(record => record.Id)),
                ExamType = model.ExamType.Trim(),
                Instructions = model.Instructions?.Trim(),
                RequestedAt = DateTime.Now,
                State = EstadoOrdenExamen.Pendiente
            });
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddIncapacityAsync(int consultationId, IncapacityViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.State != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            consultation.Incapacity = new IncapacityRecord
            {
                Days = model.Days,
                Justification = model.Justification.Trim(),
                IssuedAt = DateTime.Now
            };
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SetExamOrderStateAsync(
        int consultationId,
        int orderId,
        EstadoOrdenExamen state)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            var order = consultation?.ExamOrders.FirstOrDefault(item => item.Id == orderId);
            if (patient is null || consultation is null || order is null)
            {
                return false;
            }

            order.State = state;
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<(ClinicData Data, PatientRecord? Patient, ConsultationRecord? Consultation)> FindConsultationAsync(
        int consultationId)
    {
        var data = await ReadAsync();
        var found = FindConsultation(data, consultationId);
        return (data, found.Patient, found.Consultation);
    }

    private static (PatientRecord? Patient, ConsultationRecord? Consultation) FindConsultation(
        ClinicData data,
        int consultationId)
    {
        foreach (var patient in data.Patients)
        {
            var consultation = patient.Consultations.FirstOrDefault(item => item.Id == consultationId);
            if (consultation is not null)
            {
                return (patient, consultation);
            }
        }

        return (null, null);
    }

    private static ConsultationPageData BuildConsultationPage(
        ClinicData data,
        PatientRecord patient,
        ConsultationRecord consultation)
    {
        var previous = patient.Consultations
            .Where(item => item.Id != consultation.Id && item.State == EstadoConsulta.Cerrada)
            .OrderByDescending(item => item.CloseTime)
            .FirstOrDefault();

        return new ConsultationPageData(
            ToListItem(patient),
            consultation,
            previous is null ? null : new PreviousConsultationData(
                previous.CloseTime,
                previous.Reason,
                previous.Diagnosis,
                previous.Prescriptions.ToArray()));
    }

    private static PatientListItem ToListItem(PatientRecord patient) =>
        new(patient.Id, patient.Name, patient.DateOfBirth, patient.Phone, patient.RegisteredAt);

    private static int NextId(ClinicData data, Func<ClinicData, IEnumerable<int>> ids) =>
        ids(data).DefaultIfEmpty(0).Max() + 1;

    private async Task<ClinicData> ReadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new ClinicData();
        }

        await using var stream = new FileStream(
            _filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        return await JsonSerializer.DeserializeAsync<ClinicData>(stream, JsonOptions)
            ?? throw new InvalidDataException("El archivo de pacientes y consultas no contiene datos válidos.");
    }

    private async Task WriteAsync(ClinicData data)
    {
        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("No se pudo resolver la carpeta de datos clínicos.");
        Directory.CreateDirectory(directory);

        var temporaryFile = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
                await stream.FlushAsync();
            }

            File.Move(temporaryFile, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    private sealed class ClinicData
    {
        public List<PatientRecord> Patients { get; set; } = [];
    }

    public sealed class PatientRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Allergies { get; set; }
        public DateTime RegisteredAt { get; set; }
        public List<AntecedentRecord> Antecedents { get; set; } = [];
        public List<ConsultationRecord> Consultations { get; set; } = [];
    }

    public sealed class AntecedentRecord
    {
        public int Id { get; set; }
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
    }

    public sealed class ConsultationRecord
    {
        public int Id { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? CloseTime { get; set; }
        public PrioridadAtencion Priority { get; set; }
        public EstadoConsulta State { get; set; }
        public string? Reason { get; set; }
        public string? Symptoms { get; set; }
        public string? PhysicalEvaluation { get; set; }
        public string? Diagnosis { get; set; }
        public string? ExamResults { get; set; }
        public VitalsRecord? Vitals { get; set; }
        public List<PrescriptionRecord> Prescriptions { get; set; } = [];
        public IncapacityRecord? Incapacity { get; set; }
        public List<ExamOrderRecord> ExamOrders { get; set; } = [];
    }

    public sealed class VitalsRecord
    {
        public decimal? WeightKg { get; set; }
        public decimal? HeightMeters { get; set; }
        public decimal? Temperature { get; set; }
        public string? BloodPressure { get; set; }
        public int? HeartRate { get; set; }
        public int? OxygenSaturation { get; set; }
    }

    public sealed class PrescriptionRecord
    {
        public int Id { get; set; }
        public string Medication { get; set; } = string.Empty;
        public string Concentration { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }

    public sealed class IncapacityRecord
    {
        public int Days { get; set; }
        public string Justification { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
    }

    public sealed class ExamOrderRecord
    {
        public int Id { get; set; }
        public string ExamType { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateTime RequestedAt { get; set; }
        public EstadoOrdenExamen State { get; set; }
    }
}

public sealed record PatientListItem(
    int Id,
    string Name,
    DateTime DateOfBirth,
    string Phone,
    DateTime RegisteredAt)
{
    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            return DateOfBirth.Date > today.AddYears(-age) ? age - 1 : age;
        }
    }
}

public sealed record PatientChartData(
    PatientListItem Patient,
    string? Allergies,
    IReadOnlyList<ClinicDataStore.AntecedentRecord> Antecedents,
    IReadOnlyList<ClinicDataStore.ConsultationRecord> Consultations);

public sealed record WaitingRoomItem(
    int ConsultationId,
    int PatientId,
    string PatientName,
    DateTime DateOfBirth,
    string Phone,
    DateTime ArrivalTime,
    DateTime? StartTime,
    PrioridadAtencion Priority,
    EstadoConsulta State,
    decimal? WeightKg,
    decimal? HeightMeters,
    decimal? Temperature)
{
    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            return DateOfBirth.Date > today.AddYears(-age) ? age - 1 : age;
        }
    }
}

public sealed record ConsultationPageData(
    PatientListItem Patient,
    ClinicDataStore.ConsultationRecord Consultation,
    PreviousConsultationData? Previous);

public sealed record PreviousConsultationData(
    DateTime? ClosedAt,
    string? Reason,
    string? Diagnosis,
    IReadOnlyList<ClinicDataStore.PrescriptionRecord> Prescriptions);
