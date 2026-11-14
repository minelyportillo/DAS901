using ClinicaApp.Data;
using ClinicaApp.Repositories;
using ClinicaApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ClinicaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<ClinicaDbContext>());
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<IAlergiaPacienteRepository, AlergiaPacienteRepository>();
builder.Services.AddScoped<IEnfermedadRepository, EnfermedadRepository>();
builder.Services.AddScoped<IMedicamentoRepository, MedicamentoRepository>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IMedicamentoService, MedicamentoService>();
builder.Services.AddScoped<IConsultationAnalysisService, ConsultationAnalysisService>();
builder.Services.AddScoped<LegacyClinicDataImporter>();
builder.Services.AddSingleton<IClinicUserService, JsonClinicUserService>();
builder.Services.AddScoped<IClinicDataStore, ClinicDataStore>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "ClinicaApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim?.Value, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var users = context.HttpContext.RequestServices.GetRequiredService<IClinicUserService>();
            var currentUser = await users.GetUserByIdAsync(userId);
            var cookieRole = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUser is null || currentUser.Role != cookieRole)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicaDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider
        .GetRequiredService<LegacyClinicDataImporter>()
        .ImportIfPendingAsync();
}

var userService = app.Services.GetRequiredService<IClinicUserService>();
await userService.InitializeAsync(
    app.Configuration.GetSection("DevelopmentSeedUsers"),
    app.Environment.IsDevelopment());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
