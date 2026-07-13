using DonationWorker;
using DonationWorker.Api.Middlewares;
using DonationWorker.Infrastructure.Extensions;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
var logCounter = 0;
var logTotal = 2;

// ──────────────────────────────────────────────────────────────────────────────
// ── Configuration
// ──────────────────────────────────────────────────────────────────────────────
ConfigureAppSettings(builder.Configuration);


// ──────────────────────────────────────────────────────────────────────────────
// ── Logs
// ──────────────────────────────────────────────────────────────────────────────
using var loggerFactory = LoggerFactory.Create(logging => {
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    logging.AddSimpleConsole();
});
var logger = loggerFactory.CreateLogger("Program");
logger.LogInformation(" ***** Inicializando Worker doacoes ");
logCounter++;


// ──────────────────────────────────────────────────────────────────────────────
// ── Infrastructure Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Infrastructure Extensions ", logCounter, logTotal);
builder.Services.AddDbContext(builder.Configuration, logger);
builder.Services.AddCustomLogging(logger);
builder.Services.AddRepositories(logger);
builder.Services.AddAuditLog(builder.Configuration, logger);
builder.Services.AddMessaging(builder.Configuration, logger);
builder.Services.AddAuthenticationServices(builder.Configuration, logger);
builder.Services.AddMetricsServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Infrastructure Extensions ", logCounter++, logTotal);


builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// ──────────────────────────────────────────────────────────────────────────────
// ── Health Check
// ──────────────────────────────────────────────────────────────────────────────
app.MapMetrics("/metrics");
app.MapGet("/health", () => Results.Ok("Healthy"));


// ──────────────────────────────────────────────────────────────────────────────
// ── Middlwares
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Middlewares ", logCounter, logTotal);
app.UseExceptionMiddleware();
app.UseMetricsMiddleware();
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Middlewares ", logCounter++, logTotal);


app.Run();




#region Helper Methods
void ConfigureAppSettings(ConfigurationManager config)
{
    string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                       ?? "Production";

    config.SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
          .AddEnvironmentVariables();
}
#endregion