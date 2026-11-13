# DAS901
APPCLINICA

## Análisis de consultas con LINQ

El módulo **Análisis** (`/Analisis`) presenta consultas de EF Core para:

- filtrar consultas por periodo, estado y prioridad;
- ordenarlas por prioridad y fecha, con paginación;
- agruparlas por estado y día;
- unir consultas con pacientes y signos vitales;
- resumir diagnósticos, medicamentos prescritos y duración promedio.

Las lecturas usan `AsNoTracking`, proyecciones a modelos de vista y operaciones
de filtrado, agrupación y paginación ejecutadas por SQL Server. Para preparar
el esquema de la base de datos, configura `DefaultConnection` en
`appsettings.json` y ejecuta `dotnet ef database update`.

El resto de las pantallas clínicas todavía persiste sus datos en
`App_Data/clinic-data.json`; el módulo de análisis consulta la base de datos
configurada en EF Core y no importa ni modifica automáticamente ese archivo.
