using System.Text.Json;
using ClinicaApp.Data;
using ClinicaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Services;

public sealed class LegacyClinicDataImporter(
    ClinicaDbContext db,
    IWebHostEnvironment environment)
{
    private const string ImportName = "clinic-data-json-to-ef-core-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task ImportIfPendingAsync(CancellationToken cancellationToken = default)
    {
        if (await db.AppliedDataMigrations.AnyAsync(
                item => item.Name == ImportName,
                cancellationToken))
        {
            return;
        }

        var path = Path.Combine(environment.ContentRootPath, "App_Data", "clinic-data.json");
        LegacyClinicData legacyData = new();
        if (File.Exists(path))
        {
            await using var stream = File.OpenRead(path);
            legacyData = await JsonSerializer.DeserializeAsync<LegacyClinicData>(
                stream,
                JsonOptions,
                cancellationToken) ?? new LegacyClinicData();
        }

        if (legacyData.Patients.Count > 0
            && await db.Pacientes.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "La base de datos ya contiene pacientes y clinic-data.json también tiene datos. "
                + "No se importó para evitar duplicados; resuelve la migración de datos antes de iniciar la aplicación.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var medicationsByLegacyId = await ImportMedicationsAsync(
            legacyData.Medicamentos,
            cancellationToken);

        var consultationsByLegacyId = new Dictionary<int, Consulta>();
        var orderDetailsByLegacyId = new Dictionary<int, DetalleOrden>();
        var patientStudies = new List<(Paciente Patient, ClinicDataStore.EstudioRecord Study)>();
        var patients = new List<Paciente>();

        foreach (var legacyPatient in legacyData.Patients)
        {
            var patient = new Paciente
            {
                Nombre = legacyPatient.Nombre,
                FechaNacimiento = legacyPatient.FechaNacimiento,
                Dui = legacyPatient.Dui
            };

            foreach (var legacyAllergy in legacyPatient.Alergias)
            {
                patient.Alergias.Add(new AlergiaPaciente
                {
                    TipoAlergia = legacyAllergy.TipoAlergia,
                    SustanciaElemento = legacyAllergy.SustanciaElemento,
                    Severidad = legacyAllergy.Severidad
                });
            }

            foreach (var legacyCondition in legacyPatient.Enfermedades)
            {
                patient.Enfermedades.Add(new Enfermedad
                {
                    Nombre = legacyCondition.Nombre,
                    AnioDiagnostico = legacyCondition.AnioDiagnostico
                });
            }

            foreach (var legacyConsultation in legacyPatient.Consultations)
            {
                var consultation = new Consulta
                {
                    FechaHoraInicio = legacyConsultation.FechaHoraInicio,
                    FechaHoraFinalizacion = legacyConsultation.FechaHoraFinalizacion,
                    Motivo = legacyConsultation.Motivo,
                    ExamenFisico = legacyConsultation.ExamenFisico,
                    Diagnostico = legacyConsultation.Diagnostico,
                    Prioridad = legacyConsultation.Prioridad,
                    Estado = legacyConsultation.Estado
                };
                consultationsByLegacyId.Add(legacyConsultation.Id, consultation);

                if (legacyConsultation.SignosVitales is not null)
                {
                    consultation.SignosVitales = new SignosVitales
                    {
                        Temperatura = legacyConsultation.SignosVitales.Temperatura,
                        Altura = legacyConsultation.SignosVitales.Altura,
                        Peso = legacyConsultation.SignosVitales.Peso,
                        PresionSistolica = legacyConsultation.SignosVitales.PresionSistolica,
                        PresionDiastolica = legacyConsultation.SignosVitales.PresionDiastolica,
                        ConcentracionOxigeno = legacyConsultation.SignosVitales.ConcentracionOxigeno,
                        RitmoCardiaco = legacyConsultation.SignosVitales.RitmoCardiaco
                    };
                }

                if (legacyConsultation.Incapacidad is not null)
                {
                    consultation.Incapacidad = new Incapacidad
                    {
                        FechaInicio = legacyConsultation.Incapacidad.FechaInicio,
                        FechaFin = legacyConsultation.Incapacidad.FechaFin,
                        Motivo = legacyConsultation.Incapacidad.Motivo
                    };
                }

                foreach (var legacyRecipe in legacyConsultation.Recetas)
                {
                    var recipe = new Receta();
                    foreach (var legacyDetail in legacyRecipe.Detalles)
                    {
                        recipe.Detalles.Add(new DetalleReceta
                        {
                            IdMedicamento = legacyDetail.IdMedicamento.HasValue
                                && medicationsByLegacyId.TryGetValue(
                                    legacyDetail.IdMedicamento.Value,
                                    out var medicationId)
                                    ? medicationId
                                    : null,
                            Dosis = legacyDetail.Dosis,
                            Frecuencia = legacyDetail.Frecuencia,
                            Duracion = legacyDetail.Duracion,
                            Indicaciones = legacyDetail.Indicaciones
                        });
                    }

                    consultation.Recetas.Add(recipe);
                }

                foreach (var legacyOrder in legacyConsultation.Ordenes)
                {
                    var order = new Orden { FechaHoraEmision = legacyOrder.FechaHoraEmision };
                    foreach (var legacyDetail in legacyOrder.Detalles)
                    {
                        var detail = new DetalleOrden
                        {
                            Nombre = legacyDetail.Nombre,
                            Indicaciones = legacyDetail.Indicaciones
                        };
                        order.Detalles.Add(detail);
                        orderDetailsByLegacyId.Add(legacyDetail.Id, detail);
                    }

                    consultation.Ordenes.Add(order);
                }

                patient.Consultas.Add(consultation);
            }

            foreach (var study in legacyPatient.Estudios)
            {
                patientStudies.Add((patient, study));
            }

            patients.Add(patient);
        }

        db.Pacientes.AddRange(patients);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var (patient, legacyStudy) in patientStudies)
        {
            if (!legacyStudy.IdConsultaLectura.HasValue
                || !consultationsByLegacyId.TryGetValue(legacyStudy.IdConsultaLectura.Value, out var readingConsultation))
            {
                throw new InvalidDataException(
                    $"El estudio {legacyStudy.Id} no tiene una consulta de lectura válida.");
            }

            DetalleOrden? orderDetail = null;
            if (legacyStudy.IdDetalleOrden.HasValue
                && !orderDetailsByLegacyId.TryGetValue(legacyStudy.IdDetalleOrden.Value, out orderDetail))
            {
                throw new InvalidDataException(
                    $"El estudio {legacyStudy.Id} referencia un detalle de orden inexistente.");
            }

            patient.Estudios.Add(new Estudio
            {
                IdPaciente = patient.Id,
                IdConsultaLectura = readingConsultation.Id,
                IdDetalleOrden = orderDetail?.Id,
                Nombre = legacyStudy.Nombre,
                Observaciones = legacyStudy.Observaciones,
                Fecha = legacyStudy.Fecha
            });
        }

        db.AppliedDataMigrations.Add(new AppliedDataMigration
        {
            Name = ImportName,
            AppliedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Dictionary<int, int>> ImportMedicationsAsync(
        IReadOnlyCollection<ClinicDataStore.MedicamentoRecord> legacyMedications,
        CancellationToken cancellationToken)
    {
        var existingMedications = await db.Medicamentos.ToListAsync(cancellationToken);
        var medicationsByName = existingMedications.ToDictionary(
            medication => medication.Nombre,
            medication => medication,
            StringComparer.OrdinalIgnoreCase);
        var importedIds = new Dictionary<int, int>();

        foreach (var legacyMedication in legacyMedications)
        {
            var name = legacyMedication.Nombre.Trim();
            if (!medicationsByName.TryGetValue(name, out var medication))
            {
                medication = new Medicamento { Nombre = name };
                db.Medicamentos.Add(medication);
                medicationsByName.Add(name, medication);
            }

            importedIds.Add(legacyMedication.Id, medication.Id);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var legacyMedication in legacyMedications)
        {
            importedIds[legacyMedication.Id] = medicationsByName[legacyMedication.Nombre.Trim()].Id;
        }

        return importedIds;
    }

    private sealed class LegacyClinicData
    {
        public List<ClinicDataStore.PatientRecord> Patients { get; set; } = [];
        public List<ClinicDataStore.MedicamentoRecord> Medicamentos { get; set; } = [];
    }
}
