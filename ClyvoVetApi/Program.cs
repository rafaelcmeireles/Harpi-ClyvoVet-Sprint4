using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using ClyvoVetApi.Data;
using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.Infrastructure.Repositories;
using ClyvoVetApi.Infrastructure.HealthChecks;
using ClyvoVetApi.Infrastructure.Middleware;
using ClyvoVetApi.Application.Observability;
using System.Reflection;
using System.Text;
using ClyvoVetApi.Infrastructure.Mongo;
using ClyvoVetApi.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

// ===================== SERILOG (Bootstrap Logger) =====================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// ===================== SERILOG (Full Logger) =====================
// Em testes, usa configuracao minima para evitar conflito com WebApplicationFactory
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "Logs/clyvovet-.log",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}")
    );
}

// ===================== ENTITY FRAMEWORK =====================
var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

// Em ambiente de testes usa InMemory; em producao/desenvolvimento usa Oracle
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("ClyvoVetTest"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseOracle(
            connectionString,
            oracleOptions => oracleOptions.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
        )
    );
}

// ===================== REPOSITORIOS E SERVICOS (Application/Infrastructure) =====================
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IPetRepository, EfPetRepository>();
builder.Services.AddScoped<IConsultationRepository, EfConsultationRepository>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();

// ===================== MONGODB (NoSQL - chat das consultas) =====================
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));

if (builder.Environment.IsEnvironment("Testing"))
{
    // Testes não dependem de um servidor MongoDB: usa implementação em memória
    builder.Services.AddSingleton<IChatMessageRepository, InMemoryChatMessageRepository>();
}
else
{
    var mongoSettings = builder.Configuration.GetSection(MongoSettings.SectionName).Get<MongoSettings>() ?? new MongoSettings();
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
    builder.Services.AddSingleton<IChatMessageRepository, MongoChatMessageRepository>();
}

// ===================== AUTENTICACAO / AUTORIZACAO (JWT) =====================
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// ===================== HEALTH CHECK: SERVICO EXTERNO =====================
var externalServiceOptions = builder.Configuration
    .GetSection("HealthChecks:ExternalService")
    .Get<ExternalServiceOptions>() ?? new ExternalServiceOptions();

builder.Services.AddSingleton(externalServiceOptions);
builder.Services.AddHttpClient(ExternalServiceHealthCheck.HttpClientName);

// ===================== HEALTH CHECKS =====================
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API esta saudavel."),
        tags: new[] { "api", "live" });

// Adiciona o check do Oracle apenas fora do ambiente de testes
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddOracle(
        connectionString: connectionString!,
        name: "oracle-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "oracle", "ready" });
}

// MongoDB (chat) apenas fora do ambiente de testes
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddCheck<MongoDbHealthCheck>(
        "mongodb",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "mongo", "ready" });
}

healthChecks.AddCheck<ExternalServiceHealthCheck>(
    "external-services",
    failureStatus: HealthStatus.Degraded,
    tags: new[] { "external", "ready" });

// ===================== OPENTELEMETRY =====================
// Desativado em ambiente de testes para manter os testes de integracao rapidos e deterministicos
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(
                serviceName: ClyvoTelemetry.ServiceName,
                serviceVersion: "4.0.0"))
        .WithTracing(tracing => tracing
            .AddSource(ClyvoTelemetry.ActivitySourceName)
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
                                      && !ctx.Request.Path.StartsWithSegments("/metrics");
            })
            .AddHttpClientInstrumentation()
            .AddConsoleExporter())
        .WithMetrics(metrics => metrics
            .AddMeter(ClyvoTelemetry.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddConsoleExporter()
            .AddPrometheusExporter());
}

// ===================== CONTROLLERS + SWAGGER =====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClyvoVet API",
        Version = "v4",
        Description = "API RESTful do sistema ClyvoVet - entrega final (Sprint 4). " +
                      "Clean Architecture, JWT, paginacao/HATEOAS, Oracle (EF Core) + MongoDB, " +
                      "Health Checks, Serilog, OpenTelemetry e testes automatizados.",
        Contact = new OpenApiContact
        {
            Name = "Equipe Harpi - FIAP",
            Email = "contato@clyvovet.com"
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe o token JWT obtido em POST /api/auth/login (apenas o token, sem o prefixo Bearer).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ===================== MIDDLEWARE =====================

// Tratamento global de exceções: precisa ser o middleware mais externo para capturar
// qualquer falha das etapas seguintes (inclusive do proprio CorrelationIdMiddleware)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Correlation Id: gera/propaga o identificador da requisição antes de tudo o mais rodar
app.UseMiddleware<CorrelationIdMiddleware>();

// Logging de requisições HTTP com Serilog (apenas fora de ambiente de testes)
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000}ms";
    });
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ClyvoVet API v4");
    options.RoutePrefix = string.Empty;
});

// ===================== HEALTH CHECK ENDPOINTS =====================
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint de métricas no formato Prometheus (apenas fora de testes, ver bloco OpenTelemetry acima)
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapPrometheusScrapingEndpoint();
}

Log.Information("ClyvoVet API iniciada com sucesso.");
app.Run();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program { }

