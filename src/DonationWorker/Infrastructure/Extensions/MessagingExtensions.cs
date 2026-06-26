using Amazon.SQS;
using DonationWorker.Consumers;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Messaging;
using MassTransit;
using MassTransit.Serialization;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class MessagingExtensions
    {
        public static IServiceCollection AddMessaging(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger)
        {
            services.AddScoped<IMessageService, MessageService>();

            var applicationType = Environment.GetEnvironmentVariable("Application__Type")
                ?? configuration["ApplicationType"]
                ?? "LOCAL";

            logger.LogInformation("***** APPLICATION_TYPE detectado: {Type}", applicationType);

            services.AddMassTransit(x =>
            {
                x.AddConsumer<DonationCreatedEventConsumer>();
                x.AddDelayedMessageScheduler();

                if (applicationType == "LOCAL")
                {
                    ConfigurarRabbitMq(x, configuration);
                    logger.LogInformation("***** MassTransit: Usando RabbitMQ (LOCAL).");
                }
                else
                {
                    ConfigurarSqs(x, configuration, logger);
                    logger.LogInformation("***** MassTransit: Usando Amazon SQS (LAB/AWS).");
                }
            });

            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.WaitUntilStarted = false;
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(15);
            });

            ConfigurarClienteSqs(services, configuration, applicationType, logger);

            logger.LogInformation("***** MassTransit inicializado.");
            return services;
        }

        // ── RabbitMQ (LOCAL) ────────────────────────────────────────────────
        private static void ConfigurarRabbitMq(
            IBusRegistrationConfigurator x,
            IConfiguration configuration)
        {
            var host = configuration["RabbitMq:Host"];
            var user = configuration["RabbitMq:Username"];
            var pass = configuration["RabbitMq:Password"];

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.UseInMemoryOutbox();

                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(30)));

                cfg.Host(host, "/", h =>
                {
                    h.Username(user!);
                    h.Password(pass!);
                });

                // Nome da fila gerado automaticamente pelo MassTransit
                cfg.ConfigureEndpoints(context);
            });
        }

        // ── Amazon SQS (LAB / AWS) ──────────────────────────────────────────
        private static void ConfigurarSqs(
            IBusRegistrationConfigurator x,
            IConfiguration configuration,
            ILogger logger)
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
            var donationQueue = configuration["QUEUES:DONATION_CREATED_QUEUE"];

            logger.LogInformation("***** Queue configuraed: " + donationQueue);

            x.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(region, h => { });

                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(30)));

                cfg.ReceiveEndpoint(donationQueue, e =>
                {
                    e.DefaultContentType = new System.Net.Mime.ContentType("application/json");
                    e.UseRawJsonSerializer(RawSerializerOptions.AnyMessageType);

                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                    // Desabilita a criação da fila _error pelo MassTransit
                    e.DiscardFaultedMessages();
                    e.DiscardSkippedMessages();
                    e.ConfigureConsumer<DonationCreatedEventConsumer>(context);
                });
            });
        }

        // ── Cliente IAmazonSQS (para IMessageService publicar eventos) ───────
        private static void ConfigurarClienteSqs(
            IServiceCollection services,
            IConfiguration configuration,
            string applicationType,
            ILogger logger)
        {
            if (applicationType == "LOCAL")
            {
                logger.LogInformation("***** SQS dummy registrado (LOCAL).");

                var dummyOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
                {
                    Credentials = new Amazon.Runtime.BasicAWSCredentials("ignore", "ignore"),
                    Region = Amazon.RegionEndpoint.USEast1
                };

                services.AddDefaultAWSOptions(dummyOptions);
                services.AddAWSService<IAmazonSQS>();
            }
            else
            {
                logger.LogInformation("***** SQS real registrado (LAB/AWS).");

                var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
                var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

                var sqsConfig = new AmazonSQSConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
                };

                Amazon.Runtime.AWSCredentials credentials = !string.IsNullOrEmpty(sessionToken)
                    ? new Amazon.Runtime.SessionAWSCredentials(accessKey, secretKey, sessionToken)
                    : new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);

                services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, sqsConfig));
            }
        }
    }
}
