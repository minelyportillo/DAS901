using ClinicaApp.Data;
using ClinicaApp.Models;
using ClinicaApp.Models.Enums;
using ClinicaApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Services;

public sealed class ClinicDataStore : IClinicDataStore
{
    private readonly ClinicaDbContext _db;

    public ClinicDataStore(ClinicaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PatientListItem>> SearchPatientsAsync(string? term)
    {
        var searchTerm = term?.Trim();
        var patients = _db.Pacientes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            patients = patients.Where(patient =>
                patient.Nombre.Contains(searchTerm)
                || (patient.Dui != null && patient.Dui.Contains(searchTerm)));
        }

        return await patients
            .OrderBy(patient => patient.Nombre)
            .Select(patient => new PatientListItem(
                patient.Id,
                patient.Nombre,
                patient.FechaNacimiento,
                patient.Dui))
            .ToListAsync();
    }

    public async Task<PatientChartData?> GetPatientChartAsync(int patientId)
    {
        var patient = await LoadPatientGraph()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == patientId);
        if (patient is null)
        {
            return null;
        }

        var record = ToPatientRecord(patient);
        var consultations = record.Consultations
            .OrderByDescending(consultation => consultation.FechaHoraInicio)
            .ToArray();

        return new PatientChartData(
            ToListItem(record),
            record.Alergias.ToArray(),
            record.Enfermedades.OrderByDescending(item => item.AnioDiagnostico).ToArray(),
            consultations,
            BuildPatientStudies(record));
    }

    public async Task<IReadOnlyList<WaitingRoomItem>> GetWaitingRoomAsync(DateTime date)
    {
        var from = date.Date;
        var until = from.AddDays(1);
        return await _db.Consultas
            .AsNoTracking()
            .Where(consultation => consultation.FechaHoraInicio >= from
                && consultation.FechaHoraInicio < until
                && consultation.Estado != EstadoConsulta.Cerrada)
            .OrderByDescending(consultation => consultation.Prioridad)
            .ThenBy(consultation => consultation.FechaHoraInicio)
            .Select(consultation => new WaitingRoomItem(
                consultation.Id,
                consultation.IdPaciente,
                consultation.Paciente.Nombre,
                consultation.Paciente.FechaNacimiento,
                consultation.Paciente.Dui,
                consultation.FechaHoraInicio,
                consultation.Prioridad,
                consultation.Estado,
                consultation.SignosVitales == null ? null : consultation.SignosVitales.Peso,
                consultation.SignosVitales == null ? null : consultation.SignosVitales.Altura,
                consultation.SignosVitales == null ? null : consultation.SignosVitales.Temperatura))
            .ToListAsync();
    }

    public async Task<int> RegisterArrivalAsync(int patientId)
    {
        var patientExists = await _db.Pacientes.AnyAsync(patient => patient.Id == patientId);
        if (!patientExists)
        {
            throw new KeyNotFoundException("No se encontró el paciente.");
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var hasActiveConsultation = await _db.Consultas.AnyAsync(consultation =>
            consultation.IdPaciente == patientId
            && consultation.FechaHoraInicio >= today
            && consultation.FechaHoraInicio < tomorrow
            && consultation.Estado != EstadoConsulta.Cerrada);
        if (hasActiveConsultation)
        {
            throw new InvalidOperationException("Este paciente ya tiene una consulta activa hoy.");
        }

        var consultation = new Consulta
        {
            IdPaciente = patientId,
            FechaHoraInicio = DateTime.Now,
            Prioridad = PrioridadAtencion.Media,
            Estado = EstadoConsulta.Esperando
        };
        _db.Consultas.Add(consultation);
        await _db.SaveChangesAsync();
        return consultation.Id;
    }

    public async Task<bool> SetPriorityAsync(int consultationId, PrioridadAtencion priority)
    {
        var consultation = await _db.Consultas
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.Esperando)
        {
            return false;
        }

        consultation.Prioridad = priority;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveTriageAsync(TriageViewModel model)
    {
        var consultation = await _db.Consultas
            .Include(item => item.SignosVitales)
            .FirstOrDefaultAsync(item => item.Id == model.ConsultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.Esperando)
        {
            return false;
        }

        consultation.SignosVitales ??= new SignosVitales();
        consultation.SignosVitales.Peso = model.Peso;
        consultation.SignosVitales.Altura = model.Altura;
        consultation.SignosVitales.Temperatura = model.Temperatura;
        consultation.Prioridad = model.Prioridad;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ConsultationPageData?> StartConsultationAsync(int consultationId)
    {
        var patient = await LoadPatientGraph()
            .FirstOrDefaultAsync(item => item.Consultas.Any(consultation => consultation.Id == consultationId));
        var entity = patient?.Consultas.FirstOrDefault(item => item.Id == consultationId);
        if (patient is null || entity is null || entity.Estado == EstadoConsulta.Cerrada)
        {
            return null;
        }

        if (entity.Estado == EstadoConsulta.Esperando)
        {
            entity.Estado = EstadoConsulta.EnAtencion;
            await _db.SaveChangesAsync();
        }

        return BuildConsultationPage(ToPatientRecord(patient), ToConsultationRecord(entity));
    }

    public async Task<ConsultationPageData?> GetConsultationAsync(int consultationId)
    {
        var patient = await LoadPatientGraph()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Consultas.Any(consultation => consultation.Id == consultationId));
        var consultation = patient?.Consultas.FirstOrDefault(item => item.Id == consultationId);
        return patient is null || consultation is null
            ? null
            : BuildConsultationPage(ToPatientRecord(patient), ToConsultationRecord(consultation));
    }

    public async Task<bool> SaveClinicalNotesAsync(ClinicalConsultationViewModel model)
    {
        var consultation = await _db.Consultas
            .Include(item => item.SignosVitales)
            .FirstOrDefaultAsync(item => item.Id == model.ConsultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        consultation.Motivo = model.Motivo?.Trim();
        consultation.ExamenFisico = model.ExamenFisico?.Trim();
        consultation.Diagnostico = model.Diagnostico?.Trim();

        consultation.SignosVitales ??= new SignosVitales();
        consultation.SignosVitales.Peso = model.Peso;
        consultation.SignosVitales.Altura = model.Altura;
        consultation.SignosVitales.Temperatura = model.Temperatura;
        consultation.SignosVitales.PresionSistolica = model.PresionSistolica;
        consultation.SignosVitales.PresionDiastolica = model.PresionDiastolica;
        consultation.SignosVitales.RitmoCardiaco = model.RitmoCardiaco;
        consultation.SignosVitales.ConcentracionOxigeno = model.ConcentracionOxigeno;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CloseConsultationAsync(int consultationId)
    {
        var consultation = await _db.Consultas
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
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
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SavePrescriptionAsync(int consultationId, PrescriptionViewModel model)
    {
        if (!model.IdMedicamento.HasValue)
        {
            return false;
        }

        var consultation = await _db.Consultas
            .Include(item => item.Recetas)
            .ThenInclude(recipe => recipe.Detalles)
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        var medication = await _db.Medicamentos
            .FirstOrDefaultAsync(item => item.Id == model.IdMedicamento.Value);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion || medication is null)
        {
            return false;
        }

        if (model.DetalleId.HasValue)
        {
            var detail = consultation.Recetas
                .SelectMany(recipe => recipe.Detalles)
                .FirstOrDefault(item => item.Id == model.DetalleId.Value);
            if (detail is null)
            {
                return false;
            }

            detail.IdMedicamento = medication.Id;
            detail.Dosis = model.Dosis.Trim();
            detail.Frecuencia = model.Frecuencia.Trim();
            detail.Duracion = model.Duracion.Trim();
            detail.Indicaciones = model.Indicaciones?.Trim() ?? string.Empty;
        }
        else
        {
            var recipe = consultation.Recetas.FirstOrDefault();
            if (recipe is null)
            {
                recipe = new Receta { IdConsulta = consultation.Id };
                consultation.Recetas.Add(recipe);
            }

            recipe.Detalles.Add(new DetalleReceta
            {
                IdMedicamento = medication.Id,
                Dosis = model.Dosis.Trim(),
                Frecuencia = model.Frecuencia.Trim(),
                Duracion = model.Duracion.Trim(),
                Indicaciones = model.Indicaciones?.Trim() ?? string.Empty
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePrescriptionDetailAsync(int consultationId, int detalleId)
    {
        var consultation = await _db.Consultas
            .Include(item => item.Recetas)
            .ThenInclude(recipe => recipe.Detalles)
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        var recipe = consultation.Recetas.FirstOrDefault(item =>
            item.Detalles.Any(detail => detail.Id == detalleId));
        var detail = recipe?.Detalles.FirstOrDefault(item => item.Id == detalleId);
        if (recipe is null || detail is null)
        {
            return false;
        }

        _db.DetallesReceta.Remove(detail);
        if (recipe.Detalles.Count == 1)
        {
            _db.Recetas.Remove(recipe);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveExamOrderAsync(int consultationId, ExamOrderViewModel model)
    {
        var consultation = await _db.Consultas
            .Include(item => item.Ordenes)
            .ThenInclude(order => order.Detalles)
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        if (model.DetalleOrdenId.HasValue)
        {
            var detail = consultation.Ordenes
                .SelectMany(order => order.Detalles)
                .FirstOrDefault(item => item.Id == model.DetalleOrdenId.Value);
            if (detail is null)
            {
                return false;
            }

            detail.Nombre = model.Nombre.Trim();
            detail.Indicaciones = model.Indicaciones?.Trim() ?? string.Empty;
        }
        else
        {
            var order = consultation.Ordenes.FirstOrDefault();
            if (order is null)
            {
                order = new Orden
                {
                    ConsultaId = consultation.Id,
                    FechaHoraEmision = DateTime.Now
                };
                consultation.Ordenes.Add(order);
            }

            order.Detalles.Add(new DetalleOrden
            {
                Nombre = model.Nombre.Trim(),
                Indicaciones = model.Indicaciones?.Trim() ?? string.Empty
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteExamOrderDetailAsync(int consultationId, int detalleOrdenId)
    {
        var consultation = await _db.Consultas
            .Include(item => item.Ordenes)
            .ThenInclude(order => order.Detalles)
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        var order = consultation.Ordenes.FirstOrDefault(item =>
            item.Detalles.Any(detail => detail.Id == detalleOrdenId));
        var detail = order?.Detalles.FirstOrDefault(item => item.Id == detalleOrdenId);
        if (order is null || detail is null)
        {
            return false;
        }

        _db.DetallesOrden.Remove(detail);
        if (order.Detalles.Count == 1)
        {
            _db.Ordenes.Remove(order);
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SaveStudyAsync(StudyViewModel model)
    {
        var patient = await LoadPatientGraph()
            .FirstOrDefaultAsync(item => item.Consultas.Any(consultation => consultation.Id == model.ConsultationId));
        var consultation = patient?.Consultas.FirstOrDefault(item => item.Id == model.ConsultationId);
        if (patient is null || consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        DetalleOrden? orderDetail = null;
        if (model.IdDetalleOrden.HasValue)
        {
            orderDetail = patient.Consultas
                .Where(item => item.Id != consultation.Id)
                .SelectMany(item => item.Ordenes)
                .SelectMany(item => item.Detalles)
                .FirstOrDefault(item => item.Id == model.IdDetalleOrden.Value);
            if (orderDetail is null)
            {
                return false;
            }
        }

        var study = model.EstudioId.HasValue
            ? patient.Estudios.FirstOrDefault(item =>
                item.Id == model.EstudioId.Value && item.IdConsultaLectura == consultation.Id)
            : null;
        if (model.EstudioId.HasValue && study is null)
        {
            return false;
        }

        if (study is null)
        {
            study = new Estudio
            {
                IdPaciente = patient.Id,
                IdConsultaLectura = consultation.Id
            };
            patient.Estudios.Add(study);
        }

        study.IdDetalleOrden = orderDetail?.Id;
        study.Nombre = string.IsNullOrWhiteSpace(model.Nombre) ? null : model.Nombre.Trim();
        study.Observaciones = model.Observaciones.Trim();
        study.Fecha = model.Fecha.Date;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteStudyAsync(int consultationId, int estudioId)
    {
        var study = await _db.Estudios
            .Include(item => item.ConsultaLectura)
            .FirstOrDefaultAsync(item => item.Id == estudioId && item.IdConsultaLectura == consultationId);
        if (study is null || study.ConsultaLectura.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        _db.Estudios.Remove(study);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddIncapacityAsync(int consultationId, IncapacityViewModel model)
    {
        var consultation = await _db.Consultas
            .Include(item => item.Incapacidad)
            .FirstOrDefaultAsync(item => item.Id == consultationId);
        if (consultation is null || consultation.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        consultation.Incapacidad ??= new Incapacidad { IdConsulta = consultation.Id };
        consultation.Incapacidad.FechaInicio = model.FechaInicio.Date;
        consultation.Incapacidad.FechaFin = model.FechaFin.Date;
        consultation.Incapacidad.Motivo = model.Motivo.Trim();
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteIncapacityAsync(int consultationId)
    {
        var incapacity = await _db.Incapacidades
            .Include(item => item.Consulta)
            .FirstOrDefaultAsync(item => item.IdConsulta == consultationId);
        if (incapacity is null || incapacity.Consulta.Estado != EstadoConsulta.EnAtencion)
        {
            return false;
        }

        _db.Incapacidades.Remove(incapacity);
        await _db.SaveChangesAsync();
        return true;
    }

    private IQueryable<Paciente> LoadPatientGraph() =>
        _db.Pacientes
            .AsSplitQuery()
            .Include(patient => patient.Alergias)
            .Include(patient => patient.Enfermedades)
            .Include(patient => patient.Consultas)
                .ThenInclude(consultation => consultation.SignosVitales)
            .Include(patient => patient.Consultas)
                .ThenInclude(consultation => consultation.Incapacidad)
            .Include(patient => patient.Consultas)
                .ThenInclude(consultation => consultation.Recetas)
                    .ThenInclude(recipe => recipe.Detalles)
                        .ThenInclude(detail => detail.Medicamento)
            .Include(patient => patient.Consultas)
                .ThenInclude(consultation => consultation.Ordenes)
                    .ThenInclude(order => order.Detalles)
            .Include(patient => patient.Estudios);

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

    private static PatientRecord ToPatientRecord(Paciente patient) =>
        new()
        {
            Id = patient.Id,
            Nombre = patient.Nombre,
            FechaNacimiento = patient.FechaNacimiento,
            Dui = patient.Dui,
            Alergias = patient.Alergias.Select(item => new AlergiaRecord
            {
                Id = item.Id,
                IdPaciente = item.IdPaciente,
                TipoAlergia = item.TipoAlergia,
                SustanciaElemento = item.SustanciaElemento,
                Severidad = item.Severidad
            }).ToList(),
            Enfermedades = patient.Enfermedades.Select(item => new EnfermedadRecord
            {
                Id = item.Id,
                IdPaciente = item.IdPaciente,
                Nombre = item.Nombre,
                AnioDiagnostico = item.AnioDiagnostico
            }).ToList(),
            Consultations = patient.Consultas.Select(ToConsultationRecord).ToList(),
            Estudios = patient.Estudios.Select(item => new EstudioRecord
            {
                Id = item.Id,
                IdPaciente = item.IdPaciente,
                IdDetalleOrden = item.IdDetalleOrden,
                IdConsultaLectura = item.IdConsultaLectura,
                Nombre = item.Nombre,
                Observaciones = item.Observaciones,
                Fecha = item.Fecha
            }).ToList()
        };

    private static ConsultationRecord ToConsultationRecord(Consulta consultation) =>
        new()
        {
            Id = consultation.Id,
            IdPaciente = consultation.IdPaciente,
            FechaHoraInicio = consultation.FechaHoraInicio,
            FechaHoraFinalizacion = consultation.FechaHoraFinalizacion,
            Motivo = consultation.Motivo,
            ExamenFisico = consultation.ExamenFisico,
            Diagnostico = consultation.Diagnostico,
            Prioridad = consultation.Prioridad,
            Estado = consultation.Estado,
            SignosVitales = consultation.SignosVitales is null ? null : new SignosVitalesRecord
            {
                Id = consultation.SignosVitales.Id,
                IdConsulta = consultation.SignosVitales.IdConsulta,
                Peso = consultation.SignosVitales.Peso,
                Altura = consultation.SignosVitales.Altura,
                Temperatura = consultation.SignosVitales.Temperatura,
                PresionSistolica = consultation.SignosVitales.PresionSistolica,
                PresionDiastolica = consultation.SignosVitales.PresionDiastolica,
                RitmoCardiaco = consultation.SignosVitales.RitmoCardiaco,
                ConcentracionOxigeno = consultation.SignosVitales.ConcentracionOxigeno
            },
            Recetas = consultation.Recetas.Select(recipe => new RecetaRecord
            {
                Id = recipe.Id,
                IdConsulta = recipe.IdConsulta,
                Detalles = recipe.Detalles.Select(detail => new DetalleRecetaRecord
                {
                    Id = detail.Id,
                    IdReceta = detail.IdReceta,
                    IdMedicamento = detail.IdMedicamento,
                    NombreMedicamento = detail.Medicamento?.Nombre ?? "Medicamento no disponible",
                    Dosis = detail.Dosis,
                    Frecuencia = detail.Frecuencia,
                    Duracion = detail.Duracion,
                    Indicaciones = detail.Indicaciones
                }).ToList()
            }).ToList(),
            Incapacidad = consultation.Incapacidad is null ? null : new IncapacidadRecord
            {
                Id = consultation.Incapacidad.Id,
                IdConsulta = consultation.Incapacidad.IdConsulta,
                FechaInicio = consultation.Incapacidad.FechaInicio,
                FechaFin = consultation.Incapacidad.FechaFin,
                Motivo = consultation.Incapacidad.Motivo
            },
            Ordenes = consultation.Ordenes.Select(order => new OrdenRecord
            {
                Id = order.Id,
                ConsultaId = order.ConsultaId,
                FechaHoraEmision = order.FechaHoraEmision,
                Detalles = order.Detalles.Select(detail => new DetalleOrdenRecord
                {
                    Id = detail.Id,
                    IdOrden = detail.IdOrden,
                    Nombre = detail.Nombre,
                    Indicaciones = detail.Indicaciones
                }).ToList()
            }).ToList()
        };

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
