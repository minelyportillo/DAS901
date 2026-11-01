using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicaApp.Migrations
{
    /// <inheritdoc />
    public partial class InicializarBaseDatosClinica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medicamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicamentos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    fecha_nacimiento = table.Column<DateTime>(type: "date", nullable: false),
                    DUI = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alergias_paciente",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    tipo_alergia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sustancia_elemento = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    severidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alergias_paciente", x => x.id);
                    table.ForeignKey(
                        name: "FK_alergias_paciente_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consultas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    fecha_hora_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_hora_finalizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    examen_fisico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    diagnostico = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    prioridad = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultas", x => x.id);
                    table.ForeignKey(
                        name: "FK_consultas_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enfermedades",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    anio_diagnostico = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enfermedades", x => x.id);
                    table.ForeignKey(
                        name: "FK_enfermedades_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incapacidades",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_consulta = table.Column<int>(type: "int", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "date", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incapacidades", x => x.id);
                    table.ForeignKey(
                        name: "FK_incapacidades_consultas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "consultas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ordenes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    consulta_id = table.Column<int>(type: "int", nullable: false),
                    fecha_hora_emision = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ordenes", x => x.id);
                    table.ForeignKey(
                        name: "FK_ordenes_consultas_consulta_id",
                        column: x => x.consulta_id,
                        principalTable: "consultas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receta",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_consulta = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receta", x => x.id);
                    table.ForeignKey(
                        name: "FK_receta_consultas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "consultas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signos_vitales",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_consulta = table.Column<int>(type: "int", nullable: false),
                    temperatura = table.Column<double>(type: "float", nullable: true),
                    altura = table.Column<double>(type: "float", nullable: true),
                    peso = table.Column<double>(type: "float", nullable: true),
                    presion_sistolica = table.Column<int>(type: "int", nullable: true),
                    presion_diastolica = table.Column<int>(type: "int", nullable: true),
                    concentracion_oxigeno = table.Column<int>(type: "int", nullable: true),
                    ritmo_cardiaco = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signos_vitales", x => x.id);
                    table.ForeignKey(
                        name: "FK_signos_vitales_consultas_id_consulta",
                        column: x => x.id_consulta,
                        principalTable: "consultas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detalle_orden",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_orden = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    indicaciones = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_orden", x => x.id);
                    table.ForeignKey(
                        name: "FK_detalle_orden_ordenes_id_orden",
                        column: x => x.id_orden,
                        principalTable: "ordenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detalle_receta",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_receta = table.Column<int>(type: "int", nullable: false),
                    id_medicamento = table.Column<int>(type: "int", nullable: true),
                    dosis = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    frecuencia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    duracion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    indicaciones = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_receta", x => x.id);
                    table.ForeignKey(
                        name: "FK_detalle_receta_medicamentos_id_medicamento",
                        column: x => x.id_medicamento,
                        principalTable: "medicamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_detalle_receta_receta_id_receta",
                        column: x => x.id_receta,
                        principalTable: "receta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estudios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_paciente = table.Column<int>(type: "int", nullable: false),
                    id_detalle_orden = table.Column<int>(type: "int", nullable: true),
                    id_consulta_lectura = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    observaciones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estudios", x => x.id);
                    table.ForeignKey(
                        name: "FK_estudios_consultas_id_consulta_lectura",
                        column: x => x.id_consulta_lectura,
                        principalTable: "consultas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estudios_detalle_orden_id_detalle_orden",
                        column: x => x.id_detalle_orden,
                        principalTable: "detalle_orden",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_estudios_pacientes_id_paciente",
                        column: x => x.id_paciente,
                        principalTable: "pacientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "medicamentos",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Paracetamol 500 mg" },
                    { 2, "Ibuprofeno 400 mg" },
                    { 3, "Amoxicilina 500 mg" },
                    { 4, "Omeprazol 20 mg" },
                    { 5, "Loratadina 10 mg" },
                    { 6, "Azitromicina 500 mg" },
                    { 7, "Losartán 50 mg" },
                    { 8, "Metformina 850 mg" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_alergias_paciente_id_paciente",
                table: "alergias_paciente",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_consultas_id_paciente",
                table: "consultas",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_orden_id_orden",
                table: "detalle_orden",
                column: "id_orden");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_receta_id_medicamento",
                table: "detalle_receta",
                column: "id_medicamento");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_receta_id_receta",
                table: "detalle_receta",
                column: "id_receta");

            migrationBuilder.CreateIndex(
                name: "IX_enfermedades_id_paciente",
                table: "enfermedades",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_estudios_id_consulta_lectura",
                table: "estudios",
                column: "id_consulta_lectura");

            migrationBuilder.CreateIndex(
                name: "IX_estudios_id_detalle_orden",
                table: "estudios",
                column: "id_detalle_orden");

            migrationBuilder.CreateIndex(
                name: "IX_estudios_id_paciente",
                table: "estudios",
                column: "id_paciente");

            migrationBuilder.CreateIndex(
                name: "IX_incapacidades_id_consulta",
                table: "incapacidades",
                column: "id_consulta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ordenes_consulta_id",
                table: "ordenes",
                column: "consulta_id");

            migrationBuilder.CreateIndex(
                name: "IX_receta_id_consulta",
                table: "receta",
                column: "id_consulta");

            migrationBuilder.CreateIndex(
                name: "IX_signos_vitales_id_consulta",
                table: "signos_vitales",
                column: "id_consulta",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alergias_paciente");

            migrationBuilder.DropTable(
                name: "detalle_receta");

            migrationBuilder.DropTable(
                name: "enfermedades");

            migrationBuilder.DropTable(
                name: "estudios");

            migrationBuilder.DropTable(
                name: "incapacidades");

            migrationBuilder.DropTable(
                name: "signos_vitales");

            migrationBuilder.DropTable(
                name: "medicamentos");

            migrationBuilder.DropTable(
                name: "receta");

            migrationBuilder.DropTable(
                name: "detalle_orden");

            migrationBuilder.DropTable(
                name: "ordenes");

            migrationBuilder.DropTable(
                name: "consultas");

            migrationBuilder.DropTable(
                name: "pacientes");
        }
    }
}
