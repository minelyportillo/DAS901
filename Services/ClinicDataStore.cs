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

    private static readonly string[] DefaultMedications =
    [
        "Paracetamol 500 mg",
        "Ibuprofeno 400 mg",
        "Amoxicilina 500 mg",
        "Omeprazol 20 mg",
        "Loratadina 10 mg",
        "Azitromicina 500 mg",
        "Losartán 50 mg",
        "Metformina 850 mg"
    ];

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
                    || patient.Nombre.Contains(term.Trim(), StringComparison.CurrentCultureIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(patient.Dui)
                        && patient.Dui.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase)))
                .OrderBy(patient => patient.Nombre)
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
                Nombre = model.Nombre.Trim(),
                FechaNacimiento = model.FechaNacimiento.Date,
                Dui = string.IsNullOrWhiteSpace(model.Dui) ? null : model.Dui.Trim()
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
            var data = await ReadAsync();
            var patient = data.Patients.FirstOrDefault(item => item.Id == patientId);
            if (patient is null)
            {
                return null;
            }

            var consultations = patient.Consultations
                .OrderByDescending(consultation => consultation.FechaHoraInicio)
                .ToArray();

            var studies = BuildPatientStudies(patient);

            return new PatientChartData(
                ToListItem(patient),
                patient.Alergias.ToArray(),
                patient.Enfermedades.OrderByDescending(item => item.AnioDiagnostico).ToArray(),
                consultations,
                studies);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddAlergiaAsync(AlergiaPacienteViewModel model)
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

            var nextId = NextId(data, d => d.Patients.SelectMany(p => p.Alergias).Select(a => a.Id));
            patient.Alergias.Add(new AlergiaRecord
            {
                Id = nextId,
                IdPaciente = patient.Id,
                TipoAlergia = model.TipoAlergia.Trim(),
                SustanciaElemento = model.SustanciaElemento.Trim(),
                Severidad = model.Severidad.Trim()
            });
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> AddEnfermedadAsync(EnfermedadViewModel model)
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

            var nextId = NextId(data, d => d.Patients.SelectMany(p => p.Enfermedades).Select(e => e.Id));
            patient.Enfermedades.Add(new EnfermedadRecord
            {
                Id = nextId,
                IdPaciente = patient.Id,
                Nombre = model.Nombre.Trim(),
                AnioDiagnostico = model.AnioDiagnostico
            });
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<MedicamentoRecord>> GetMedicamentosAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            return data.Medicamentos
                .OrderBy(item => item.Nombre)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> CreateMedicamentoAsync(string nombre)
    {
        await _gate.WaitAsync();
        try
        {
            var data = await ReadAsync();
            var nombreLimpio = nombre.Trim();
            var existente = data.Medicamentos.FirstOrDefault(m =>
                string.Equals(m.Nombre, nombreLimpio, StringComparison.OrdinalIgnoreCase));

            if (existente is not null)
            {
                return existente.Id;
            }

            var nuevo = new MedicamentoRecord
            {
                Id = NextId(data, d => d.Medicamentos.Select(m => m.Id)),
                Nombre = nombreLimpio
            };
            data.Medicamentos.Add(nuevo);
            await WriteAsync(data);
            return nuevo.Id;
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
                    .Where(consultation => consultation.FechaHoraInicio.Date == date.Date
                        && consultation.Estado != EstadoConsulta.Cerrada)
                    .Select(consultation => new WaitingRoomItem(
                        consultation.Id,
                        patient.Id,
                        patient.Nombre,
                        patient.FechaNacimiento,
                        patient.Dui,
                        consultation.FechaHoraInicio,
                        consultation.Prioridad,
                        consultation.Estado,
                        consultation.SignosVitales?.Peso,
                        consultation.SignosVitales?.Altura,
                        consultation.SignosVitales?.Temperatura)))
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.StartTime)
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
                item.FechaHoraInicio.Date == DateTime.Today && item.Estado != EstadoConsulta.Cerrada))
            {
                throw new InvalidOperationException("Este paciente ya tiene una consulta activa hoy.");
            }

            var consultation = new ConsultationRecord
            {
                Id = NextId(data, d => d.Patients.SelectMany(p => p.Consultations).Select(c => c.Id)),
                IdPaciente = patient.Id,
                FechaHoraInicio = DateTime.Now,
                Prioridad = PrioridadAtencion.Media,
                Estado = EstadoConsulta.Esperando
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
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.Esperando)
            {
                return false;
            }

            consultation.Prioridad = priority;
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
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.Esperando)
            {
                return false;
            }

            consultation.SignosVitales ??= new SignosVitalesRecord
            {
                Id = NextId(data, d => d.Patients
                    .SelectMany(p => p.Consultations)
                    .Where(c => c.SignosVitales is not null)
                    .Select(c => c.SignosVitales!.Id)),
                IdConsulta = consultation.Id
            };
            consultation.SignosVitales.Peso = model.Peso;
            consultation.SignosVitales.Altura = model.Altura;
            consultation.SignosVitales.Temperatura = model.Temperatura;
            consultation.Prioridad = model.Prioridad;
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
                || found.Consultation.Estado == EstadoConsulta.Cerrada)
            {
                return null;
            }

            if (found.Consultation.Estado == EstadoConsulta.Esperando)
            {
                found.Consultation.Estado = EstadoConsulta.EnAtencion;
                await WriteAsync(data);
            }

            return BuildConsultationPage(found.Patient, found.Consultation);
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
                : BuildConsultationPage(found.Patient, found.Consultation);
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
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            consultation.Motivo = model.Motivo?.Trim();
            consultation.ExamenFisico = model.ExamenFisico?.Trim();
            consultation.Diagnostico = model.Diagnostico?.Trim();

            consultation.SignosVitales ??= new SignosVitalesRecord
            {
                Id = NextId(data, d => d.Patients
                    .SelectMany(p => p.Consultations)
                    .Where(c => c.SignosVitales is not null)
                    .Select(c => c.SignosVitales!.Id)),
                IdConsulta = consultation.Id
            };
            consultation.SignosVitales.Peso = model.Peso;
            consultation.SignosVitales.Altura = model.Altura;
            consultation.SignosVitales.Temperatura = model.Temperatura;
            consultation.SignosVitales.PresionSistolica = model.PresionSistolica;
            consultation.SignosVitales.PresionDiastolica = model.PresionDiastolica;
            consultation.SignosVitales.RitmoCardiaco = model.RitmoCardiaco;
            consultation.SignosVitales.ConcentracionOxigeno = model.ConcentracionOxigeno;

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
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(consultation.Motivo)
                || string.IsNullOrWhiteSpace(consultation.Diagnostico))
            {
                throw new InvalidOperationException("Guarda el motivo y el diagnóstico antes de finalizar la consulta.");
            }

            consultation.Estado = EstadoConsulta.Cerrada;
            consultation.FechaHoraFinalizacion = DateTime.Now;
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SavePrescriptionAsync(int consultationId, PrescriptionViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            if (!model.IdMedicamento.HasValue)
            {
                return false;
            }

            var medicamento = data.Medicamentos.FirstOrDefault(m => m.Id == model.IdMedicamento.Value);
            if (medicamento is null)
            {
                return false;
            }

            if (model.DetalleId.HasValue)
            {
                var existente = consultation.Recetas
                    .SelectMany(r => r.Detalles)
                    .FirstOrDefault(d => d.Id == model.DetalleId.Value);
                if (existente is null)
                {
                    return false;
                }

                existente.IdMedicamento = medicamento.Id;
                existente.NombreMedicamento = medicamento.Nombre;
                existente.Dosis = model.Dosis.Trim();
                existente.Frecuencia = model.Frecuencia.Trim();
                existente.Duracion = model.Duracion.Trim();
                existente.Indicaciones = string.IsNullOrWhiteSpace(model.Indicaciones) ? string.Empty : model.Indicaciones.Trim();
                await WriteAsync(data);
                return true;
            }

            var receta = consultation.Recetas.FirstOrDefault();
            if (receta is null)
            {
                receta = new RecetaRecord
                {
                    Id = NextId(data, d => d.Patients
                        .SelectMany(p => p.Consultations)
                        .SelectMany(c => c.Recetas)
                        .Select(r => r.Id)),
                    IdConsulta = consultation.Id
                };
                consultation.Recetas.Add(receta);
            }

            var nextDetalleId = NextId(data, d => d.Patients
                .SelectMany(p => p.Consultations)
                .SelectMany(c => c.Recetas)
                .SelectMany(r => r.Detalles)
                .Select(det => det.Id));

            receta.Detalles.Add(new DetalleRecetaRecord
            {
                Id = nextDetalleId,
                IdReceta = receta.Id,
                IdMedicamento = medicamento.Id,
                NombreMedicamento = medicamento.Nombre,
                Dosis = model.Dosis.Trim(),
                Frecuencia = model.Frecuencia.Trim(),
                Duracion = model.Duracion.Trim(),
                Indicaciones = string.IsNullOrWhiteSpace(model.Indicaciones) ? string.Empty : model.Indicaciones.Trim()
            });

            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeletePrescriptionDetailAsync(int consultationId, int detalleId)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            foreach (var receta in consultation.Recetas.ToList())
            {
                var detalle = receta.Detalles.FirstOrDefault(d => d.Id == detalleId);
                if (detalle is not null)
                {
                    receta.Detalles.Remove(detalle);
                    if (receta.Detalles.Count == 0)
                    {
                        consultation.Recetas.Remove(receta);
                    }
                    await WriteAsync(data);
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SaveExamOrderAsync(int consultationId, ExamOrderViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            if (model.DetalleOrdenId.HasValue)
            {
                var existente = consultation.Ordenes
                    .SelectMany(o => o.Detalles)
                    .FirstOrDefault(d => d.Id == model.DetalleOrdenId.Value);
                if (existente is null)
                {
                    return false;
                }

                existente.Nombre = model.Nombre.Trim();
                existente.Indicaciones = string.IsNullOrWhiteSpace(model.Indicaciones) ? string.Empty : model.Indicaciones.Trim();
                await WriteAsync(data);
                return true;
            }

            var orden = consultation.Ordenes.FirstOrDefault();
            if (orden is null)
            {
                orden = new OrdenRecord
                {
                    Id = NextId(data, d => d.Patients
                        .SelectMany(p => p.Consultations)
                        .SelectMany(c => c.Ordenes)
                        .Select(o => o.Id)),
                    ConsultaId = consultation.Id,
                    FechaHoraEmision = DateTime.Now
                };
                consultation.Ordenes.Add(orden);
            }

            var nextDetalleId = NextId(data, d => d.Patients
                .SelectMany(p => p.Consultations)
                .SelectMany(c => c.Ordenes)
                .SelectMany(o => o.Detalles)
                .Select(det => det.Id));

            orden.Detalles.Add(new DetalleOrdenRecord
            {
                Id = nextDetalleId,
                IdOrden = orden.Id,
                Nombre = model.Nombre.Trim(),
                Indicaciones = string.IsNullOrWhiteSpace(model.Indicaciones) ? string.Empty : model.Indicaciones.Trim()
            });

            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteExamOrderDetailAsync(int consultationId, int detalleOrdenId)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            foreach (var orden in consultation.Ordenes.ToList())
            {
                var detalle = orden.Detalles.FirstOrDefault(d => d.Id == detalleOrdenId);
                if (detalle is not null)
                {
                    orden.Detalles.Remove(detalle);
                    if (orden.Detalles.Count == 0)
                    {
                        consultation.Ordenes.Remove(orden);
                    }
                    await WriteAsync(data);
                    return true;
                }
            }

            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> SaveStudyAsync(StudyViewModel model)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(model.ConsultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            DetalleOrdenRecord? detalleOrden = null;
            if (model.IdDetalleOrden.HasValue)
            {
                detalleOrden = patient.Consultations
                    .Where(c => c.Id != consultation.Id)
                    .SelectMany(c => c.Ordenes)
                    .SelectMany(o => o.Detalles)
                    .FirstOrDefault(d => d.Id == model.IdDetalleOrden.Value);
                if (detalleOrden is null)
                {
                    return false;
                }
            }

            if (model.EstudioId.HasValue)
            {
                var existente = patient.Estudios.FirstOrDefault(e =>
                    e.Id == model.EstudioId.Value && e.IdConsultaLectura == consultation.Id);
                if (existente is null)
                {
                    return false;
                }

                existente.IdDetalleOrden = detalleOrden?.Id;
                existente.Nombre = string.IsNullOrWhiteSpace(model.Nombre) ? null : model.Nombre.Trim();
                existente.Observaciones = model.Observaciones.Trim();
                existente.Fecha = model.Fecha.Date;
                await WriteAsync(data);
                return true;
            }

            var nextStudyId = NextId(data, d => d.Patients.SelectMany(p => p.Estudios).Select(e => e.Id));
            patient.Estudios.Add(new EstudioRecord
            {
                Id = nextStudyId,
                IdPaciente = patient.Id,
                IdDetalleOrden = detalleOrden?.Id,
                IdConsultaLectura = consultation.Id,
                Nombre = string.IsNullOrWhiteSpace(model.Nombre) ? null : model.Nombre.Trim(),
                Observaciones = model.Observaciones.Trim(),
                Fecha = model.Fecha.Date
            });

            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteStudyAsync(int consultationId, int estudioId)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            var estudio = patient.Estudios.FirstOrDefault(e =>
                e.Id == estudioId && e.IdConsultaLectura == consultation.Id);
            if (estudio is null)
            {
                return false;
            }

            patient.Estudios.Remove(estudio);
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
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            var existingId = consultation.Incapacidad?.Id;
            var nextId = existingId ?? NextId(data, d => d.Patients
                .SelectMany(p => p.Consultations)
                .Where(c => c.Incapacidad is not null)
                .Select(c => c.Incapacidad!.Id));

            consultation.Incapacidad = new IncapacidadRecord
            {
                Id = nextId,
                IdConsulta = consultation.Id,
                FechaInicio = model.FechaInicio.Date,
                FechaFin = model.FechaFin.Date,
                Motivo = model.Motivo.Trim()
            };
            await WriteAsync(data);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteIncapacityAsync(int consultationId)
    {
        await _gate.WaitAsync();
        try
        {
            var (data, patient, consultation) = await FindConsultationAsync(consultationId);
            if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
            {
                return false;
            }

            if (consultation.Incapacidad is null)
            {
                return false;
            }

            consultation.Incapacidad = null;
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
        PatientRecord patient,
        ConsultationRecord consultation)
    {
        var previous = patient.Consultations
            .Where(item => item.Id != consultation.Id && item.Estado == EstadoConsulta.Cerrada)
            .OrderByDescending(item => item.FechaHoraFinalizacion ?? item.FechaHoraInicio)
            .FirstOrDefault();

        var allStudies = BuildPatientStudies(patient);
        var consultationStudies = allStudies
            .Where(item => item.IdConsultaLectura == consultation.Id)
            .ToArray();

        var completedOrderDetailIds = patient.Estudios
            .Where(e => e.IdDetalleOrden.HasValue)
            .Select(e => e.IdDetalleOrden!.Value)
            .ToHashSet();

        var pendingOrderDetails = patient.Consultations
            .Where(c => c.Id != consultation.Id)
            .SelectMany(c => c.Ordenes.SelectMany(o => o.Detalles.Select(d => new PendingOrderDetailItem(
                d.Id,
                o.Id,
                c.Id,
                o.FechaHoraEmision,
                d.Nombre,
                d.Indicaciones,
                completedOrderDetailIds.Contains(d.Id)))))
            .OrderByDescending(item => item.FechaEmision)
            .ToArray();

        return new ConsultationPageData(
            ToListItem(patient),
            patient.Alergias.ToArray(),
            patient.Enfermedades.OrderByDescending(e => e.AnioDiagnostico).ToArray(),
            consultation,
            consultationStudies,
            pendingOrderDetails,
            previous is null ? null : new PreviousConsultationData(
                previous.FechaHoraFinalizacion ?? previous.FechaHoraInicio,
                previous.Motivo,
                previous.Diagnostico,
                previous.Recetas.SelectMany(r => r.Detalles).ToArray()));
    }

    private static IReadOnlyList<StudyDisplayItem> BuildPatientStudies(PatientRecord patient)
    {
        var orderDetailsById = patient.Consultations
            .SelectMany(c => c.Ordenes)
            .SelectMany(o => o.Detalles)
            .ToDictionary(d => d.Id);

        return patient.Estudios
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .Select(e =>
            {
                DetalleOrdenRecord? detalleOrden = null;
                if (e.IdDetalleOrden.HasValue)
                {
                    orderDetailsById.TryGetValue(e.IdDetalleOrden.Value, out detalleOrden);
                }

                var displayName = !string.IsNullOrWhiteSpace(e.Nombre)
                    ? e.Nombre
                    : detalleOrden?.Nombre ?? "Estudio sin nombre";

                return new StudyDisplayItem(
                    e.Id,
                    e.IdPaciente,
                    e.IdDetalleOrden,
                    detalleOrden?.Nombre,
                    e.IdConsultaLectura,
                    e.Nombre,
                    displayName,
                    e.Observaciones,
                    e.Fecha);
            })
            .ToArray();
    }

    private static PatientListItem ToListItem(PatientRecord patient) =>
        new(patient.Id, patient.Nombre, patient.FechaNacimiento, patient.Dui);

    private static int NextId(ClinicData data, Func<ClinicData, IEnumerable<int>> ids) =>
        ids(data).DefaultIfEmpty(0).Max() + 1;

    private async Task<ClinicData> ReadAsync()
    {
        ClinicData data;
        if (!File.Exists(_filePath))
        {
            data = new ClinicData();
        }
        else
        {
            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            data = await JsonSerializer.DeserializeAsync<ClinicData>(stream, JsonOptions)
                ?? new ClinicData();
        }

        if (data.Medicamentos.Count == 0)
        {
            var id = 1;
            foreach (var med in DefaultMedications)
            {
                data.Medicamentos.Add(new MedicamentoRecord { Id = id++, Nombre = med });
            }
            await WriteAsync(data);
        }

        return data;
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
        public List<MedicamentoRecord> Medicamentos { get; set; } = [];
    }

    // Tabla: pacientes
    public sealed class PatientRecord
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public string? Dui { get; set; }
        public List<AlergiaRecord> Alergias { get; set; } = [];
        public List<EnfermedadRecord> Enfermedades { get; set; } = [];
        public List<ConsultationRecord> Consultations { get; set; } = [];
        public List<EstudioRecord> Estudios { get; set; } = [];
    }

    // Tabla: alergias_paciente
    public sealed class AlergiaRecord
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public string TipoAlergia { get; set; } = string.Empty;
        public string SustanciaElemento { get; set; } = string.Empty;
        public string Severidad { get; set; } = string.Empty;
    }

    // Tabla: enfermedades
    public sealed class EnfermedadRecord
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int AnioDiagnostico { get; set; }
    }

    // Tabla: consultas
    public sealed class ConsultationRecord
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public DateTime? FechaHoraFinalizacion { get; set; }
        public string? Motivo { get; set; }
        public string? ExamenFisico { get; set; }
        public string? Diagnostico { get; set; }
        public PrioridadAtencion Prioridad { get; set; } = PrioridadAtencion.Media;
        public EstadoConsulta Estado { get; set; } = EstadoConsulta.Esperando;
        public SignosVitalesRecord? SignosVitales { get; set; }
        public List<RecetaRecord> Recetas { get; set; } = [];
        public IncapacidadRecord? Incapacidad { get; set; }
        public List<OrdenRecord> Ordenes { get; set; } = [];
    }

    // Tabla: signos_vitales
    public sealed class SignosVitalesRecord
    {
        public int Id { get; set; }
        public int IdConsulta { get; set; }
        public double? Temperatura { get; set; }
        public double? Altura { get; set; }
        public double? Peso { get; set; }
        public int? PresionSistolica { get; set; }
        public int? PresionDiastolica { get; set; }
        public int? ConcentracionOxigeno { get; set; }
        public int? RitmoCardiaco { get; set; }
    }

    // Tabla: medicamentos
    public sealed class MedicamentoRecord
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    // Tabla: receta
    public sealed class RecetaRecord
    {
        public int Id { get; set; }
        public int IdConsulta { get; set; }
        public List<DetalleRecetaRecord> Detalles { get; set; } = [];
    }

    // Tabla: detalle_receta
    public sealed class DetalleRecetaRecord
    {
        public int Id { get; set; }
        public int IdReceta { get; set; }
        public int? IdMedicamento { get; set; }
        public string NombreMedicamento { get; set; } = string.Empty;
        public string Dosis { get; set; } = string.Empty;
        public string Frecuencia { get; set; } = string.Empty;
        public string Duracion { get; set; } = string.Empty;
        public string Indicaciones { get; set; } = string.Empty;
    }

    // Tabla: incapacidades
    public sealed class IncapacidadRecord
    {
        public int Id { get; set; }
        public int IdConsulta { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Motivo { get; set; } = string.Empty;

        public int DiasCalculados => Math.Max(1, (FechaFin.Date - FechaInicio.Date).Days + 1);
    }

    // Tabla: ordenes
    public sealed class OrdenRecord
    {
        public int Id { get; set; }
        public int ConsultaId { get; set; }
        public DateTime FechaHoraEmision { get; set; }
        public List<DetalleOrdenRecord> Detalles { get; set; } = [];
    }

    // Tabla: detalle_orden
    public sealed class DetalleOrdenRecord
    {
        public int Id { get; set; }
        public int IdOrden { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Indicaciones { get; set; } = string.Empty;
    }

    // Tabla: estudios
    public sealed class EstudioRecord
    {
        public int Id { get; set; }
        public int IdPaciente { get; set; }
        public int? IdDetalleOrden { get; set; }
        public int? IdConsultaLectura { get; set; }
        public string? Nombre { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }
}

public sealed record PatientListItem(
    int Id,
    string Nombre,
    DateTime FechaNacimiento,
    string? Dui)
{
    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - FechaNacimiento.Year;
            return FechaNacimiento.Date > today.AddYears(-age) ? age - 1 : age;
        }
    }
}

public sealed record StudyDisplayItem(
    int Id,
    int IdPaciente,
    int? IdDetalleOrden,
    string? NombreOrdenAsociada,
    int? IdConsultaLectura,
    string? NombrePropio,
    string NombreMostrar,
    string Observaciones,
    DateTime Fecha);

public sealed record PendingOrderDetailItem(
    int IdDetalleOrden,
    int IdOrden,
    int IdConsultaOrigen,
    DateTime FechaEmision,
    string Nombre,
    string Indicaciones,
    bool TieneEstudioRegistrado);

public sealed record PatientChartData(
    PatientListItem Patient,
    IReadOnlyList<ClinicDataStore.AlergiaRecord> Alergias,
    IReadOnlyList<ClinicDataStore.EnfermedadRecord> Enfermedades,
    IReadOnlyList<ClinicDataStore.ConsultationRecord> Consultations,
    IReadOnlyList<StudyDisplayItem> Estudios);

public sealed record WaitingRoomItem(
    int ConsultationId,
    int PatientId,
    string PatientName,
    DateTime DateOfBirth,
    string? Dui,
    DateTime StartTime,
    PrioridadAtencion Priority,
    EstadoConsulta State,
    double? Peso,
    double? Altura,
    double? Temperatura)
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
    IReadOnlyList<ClinicDataStore.AlergiaRecord> Alergias,
    IReadOnlyList<ClinicDataStore.EnfermedadRecord> Enfermedades,
    ClinicDataStore.ConsultationRecord Consultation,
    IReadOnlyList<StudyDisplayItem> EstudiosLeidos,
    IReadOnlyList<PendingOrderDetailItem> DetallesOrdenPaciente,
    PreviousConsultationData? Previous);

public sealed record PreviousConsultationData(
    DateTime? ClosedAt,
    string? Motivo,
    string? Diagnostico,
    IReadOnlyList<ClinicDataStore.DetalleRecetaRecord> DetallesReceta);
