using ClinicaApp.Data;
using ClinicaApp.Models;
using ClinicaApp.Repositories;
using ClinicaApp.ViewModels;

namespace ClinicaApp.Services
{
    public class PacienteService : IPacienteService
    {
        private readonly IPacienteRepository _pacienteRepository;
        private readonly IAlergiaPacienteRepository _alergiaRepository;
        private readonly IEnfermedadRepository _enfermedadRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PacienteService(
            IPacienteRepository pacienteRepository,
            IAlergiaPacienteRepository alergiaRepository,
            IEnfermedadRepository enfermedadRepository,
            IUnitOfWork unitOfWork)
        {
            _pacienteRepository = pacienteRepository;
            _alergiaRepository = alergiaRepository;
            _enfermedadRepository = enfermedadRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<Paciente>> BuscarAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (await _pacienteRepository.GetAllAsync())
                    .OrderBy(paciente => paciente.Nombre)
                    .ToArray();
            }

            return (await _pacienteRepository.SearchAsync(query.Trim())).ToArray();
        }

        public async Task<IEnumerable<Paciente>> ObtenerTodosAsync()
        {
            return await _pacienteRepository.GetAllAsync();
        }

        public async Task<Paciente?> ObtenerPorIdAsync(int id)
        {
            return await _pacienteRepository.GetByIdAsync(id);
        }

        public async Task<Paciente?> ObtenerPorDuiAsync(string dui)
        {
            return await _pacienteRepository.GetByDuiAsync(dui);
        }

        public async Task<Paciente?> RegistrarPacienteAsync(RegistrarPacienteViewModel model)
        {
            var dui = model.Dui?.Trim();
            if (!string.IsNullOrWhiteSpace(dui))
            {
                var existente = await _pacienteRepository.GetByDuiAsync(dui);
                if (existente != null)
                {
                    return null;
                }
            }

            var paciente = new Paciente
            {
                Nombre = model.Nombre.Trim(),
                Dui = dui,
                FechaNacimiento = model.FechaNacimiento
            };

            await _pacienteRepository.AddAsync(paciente);
            await _unitOfWork.SaveChangesAsync();
            return paciente;
        }

        public async Task<bool> ActualizarPacienteAsync(int id, RegistrarPacienteViewModel model)
        {
            var paciente = await _pacienteRepository.GetByIdAsync(id);
            if (paciente is null)
            {
                return false;
            }

            var dui = model.Dui?.Trim();
            if (!string.IsNullOrWhiteSpace(dui))
            {
                var existing = await _pacienteRepository.GetByDuiAsync(dui);
                if (existing is not null && existing.Id != id)
                {
                    return false;
                }
            }

            paciente.Nombre = model.Nombre.Trim();
            paciente.Dui = string.IsNullOrWhiteSpace(dui) ? null : dui;
            paciente.FechaNacimiento = model.FechaNacimiento.Date;
            _pacienteRepository.Update(paciente);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarPacienteSinHistorialAsync(int id)
        {
            var paciente = await _pacienteRepository.GetByIdAsync(id);
            if (paciente is null)
            {
                return false;
            }

            if (await _pacienteRepository.HasClinicalHistoryAsync(id))
            {
                return false;
            }

            _pacienteRepository.Delete(paciente);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public Task<IReadOnlyList<AlergiaPaciente>> ObtenerAlergiasAsync(int patientId) =>
            _alergiaRepository.GetByPatientAsync(patientId);

        public async Task<bool> AgregarAlergiaAsync(AlergiaPacienteViewModel model)
        {
            if (await _pacienteRepository.GetByIdAsync(model.PatientId) is null)
            {
                return false;
            }

            await _alergiaRepository.AddAsync(new AlergiaPaciente
            {
                IdPaciente = model.PatientId,
                TipoAlergia = model.TipoAlergia.Trim(),
                SustanciaElemento = model.SustanciaElemento.Trim(),
                Severidad = model.Severidad.Trim()
            });
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarAlergiaAsync(AlergiaPacienteViewModel model)
        {
            if (!model.Id.HasValue)
            {
                return false;
            }

            var allergy = await _alergiaRepository.GetByIdAndPatientAsync(model.Id.Value, model.PatientId);
            if (allergy is null)
            {
                return false;
            }

            allergy.TipoAlergia = model.TipoAlergia.Trim();
            allergy.SustanciaElemento = model.SustanciaElemento.Trim();
            allergy.Severidad = model.Severidad.Trim();
            _alergiaRepository.Update(allergy);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAlergiaAsync(int id, int patientId)
        {
            var allergy = await _alergiaRepository.GetByIdAndPatientAsync(id, patientId);
            if (allergy is null)
            {
                return false;
            }

            _alergiaRepository.Delete(allergy);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public Task<IReadOnlyList<Enfermedad>> ObtenerEnfermedadesAsync(int patientId) =>
            _enfermedadRepository.GetByPatientAsync(patientId);

        public async Task<bool> AgregarEnfermedadAsync(EnfermedadViewModel model)
        {
            if (await _pacienteRepository.GetByIdAsync(model.PatientId) is null)
            {
                return false;
            }

            await _enfermedadRepository.AddAsync(new Enfermedad
            {
                IdPaciente = model.PatientId,
                Nombre = model.Nombre.Trim(),
                AnioDiagnostico = model.AnioDiagnostico
            });
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActualizarEnfermedadAsync(EnfermedadViewModel model)
        {
            if (!model.Id.HasValue)
            {
                return false;
            }

            var condition = await _enfermedadRepository.GetByIdAndPatientAsync(model.Id.Value, model.PatientId);
            if (condition is null)
            {
                return false;
            }

            condition.Nombre = model.Nombre.Trim();
            condition.AnioDiagnostico = model.AnioDiagnostico;
            _enfermedadRepository.Update(condition);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarEnfermedadAsync(int id, int patientId)
        {
            var condition = await _enfermedadRepository.GetByIdAndPatientAsync(id, patientId);
            if (condition is null)
            {
                return false;
            }

            _enfermedadRepository.Delete(condition);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
