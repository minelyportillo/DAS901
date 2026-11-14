using ClinicaApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaApp.Migrations;

[DbContext(typeof(ClinicaDbContext))]
[Migration("20261114041932_RegistrarMigracionesDatosClinicos")]
public partial class RegistrarMigracionesDatosClinicos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "applied_data_migrations",
            columns: table => new
            {
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_applied_data_migrations", x => x.Name));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "applied_data_migrations");
    }
}
