using System.Text;
using FraudDetection.Api.ErrorHandling;
using FraudDetection.Application;
using FraudDetection.Infrastructure;
using FraudDetection.Infrastructure.Identity;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    // Every endpoint requires a valid bearer token by default; AuthController opts
    // out with [AllowAnonymous] since a caller can't have a token before registering
    // or logging in. Layering per-role policies on top of this is a matter of adding
    // [Authorize(Roles = "...")] to individual controllers/actions later — nothing
    // here needs to change for that.
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fraud Detection API",
        Version = "v1",
        Description = "Ingests categorized transaction events, evaluates them against a set of fraud rules, and exposes the results."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT bearer token from POST /api/auth/login or /api/auth/register. Example: \"Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition(bearerScheme.Reference.Id, bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException($"Missing '{JwtSettings.SectionName}' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString(FraudDetection.Infrastructure.DependencyInjection.ConnectionStringName);
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString ?? throw new InvalidOperationException("Missing connection string for health checks."));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await MigrateDatabaseAsync(app);

app.Run();

// Applies any pending EF Core migrations on startup, effectively auto-creating the
// database schema the first time the API runs against a fresh PostgreSQL instance.
// Retries briefly to ride out the race where the API container starts slightly ahead
// of Postgres becoming ready to accept connections.
static async Task MigrateDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    var delay = TimeSpan.FromSeconds(3);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            logger.LogInformation("Applying database migrations (attempt {Attempt}/{MaxAttempts})...", attempt, maxAttempts);
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready yet, retrying in {Delay}s...", delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }
}

// Exposes the generated entry point type to WebApplicationFactory<Program> in the
// integration test project.
public partial class Program;
