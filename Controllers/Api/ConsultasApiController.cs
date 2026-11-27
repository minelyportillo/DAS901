using ClinicaApp.DTOs;
using ClinicaApp.Models.Enums;
using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers.Api;

/// <summary>
/// Endpoints RESTful para la gestión de consultas, sala de espera, triaje y reportes analíticos LINQ.
/// </summary>
[ApiController]
[Route("api/consultas")]
[Produces("application/json")]
[Authorize(Roles = "Secretaria,Doctora")]
public sealed class ConsultasApiController(
    IClinicDataStore clinicData,
    IConsultationAnalysisService analysisService) : ControllerBase
{
    /// <summary>
    /// Obtiene la cola actual de pacientes en la sala de espera para la fecha indicada (por defecto hoy).
    /// </summary>
    [HttpGet("sala-espera")]
    [ProducesResponseType(typeof(IReadOnlyList<WaitingRoomItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WaitingRoomItem>>> GetWaitingRoom([FromQuery] DateTime? fecha)
    {
        var items = await clinicData.GetWaitingRoomAsync(fecha?.Date ?? DateTime.Today);
        return Ok(items);
    }

    /// <summary>
    /// Obtiene el detalle completo de una consulta médica por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ConsultationPageData), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultationPageData>> GetById(int id)
    {
        var consulta = await clinicData.GetConsultationAsync(id);
        if (consulta is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Consulta no encontrada",
                Detail = $"No existe una consulta médica con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(consulta);
    }

    /// <summary>
    /// Registra la llegada de un paciente a la sala de espera creando una nueva consulta activa.
    /// </summary>
    [HttpPost("llegada/{pacienteId:int}")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ConsultationPageData), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultationPageData>> RegisterArrival(int pacienteId)
    {
        var consultaId = await clinicData.RegisterArrivalAsync(pacienteId);
        if (consultaId <= 0)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Paciente no encontrado",
                Detail = $"No existe un paciente con el identificador {pacienteId}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var consulta = await clinicData.GetConsultationAsync(consultaId);
        return CreatedAtAction(nameof(GetById), new { id = consultaId }, consulta);
    }

    /// <summary>
    /// Actualiza la prioridad de atención de una consulta en sala de espera.
    /// </summary>
    [HttpPut("{id:int}/prioridad")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> UpdatePriority(int id, [FromBody] PrioridadConsultaRequestDto request)
    {
        if (!Enum.IsDefined(request.Prioridad))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Prioridad inválida",
                Detail = "El valor de prioridad especificado no es válido.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var ok = await clinicData.SetPriorityAsync(id, request.Prioridad);
        if (!ok)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Consulta no disponible",
                Detail = $"No se encontró una consulta abierta con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiMessageResponse("Prioridad de la consulta actualizada correctamente."));
    }

    /// <summary>
    /// Registra o actualiza los signos vitales iniciales de triaje (peso, altura, temperatura).
    /// </summary>
    [HttpPut("{id:int}/triaje")]
    [Authorize(Policy = "ApiBearer")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> SaveTriage(int id, [FromBody] TriajeRequestDto request)
    {
        var ok = await clinicData.SaveTriageAsync(new TriageViewModel
        {
            ConsultationId = id,
            Peso = request.Peso,
            Altura = request.Altura,
            Temperatura = request.Temperatura
        });

        if (!ok)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Consulta no disponible",
                Detail = $"No se encontró una consulta abierta con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiMessageResponse("Signos vitales de triaje actualizados correctamente."));
    }

    /// <summary>
    /// Finaliza una consulta médica que se encuentra en atención.
    /// </summary>
    [HttpDelete("{id:int}/cerrar")]
    [Authorize(Policy = "ApiBearer")]
    [Authorize(Roles = "Doctora")]
    [ProducesResponseType(typeof(ApiMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiMessageResponse>> CloseConsultation(int id)
    {
        if (!await clinicData.CloseConsultationAsync(id))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Consulta no encontrada o ya cerrada",
                Detail = $"No se pudo cerrar la consulta {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiMessageResponse("La consulta médica fue cerrada correctamente."));
    }

    /// <summary>
    /// Obtiene el reporte analítico y estadístico de consultas médicas mediante consultas LINQ.
    /// </summary>
    [HttpGet("analisis")]
    [ProducesResponseType(typeof(ConsultationAnalysisViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultationAnalysisViewModel>> GetAnalysis(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] EstadoConsulta? estado,
        [FromQuery] PrioridadAtencion? prioridad,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10,
        CancellationToken cancellationToken = default)
    {
        var filters = new ConsultationAnalysisViewModel
        {
            Desde = desde?.Date ?? DateTime.Today.AddDays(-29),
            Hasta = hasta?.Date ?? DateTime.Today,
            Estado = estado,
            Prioridad = prioridad,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };

        if (filters.Desde > filters.Hasta)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Rango de fechas inválido",
                Detail = "La fecha inicial debe ser menor o igual a la fecha final.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var report = await analysisService.GetReportAsync(filters, cancellationToken);
        return Ok(report);
    }
}
