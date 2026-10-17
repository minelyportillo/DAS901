namespace ClinicaApp.Services;

public interface IClinicUserService
{
    Task InitializeAsync(IConfigurationSection developmentSeedUsers, bool isDevelopment);
    Task<ClinicUserSummary?> AuthenticateAsync(string email, string password);
    Task<ClinicUserSummary?> GetUserByIdAsync(Guid id);
    Task<IReadOnlyList<ClinicUserSummary>> GetSecretariesAsync();
    Task CreateSecretaryAsync(string name, string email, string password);
    Task<bool> DeleteSecretaryAsync(Guid id);
}
