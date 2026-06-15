using DonationWorker;
using DonationWorker.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);
var logCounter = 0;
var logTotal = 7;

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
logger.LogInformation(" ***** ({0}/{1}) - Inicializando Worker doacoes ", logCounter++, logTotal);


// ──────────────────────────────────────────────────────────────────────────────
// ── Infrastructure Extensions
// ──────────────────────────────────────────────────────────────────────────────
logger.LogInformation(" ***** ({0}/{1}) - Inicio inicialização de Infrastructure Extensions ", logCounter++, logTotal);
builder.Services.AddDbContext(builder.Configuration, logger);
builder.Services.AddCustomLogging(logger);
builder.Services.AddRepositories(logger);
builder.Services.AddAuditLog(builder.Configuration, logger);
builder.Services.AddMessaging(builder.Configuration, logger);
builder.Services.AddAuthenticationServices(builder.Configuration, logger);
builder.Services.AddMetricsServices(logger);
logger.LogInformation(" ***** ({0}/{1}) - Termino inicialização de Infrastructure Extensions ", logCounter, logTotal);



builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();





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
