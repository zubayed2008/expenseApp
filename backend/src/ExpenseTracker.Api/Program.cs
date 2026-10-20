using ExpenseTracker.Api;
using ExpenseTracker.Infrastructure;
using ExpenseTracker.Infrastructure.Migrations;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Service;
using ExpenseTracker.WebCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ----- Logging (NFR-060: structured logs via Serilog) -----
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// ----- Fail fast in production when secrets/config are missing (NFR-013) -----
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is required (set ConnectionStrings__DefaultConnection).");
    }

    if ((builder.Configuration["Jwt:SigningKey"] ?? string.Empty).Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey must be at least 32 characters (set Jwt__SigningKey).");
    }
}

// ----- Layered services -----
builder.Services
    .AddInfrastructure(builder.Configuration)   // EF Core + Npgsql, repositories, security
    .AddApplication()                         // CQRS: MediatR handlers + validators
    .AddWebCore(builder.Configuration);        // JWT auth + exception middleware

builder.Services.AddControllers();

// ----- OpenAPI / Swagger (FR-228, NFR-051) -----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Expense Tracker API",
        Version = "v1",
        Description = "REST API for the Expense Tracker platform (SRS v2).",
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token from POST /api/auth/login.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// ----- Apply numbered SQL migrations (opt-in; scripts live in ExpenseTracker.Domain) -----
if (app.Configuration.GetValue<bool>("Database:ApplySqlMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var runner = new SqlMigrationRunner(db);
    await runner.ApplyAllAsync(app.Lifetime.ApplicationStopping);
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebCore();   // exception -> ProblemDetails middleware

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Exposes Program to WebApplicationFactory in the test project.</summary>
public partial class Program;
