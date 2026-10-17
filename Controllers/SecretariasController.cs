using ClinicaApp.Services;
using ClinicaApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaApp.Controllers;

[Authorize(Roles = "Doctora")]
public sealed class SecretariasController(IClinicUserService userService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await userService.GetSecretariesAsync());
    }

    [HttpGet]
    public IActionResult Crear() => View(new CreateSecretaryViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CreateSecretaryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await userService.CreateSecretaryAsync(model.Name, model.Email, model.Password);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.Email), exception.Message);
            return View(model);
        }

        TempData["Mensaje"] = $"La cuenta de {model.Name.Trim()} se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        if (!await userService.DeleteSecretaryAsync(id))
        {
            return NotFound();
        }

        TempData["Mensaje"] = "La cuenta de secretaría se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
