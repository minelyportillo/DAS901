using System.Text.Json;
using ClinicaApp.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace ClinicaApp.Services;

public sealed class JsonClinicUserService : IClinicUserService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _usersFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly IPasswordHasher<ClinicUser> _passwordHasher = new PasswordHasher<ClinicUser>();

    public JsonClinicUserService(IWebHostEnvironment environment)
    {
        _usersFilePath = Path.Combine(environment.ContentRootPath, "App_Data", "users.json");
    }

    public async Task InitializeAsync(IConfigurationSection developmentSeedUsers, bool isDevelopment)
    {
        await _fileLock.WaitAsync();
        try
        {
            if (File.Exists(_usersFilePath))
            {
                _ = await ReadUsersAsync();
                return;
            }

            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "No hay usuarios configurados. Crea App_Data/users.json de forma segura antes de iniciar fuera de Development.");
            }

            var seeds = developmentSeedUsers.Get<List<DevelopmentSeedUser>>();
            if (seeds is null || seeds.Count == 0)
            {
                throw new InvalidOperationException(
                    "No se configuraron las cuentas iniciales. Revisa DevelopmentSeedUsers en appsettings.Development.json.");
            }

            var users = new List<ClinicUser>();
            foreach (var seed in seeds)
            {
                if (string.IsNullOrWhiteSpace(seed.Name)
                    || string.IsNullOrWhiteSpace(seed.Email)
                    || string.IsNullOrEmpty(seed.Password)
                    || string.IsNullOrWhiteSpace(seed.Role))
                {
                    throw new InvalidOperationException("Cada cuenta inicial debe tener nombre, correo, contraseña y rol.");
                }

                var user = new ClinicUser
                {
                    Name = seed.Name.Trim(),
                    Email = seed.Email.Trim(),
                    NormalizedEmail = NormalizeEmail(seed.Email),
                    Role = seed.Role.Trim()
                };

                if (user.Role is not (nameof(RolUsuario.Doctora) or nameof(RolUsuario.Secretaria)))
                {
                    throw new InvalidOperationException($"El rol inicial '{user.Role}' no es válido.");
                }

                if (users.Any(existing => existing.NormalizedEmail == user.NormalizedEmail))
                {
                    throw new InvalidOperationException($"El correo inicial '{user.Email}' está repetido.");
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, seed.Password);
                users.Add(user);
            }

            await WriteUsersAsync(users);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<ClinicUserSummary?> AuthenticateAsync(string email, string password)
    {
        await _fileLock.WaitAsync();
        try
        {
            var normalizedEmail = NormalizeEmail(email);
            var users = await ReadUsersAsync();
            var user = users.FirstOrDefault(candidate => candidate.NormalizedEmail == normalizedEmail);
            if (user is null)
            {
                return null;
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                await WriteUsersAsync(users);
            }

            return ToSummary(user);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<ClinicUserSummary?> GetUserByIdAsync(Guid id)
    {
        await _fileLock.WaitAsync();
        try
        {
            var user = (await ReadUsersAsync()).FirstOrDefault(candidate => candidate.Id == id);
            return user is null ? null : ToSummary(user);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IReadOnlyList<ClinicUserSummary>> GetSecretariesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            return (await ReadUsersAsync())
                .Where(user => user.Role == nameof(RolUsuario.Secretaria))
                .OrderBy(user => user.Name)
                .Select(ToSummary)
                .ToArray();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task CreateSecretaryAsync(string name, string email, string password)
    {
        await _fileLock.WaitAsync();
        try
        {
            var users = await ReadUsersAsync();
            var normalizedEmail = NormalizeEmail(email);
            if (users.Any(user => user.NormalizedEmail == normalizedEmail))
            {
                throw new InvalidOperationException("Ya existe una cuenta con ese correo.");
            }

            var secretary = new ClinicUser
            {
                Name = name.Trim(),
                Email = email.Trim(),
                NormalizedEmail = normalizedEmail,
                Role = nameof(RolUsuario.Secretaria)
            };
            secretary.PasswordHash = _passwordHasher.HashPassword(secretary, password);
            users.Add(secretary);
            await WriteUsersAsync(users);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> DeleteSecretaryAsync(Guid id)
    {
        await _fileLock.WaitAsync();
        try
        {
            var users = await ReadUsersAsync();
            var removed = users.RemoveAll(user =>
                user.Id == id && user.Role == nameof(RolUsuario.Secretaria));
            if (removed == 0)
            {
                return false;
            }

            await WriteUsersAsync(users);
            return true;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<ClinicUser>> ReadUsersAsync()
    {
        await using var stream = new FileStream(
            _usersFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return await JsonSerializer.DeserializeAsync<List<ClinicUser>>(stream, JsonOptions)
            ?? throw new InvalidDataException("El archivo local de usuarios no contiene una lista válida.");
    }

    private async Task WriteUsersAsync(List<ClinicUser> users)
    {
        var directory = Path.GetDirectoryName(_usersFilePath)
            ?? throw new InvalidOperationException("No se pudo resolver la carpeta local de usuarios.");
        Directory.CreateDirectory(directory);

        var temporaryFile = $"{_usersFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryFile,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
                await stream.FlushAsync();
            }

            File.Move(temporaryFile, _usersFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static ClinicUserSummary ToSummary(ClinicUser user) =>
        new(user.Id, user.Name, user.Email, user.Role);

    private sealed class DevelopmentSeedUser
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
