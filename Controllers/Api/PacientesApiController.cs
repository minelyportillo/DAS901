using ClinicaApp.DTOs;
using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers.Api;

/// <summary>
/// Endpoints RESTful para la gestión de pacientes, expedientes clínicos, alergias y enfermedades.
/// </summary>
[ApiController]
[Route("api/pacientes")]
[Produces("application/json")]
[Authorize(Roles = "Secretaria,Doctora")]
public sealed class PacientesApiController(
    IPacienteService pacienteService,
    IClinicDataStore clinicData) : ControllerBase
{
    /// <summary>
    /// Lista todos los pacientes o filtra por nombre o DUI.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PacienteResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PacienteResponseDto>>> GetAll([FromQuery] string? termino)
    {
        var pacientes = await pacienteService.BuscarAsync(termino);
        var response = pacientes
            .Select(p => new PacienteResponseDto(
                p.Id,
                p.Nombre,
                p.FechaNacimiento,
                p.Edad,
                p.Dui))
            .ToArray();

        return Ok(response);
    }

    /// <summary>
    /// Obtiene el expediente clínico completo de un paciente por su ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PatientChartData), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientChartData>> GetById(int id)
    {
        var expediente = await clinicData.GetPatientChartAsync(id);
        if (expediente is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Paciente no encontrado",
                Detail = $"No existe un paciente registrado con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(expediente);
    }

    /// <summary>
    /// Registra un nuevo paciente en la base de datos.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(PacienteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PacienteResponseDto>> Create([FromBody] PacienteRequestDto request)
    {
        if (!TryValidateBirthDate(request.FechaNacimiento, out var errorFecha))
        {
            ModelState.AddModelError(nameof(request.FechaNacimiento), errorFecha!);
            return ValidationProblem(ModelState);
        }

        var creado = await pacienteService.RegistrarPacienteAsync(new RegistrarPacienteViewModel
        {
            Nombre = request.Nombre,
            FechaNacimiento = request.FechaNacimiento,
            Dui = request.Dui
        });

        if (creado is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "DUI duplicado",
                Detail = "Ya existe un paciente registrado con el DUI proporcionado.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var response = new PacienteResponseDto(
            creado.Id,
            creado.Nombre,
            creado.FechaNacimiento,
            creado.Edad,
            creado.Dui);

        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, response);
    }

    /// <summary>
    /// Actualiza los datos personales de un paciente existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(PacienteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PacienteResponseDto>> Update(int id, [FromBody] PacienteRequestDto request)
    {
        if (!TryValidateBirthDate(request.FechaNacimiento, out var errorFecha))
        {
            ModelState.AddModelError(nameof(request.FechaNacimiento), errorFecha!);
            return ValidationProblem(ModelState);
        }

        var actualizado = await pacienteService.ActualizarPacienteAsync(id, new RegistrarPacienteViewModel
        {
            Nombre = request.Nombre,
            FechaNacimiento = request.FechaNacimiento,
            Dui = request.Dui
        });

        if (!actualizado)
        {
            var existente = await pacienteService.ObtenerPorIdAsync(id);
            if (existente is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Paciente no encontrado",
                    Detail = $"No existe un paciente con el identificador {id}.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Conflict(new ProblemDetails
            {
                Title = "DUI duplicado",
                Detail = "Ya existe otro paciente registrado con ese DUI.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var paciente = await pacienteService.ObtenerPorIdAsync(id);
        return Ok(new PacienteResponseDto(
            paciente!.Id,
            paciente.Nombre,
            paciente.FechaNacimiento,
            paciente.Edad,
            paciente.Dui));
    }

    /// <summary>
    /// Elimina un paciente únicamente si no posee historial clínico asociado.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var eliminado = await pacienteService.EliminarPacienteSinHistorialAsync(id);
        if (!eliminado)
        {
            if (await pacienteService.ObtenerPorIdAsync(id) is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Paciente no encontrado",
                    Detail = $"No existe un paciente con el identificador {id}.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Conflict(new ProblemDetails
            {
                Title = "Eliminación bloqueada por integridad clínica",
                Detail = "No se puede eliminar un paciente que ya posee historial clínico (alergias, enfermedades, consultas o estudios).",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Registra una alergia en el expediente del paciente.
    /// </summary>
    [HttpPost("{id:int}/alergias")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> AddAllergy(int id, [FromBody] AlergiaRequestDto request)
    {
        var ok = await pacienteService.AgregarAlergiaAsync(new AlergiaPacienteViewModel
        {
            PatientId = id,
            TipoAlergia = request.TipoAlergia,
            SustanciaElemento = request.SustanciaElemento,
            Severidad = request.Severidad
        });

        if (!ok)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Paciente no encontrado",
                Detail = $"No existe un paciente con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return CreatedAtAction(nameof(GetById), new { id }, new ApiMessageResponse("Alergia registrada correctamente."));
    }

    /// <summary>
    /// Actualiza una alergia existente del paciente.
    /// </summary>
    [HttpPut("{id:int}/alergias/{alergiaId:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> UpdateAllergy(int id, int alergiaId, [FromBody] AlergiaRequestDto request)
    {
        var ok = await pacienteService.ActualizarAlergiaAsync(new AlergiaPacienteViewModel
        {
            Id = alergiaId,
            PatientId = id,
            TipoAlergia = request.TipoAlergia,
            SustanciaElemento = request.SustanciaElemento,
            Severidad = request.Severidad
        });

        if (!ok)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Alergia no encontrada",
                Detail = $"No se encontró la alergia {alergiaId} para el paciente {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiMessageResponse("Alergia actualizada correctamente."));
    }

    /// <summary>
    /// Elimina una alergia del expediente del paciente.
    /// </summary>
    [HttpDelete("{id:int}/alergias/{alergiaId:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAllergy(int id, int alergiaId)
    {
        if (!await pacienteService.EliminarAlergiaAsync(alergiaId, id))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Alergia no encontrada",
                Detail = $"No se encontró la alergia {alergiaId} para el paciente {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Registra una enfermedad crónica o antecedente patológico para el paciente.
    /// </summary>
    [HttpPost("{id:int}/enfermedades")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> AddDisease(int id, [FromBody] EnfermedadRequestDto request)
    {
        var ok = await pacienteService.AgregarEnfermedadAsync(new EnfermedadViewModel
        {
            PatientId = id,
            Nombre = request.Nombre,
            AnioDiagnostico = request.AnioDiagnostico
        });

        if (!ok)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Paciente no encontrado",
                Detail = $"No existe un paciente con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return CreatedAtAction(nameof(GetById), new { id }, new ApiMessageResponse("Enfermedad registrada correctamente."));
    }

    /// <summary>
    /// Actualiza una enfermedad registrada del paciente.
    /// </summary>
    [HttpPut("{id:int}/enfermedades/{enfermedadId:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> UpdateDisease(int id, int enfermedadId, [FromBody] EnfermedadRequestDto request)
    {
        var ok = await pacienteService.ActualizarEnfermedadAsync(new EnfermedadViewModel
        {
            Id = enfermedadId,
            PatientId = id,
            Nombre = request.Nombre,
            AnioDiagnostico = request.AnioDiagnostico
        });

        if (!ok)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Enfermedad no encontrada",
                Detail = $"No se encontró la enfermedad {enfermedadId} para el paciente {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiMessageResponse("Enfermedad actualizada correctamente."));
    }

    /// <summary>
    /// Elimina una enfermedad registrada del expediente del paciente.
    /// </summary>
    [HttpDelete("{id:int}/enfermedades/{enfermedadId:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDisease(int id, int enfermedadId)
    {
        if (!await pacienteService.EliminarEnfermedadAsync(enfermedadId, id))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Enfermedad no encontrada",
                Detail = $"No se encontró la enfermedad {enfermedadId} para el paciente {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    private static bool TryValidateBirthDate(DateTime fechaNacimiento, out string? error)
    {
        var birthDate = fechaNacimiento.Date;
        if (birthDate > DateTime.Today)
        {
            error = "La fecha de nacimiento no puede ser futura.";
            return false;
        }

        if (birthDate < DateTime.Today.AddYears(-130))
        {
            error = "Ingrese una fecha de nacimiento válida.";
            return false;
        }

        error = null;
        return true;
    }
}
