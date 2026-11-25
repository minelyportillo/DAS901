using ClinicaApp.DTOs;
using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers.Api;

/// <summary>
/// Endpoints RESTful para la gestión del catálogo de medicamentos.
/// </summary>
[ApiController]
[Route("api/medicamentos")]
[Produces("application/json")]
[Authorize(Roles = "Secretaria,Doctora")]
public sealed class MedicamentosApiController(
    IMedicamentoService medicamentoService) : ControllerBase
{
    /// <summary>
    /// Obtiene todos los medicamentos disponibles en el catálogo ordenados alfabéticamente.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicationOption>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicationOption>>> GetAll()
    {
        var catalogo = await medicamentoService.ObtenerCatalogoAsync();
        return Ok(catalogo);
    }

    /// <summary>
    /// Obtiene un medicamento específico por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MedicationOption), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicationOption>> GetById(int id)
    {
        var medicamento = await medicamentoService.ObtenerPorIdAsync(id);
        if (medicamento is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Medicamento no encontrado",
                Detail = $"No existe un medicamento con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(medicamento);
    }

    /// <summary>
    /// Registra un nuevo medicamento en el catálogo.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctora")]
    [ProducesResponseType(typeof(MedicationOption), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicationOption>> Create([FromBody] MedicamentoRequestDto request)
    {
        var id = await medicamentoService.RegistrarAsync(request.Nombre);
        var creado = await medicamentoService.ObtenerPorIdAsync(id);

        return CreatedAtAction(nameof(GetById), new { id }, creado);
    }

    /// <summary>
    /// Actualiza el nombre de un medicamento del catálogo.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctora")]
    [ProducesResponseType(typeof(MedicationOption), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MedicationOption>> Update(int id, [FromBody] MedicamentoRequestDto request)
    {
        var actualizado = await medicamentoService.ActualizarAsync(id, request.Nombre);
        if (!actualizado)
        {
            if (await medicamentoService.ObtenerPorIdAsync(id) is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Medicamento no encontrado",
                    Detail = $"No existe un medicamento con el identificador {id}.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Conflict(new ProblemDetails
            {
                Title = "Medicamento duplicado",
                Detail = "Ya existe otro medicamento registrado con ese nombre en el catálogo.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var medicamento = await medicamentoService.ObtenerPorIdAsync(id);
        return Ok(medicamento);
    }

    /// <summary>
    /// Elimina un medicamento del catálogo si aún no ha sido prescrito en ninguna receta.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Doctora")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await medicamentoService.ObtenerPorIdAsync(id) is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Medicamento no encontrado",
                Detail = $"No existe un medicamento con el identificador {id}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var eliminado = await medicamentoService.EliminarSiNoEstaPrescritoAsync(id);
        if (!eliminado)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Medicamento en uso",
                Detail = "No se puede eliminar un medicamento que ya está asociado a recetas médicas.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }
}
