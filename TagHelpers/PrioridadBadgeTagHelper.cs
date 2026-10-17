using ClinicaApp.Models.Enums;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ClinicaApp.TagHelpers;

[HtmlTargetElement("prioridad-badge", Attributes = "prioridad")]
public sealed class PrioridadBadgeTagHelper : TagHelper
{
    public PrioridadAtencion Prioridad { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var (texto, clase) = Prioridad switch
        {
            PrioridadAtencion.Baja => ("Baja", "text-bg-secondary"),
            PrioridadAtencion.Media => ("Media", "text-bg-primary"),
            PrioridadAtencion.Alta => ("Alta", "text-bg-danger"),
            _ => throw new ArgumentOutOfRangeException(nameof(Prioridad), Prioridad, "Prioridad no reconocida.")
        };

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge rounded-pill {clase}");
        output.Attributes.SetAttribute("aria-label", $"Prioridad {texto}");
        output.Content.SetContent(texto);
    }
}
