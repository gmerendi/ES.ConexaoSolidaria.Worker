using DonationWorker.Consumers;
using DonationWorker.Domain.Shared.Interfaces;
using DonationWorker.Infrastructure.Services.Messaging;
using MassTransit;

namespace DonationWorker.Infrastructure.Extensions
{
    public static class MessagingExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration, ILogger logger)
        {
            services.AddScoped<IMessageService, MessageService>();

            var applicationType = configuration["Application:Type"] ?? "LOCAL";

            services.AddMassTransit(x =>
            {

                // Consumers
                x.AddConsumer<DonationCreatedEventConsumer>();
                x.AddDelayedMessageScheduler();

                var host = configuration["RabbitMq:Host"];
                var user = configuration["RabbitMq:Username"];
                var pass = configuration["RabbitMq:Password"];
                var donation_created_queue = configuration["QUEUES:DONATION_CREATED_QUEUE"];

                x.UsingRabbitMq((context, cfg) =>
                {
                    // Mantém na memória do app se o Rabbit cair
                    cfg.UseInMemoryOutbox();

                    cfg.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(30)));

                    
                    // AWS usa URI completa; todos os outros ambientes usam hostname simples
                    if (applicationType == "AWS")
                    {
                        cfg.Host(new Uri(host!), "/", h =>
                        {
                            h.Username(user!);
                            h.Password(pass!);
                        });
                    }
                    else
                    {
                        cfg.Host(host, "/", h =>
                        {
                            h.Username(user!);
                            h.Password(pass!);
                        });
                    }

                   
                    cfg.ReceiveEndpoint(donation_created_queue, e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // Tenta 3 vezes a cada 5 seg
                        e.ConfigureConsumer<DonationCreatedEventConsumer>(context);
                    });

                    //cfg.ConfigureEndpoints(context);
                });
            });

            // Não bloqueia o startup se o RabbitMQ ainda não estiver disponível
            services.AddOptions<MassTransitHostOptions>().Configure(options =>
            {
                options.WaitUntilStarted = false;
                options.StartTimeout = TimeSpan.FromSeconds(30);
                options.StopTimeout = TimeSpan.FromSeconds(15);
            });

            logger.LogInformation(" ***** Masstransit service inicializado.");

            return services;
        }
    }
}

       