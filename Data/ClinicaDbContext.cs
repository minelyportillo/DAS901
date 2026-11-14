using ClinicaApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaApp.Data
{
    public class ClinicaDbContext(DbContextOptions<ClinicaDbContext> options) : DbContext(options), IUnitOfWork
    {
        public DbSet<Paciente> Pacientes => Set<Paciente>();
        public DbSet<Enfermedad> Enfermedades => Set<Enfermedad>();
        public DbSet<AlergiaPaciente> AlergiasPaciente => Set<AlergiaPaciente>();
        public DbSet<Consulta> Consultas => Set<Consulta>();
        public DbSet<SignosVitales> SignosVitales => Set<SignosVitales>();
        public DbSet<Incapacidad> Incapacidades => Set<Incapacidad>();
        public DbSet<Receta> Recetas => Set<Receta>();
        public DbSet<DetalleReceta> DetallesReceta => Set<DetalleReceta>();
        public DbSet<Medicamento> Medicamentos => Set<Medicamento>();
        public DbSet<Orden> Ordenes => Set<Orden>();
        public DbSet<DetalleOrden> DetallesOrden => Set<DetalleOrden>();
        public DbSet<Estudio> Estudios => Set<Estudio>();
        public DbSet<AppliedDataMigration> AppliedDataMigrations => Set<AppliedDataMigration>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppliedDataMigration>(entity =>
            {
                entity.ToTable("applied_data_migrations");
                entity.HasKey(migration => migration.Name);
                entity.Property(migration => migration.Name).HasMaxLength(100);
                entity.Property(migration => migration.AppliedAtUtc).IsRequired();
            });

            // 1. Tabla pacientes
            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.ToTable("pacientes");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).HasMaxLength(150).IsRequired();
                entity.Property(p => p.FechaNacimiento).HasColumnType("date").IsRequired();
                entity.Property(p => p.Dui).HasMaxLength(10).IsRequired(false);
            });

            // 2. Relación 1:N -> pacientes a enfermedades
            modelBuilder.Entity<Enfermedad>(entity =>
            {
                entity.ToTable("enfermedades");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
                entity.Property(e => e.AnioDiagnostico).IsRequired();

                entity.HasOne(e => e.Paciente)
                    .WithMany(p => p.Enfermedades)
                    .HasForeignKey(e => e.IdPaciente)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 3. Relación 1:N -> pacientes a alergias_paciente
            modelBuilder.Entity<AlergiaPaciente>(entity =>
            {
                entity.ToTable("alergias_paciente");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.TipoAlergia).HasMaxLength(100).IsRequired();
                entity.Property(a => a.SustanciaElemento).HasMaxLength(150).IsRequired();
                entity.Property(a => a.Severidad).HasMaxLength(50).IsRequired();

                entity.HasOne(a => a.Paciente)
                    .WithMany(p => p.Alergias)
                    .HasForeignKey(a => a.IdPaciente)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 4. Relación 1:N -> pacientes a consultas
            modelBuilder.Entity<Consulta>(entity =>
            {
                entity.ToTable("consultas");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.FechaHoraInicio).IsRequired();
                entity.Property(c => c.FechaHoraFinalizacion).IsRequired(false);
                entity.Property(c => c.Motivo).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(c => c.ExamenFisico).HasColumnType("nvarchar(max)").IsRequired(false);
                entity.Property(c => c.Diagnostico).HasMaxLength(500).IsRequired(false);
                entity.Property(c => c.Prioridad).IsRequired();
                entity.Property(c => c.Estado).IsRequired();

                entity.HasOne(c => c.Paciente)
                    .WithMany(p => p.Consultas)
                    .HasForeignKey(c => c.IdPaciente)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 5. Relación 1:1 -> consultas a signos_vitales
            modelBuilder.Entity<SignosVitales>(entity =>
            {
                entity.ToTable("signos_vitales");
                entity.HasKey(sv => sv.Id);
                entity.HasIndex(sv => sv.IdConsulta).IsUnique();

                entity.HasOne(sv => sv.Consulta)
                    .WithOne(c => c.SignosVitales)
                    .HasForeignKey<SignosVitales>(sv => sv.IdConsulta)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 6. Relación 1:0..1 -> consultas a incapacidades
            modelBuilder.Entity<Incapacidad>(entity =>
            {
                entity.ToTable("incapacidades");
                entity.HasKey(i => i.Id);
                entity.HasIndex(i => i.IdConsulta).IsUnique();
                entity.Property(i => i.FechaInicio).HasColumnType("date").IsRequired();
                entity.Property(i => i.FechaFin).HasColumnType("date").IsRequired();
                entity.Property(i => i.Motivo).HasColumnType("nvarchar(max)").IsRequired();

                entity.HasOne(i => i.Consulta)
                    .WithOne(c => c.Incapacidad)
                    .HasForeignKey<Incapacidad>(i => i.IdConsulta)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 7. Relación 1:N -> consultas a receta
            modelBuilder.Entity<Receta>(entity =>
            {
                entity.ToTable("receta");
                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.Consulta)
                    .WithMany(c => c.Recetas)
                    .HasForeignKey(r => r.IdConsulta)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 8. Tabla medicamentos y datos semilla iniciales
            modelBuilder.Entity<Medicamento>(entity =>
            {
                entity.ToTable("medicamentos");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.Nombre).HasMaxLength(150).IsRequired();

                entity.HasData(
                    new Medicamento { Id = 1, Nombre = "Paracetamol 500 mg" },
                    new Medicamento { Id = 2, Nombre = "Ibuprofeno 400 mg" },
                    new Medicamento { Id = 3, Nombre = "Amoxicilina 500 mg" },
                    new Medicamento { Id = 4, Nombre = "Omeprazol 20 mg" },
                    new Medicamento { Id = 5, Nombre = "Loratadina 10 mg" },
                    new Medicamento { Id = 6, Nombre = "Azitromicina 500 mg" },
                    new Medicamento { Id = 7, Nombre = "Losartán 50 mg" },
                    new Medicamento { Id = 8, Nombre = "Metformina 850 mg" }
                );
            });

            // 9. Relación 1:N -> receta a detalle_receta y N:1 -> detalle_receta a medicamentos
            modelBuilder.Entity<DetalleReceta>(entity =>
            {
                entity.ToTable("detalle_receta");
                entity.HasKey(dr => dr.Id);
                entity.Property(dr => dr.Dosis).HasMaxLength(100).IsRequired();
                entity.Property(dr => dr.Frecuencia).HasMaxLength(100).IsRequired();
                entity.Property(dr => dr.Duracion).HasMaxLength(100).IsRequired();
                entity.Property(dr => dr.Indicaciones).HasColumnType("nvarchar(max)").IsRequired();

                entity.HasOne(dr => dr.Receta)
                    .WithMany(r => r.Detalles)
                    .HasForeignKey(dr => dr.IdReceta)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dr => dr.Medicamento)
                    .WithMany(m => m.DetallesReceta)
                    .HasForeignKey(dr => dr.IdMedicamento)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 10. Relación 1:N -> consultas a ordenes
            modelBuilder.Entity<Orden>(entity =>
            {
                entity.ToTable("ordenes");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.FechaHoraEmision).IsRequired();

                entity.HasOne(o => o.Consulta)
                    .WithMany(c => c.Ordenes)
                    .HasForeignKey(o => o.ConsultaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 11. Relación 1:N -> ordenes a detalle_orden
            modelBuilder.Entity<DetalleOrden>(entity =>
            {
                entity.ToTable("detalle_orden");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Nombre).HasMaxLength(150).IsRequired();
                entity.Property(d => d.Indicaciones).HasColumnType("nvarchar(max)").IsRequired();

                entity.HasOne(d => d.Orden)
                    .WithMany(o => o.Detalles)
                    .HasForeignKey(d => d.IdOrden)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 12. Relaciones de estudios (N:1 con pacientes, N:1 opcional con detalle_orden, N:1 con consultas)
            modelBuilder.Entity<Estudio>(entity =>
            {
                entity.ToTable("estudios");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired(false);
                entity.Property(e => e.Observaciones).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(e => e.Fecha).HasColumnType("date").IsRequired();

                entity.HasOne(e => e.Paciente)
                    .WithMany(p => p.Estudios)
                    .HasForeignKey(e => e.IdPaciente)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DetalleOrden)
                    .WithMany(d => d.Estudios)
                    .HasForeignKey(e => e.IdDetalleOrden)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ConsultaLectura)
                    .WithMany(c => c.EstudiosLeidos)
                    .HasForeignKey(e => e.IdConsultaLectura)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
