using System.ComponentModel.DataAnnotations;

namespace ClinicaApp.Data;

public sealed class AppliedDataMigration
{
    [Key]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime AppliedAtUtc { get; set; }
}
