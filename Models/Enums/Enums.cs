namespace ClinicaApp.Models.Enums
{
    // Roles de acceso al sistema
    public enum RolUsuario
    {
        Secretaria = 1,
        Doctora = 2
    }

    // Prioridad clínica utilizada para ordenar la sala de espera.
    public enum PrioridadAtencion
    {
        Baja = 1,
        Media = 2,
        Alta = 3
    }

    // Ciclo de atención del paciente durante el día
    public enum EstadoConsulta
    {
        Esperando = 1,
        EnAtencion = 2,
        Cerrada = 3
    }

    // Control de órdenes clínicas
    public enum EstadoOrdenExamen
    {
        Pendiente = 1,
        Completado = 2
    }
}